// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Positions wave data render camera. Snaps to shape texels to avoid aliasing.
    /// </summary>
    public class WaveDataCam : MonoBehaviour
    {
        [HideInInspector]
        public int _lodIndex = 0;
        [HideInInspector]
        public int _lodCount = 5;

        Material _matOceanDepth;
        RenderTexture _rtOceanDepth;
        CommandBuffer _bufOceanDepth = null;
        bool _oceanDepthRenderersDirty = true;
        /// <summary>Called when one or more objects that will render into depth are created, so that all objects are registered.</summary>
        public void OnOceanDepthRenderersChanged() { _oceanDepthRenderersDirty = true; }

        Material _combineMaterial;
        CommandBuffer _bufCombineShapes = null;

        // shape texture resolution
        int _shapeRes = -1;

        public static bool _shapeCombinePass = true;

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
            public Vector3 _posSnappedLast;
        }
        public RenderData _renderData = new RenderData();

        void Start()
        {
            cam.depthTextureMode = DepthTextureMode.None;

            _matOceanDepth = new Material(Shader.Find("Ocean/Ocean Depth"));
            _combineMaterial = new Material(Shader.Find("Ocean/Shape/Combine"));
        }

        private void Update()
        {
            _renderData._posSnappedLast = _renderData._posSnapped;

            // shape combine pass done by last shape camera - lod 0
            if (_lodIndex == 0)
            {
                UpdateCmdBufShapeCombine();
            }

            if (_oceanDepthRenderersDirty)
            {
                UpdateCmdBufOceanFloorDepth();
                _oceanDepthRenderersDirty = false;
            }
        }

        // script execution order ensures this runs after ocean has been placed
        public void LateUpdate()
        {
            LateUpdateTransformData();

            LateUpdateShapeCombinePassSettings();
        }

        void LateUpdateTransformData()
        {
            // ensure camera size matches geometry size
            cam.orthographicSize = 2f * transform.lossyScale.x;

            // find snap period
            int width = cam.targetTexture.width;
            // debug functionality to resize RT if different size was specified.
            if( _shapeRes == -1 )
            {
                _shapeRes = width;
            }
            else if( width != _shapeRes )
            {
                cam.targetTexture.Release();
                cam.targetTexture.width = cam.targetTexture.height = _shapeRes;
                cam.targetTexture.Create();
            }
            _renderData._textureRes = (float)cam.targetTexture.width;
            _renderData._texelWidth = 2f * cam.orthographicSize / _renderData._textureRes;
            // snap so that shape texels are stationary
            _renderData._posSnapped = transform.position
                - new Vector3( Mathf.Repeat( transform.position.x, _renderData._texelWidth ), 0f, Mathf.Repeat( transform.position.z, _renderData._texelWidth ) );

            // set projection matrix to snap to texels
            cam.ResetProjectionMatrix();
            Matrix4x4 P = cam.projectionMatrix, T = new Matrix4x4();
            T.SetTRS( new Vector3( transform.position.x - _renderData._posSnapped.x, transform.position.z - _renderData._posSnapped.z ), Quaternion.identity, Vector3.one );
            P = P * T;
            cam.projectionMatrix = P;
        }

        // apply this camera's properties to the shape combine materials
        void LateUpdateShapeCombinePassSettings()
        {
            var cams = OceanRenderer.Instance.Builder._shapeCameras;
            ApplyMaterialParams(0, new PropertyWrapperMaterial(_combineMaterial));
            if (_lodIndex > 0)
            {
                ApplyMaterialParams(1, new PropertyWrapperMaterial(cams[_lodIndex - 1].GetComponent<WaveDataCam>()._combineMaterial));
            }
        }

        // The command buffer populates the LODs with ocean depth data. It submits any objects with a RenderOceanDepth component attached.
        // It's stateless - the textures don't have to be managed across frames/scale changes
        void UpdateCmdBufOceanFloorDepth()
        {
            var objs = FindObjectsOfType<RenderOceanDepth>();

            // if there is nothing in the scene tagged up for depth rendering then there is no depth rendering required
            if (objs.Length < 1)
            {
                if (_bufOceanDepth != null)
                {
                    _bufOceanDepth.Clear();
                }

                return;
            }

            if (!_rtOceanDepth)
            {
                _rtOceanDepth = new RenderTexture(cam.targetTexture.width, cam.targetTexture.height, 0);
                _rtOceanDepth.name = gameObject.name + "_oceanDepth";
                _rtOceanDepth.format = RenderTextureFormat.RHalf;
                _rtOceanDepth.useMipMap = false;
                _rtOceanDepth.anisoLevel = 0;
            }

            if (_bufOceanDepth == null)
            {
                _bufOceanDepth = new CommandBuffer();
                cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bufOceanDepth);
                _bufOceanDepth.name = "Ocean Depth";
            }

            _bufOceanDepth.Clear();

            _bufOceanDepth.SetRenderTarget( _rtOceanDepth );
            _bufOceanDepth.ClearRenderTarget( false, true, Color.red * 10000f );

            foreach (var obj in objs)
            {
                if (!obj.enabled)
                    continue;

                var r = obj.GetComponent<Renderer>();
                if (r == null)
                {
                    Debug.LogError("GameObject '" + obj.gameObject.name + "' must have a renderer component attached. Unity Terrain objects are not supported - these must be captured by an Ocean Depth Cache.", obj);
                }
                else if (obj.transform.parent != null && obj.transform.parent.GetComponent<OceanDepthCache>() != null)
                {
                    _bufOceanDepth.DrawRenderer(r, r.material);
                }
                else
                {
                    _bufOceanDepth.DrawRenderer(r, _matOceanDepth);
                }
            }
        }

        // executed once per frame - attached to the LOD0 camera
        void UpdateCmdBufShapeCombine()
        {
            if(!_shapeCombinePass)
            {
                if (_bufCombineShapes != null)
                {
                    cam.RemoveCommandBuffer(CameraEvent.AfterEverything, _bufCombineShapes);
                    _bufCombineShapes = null;
                }

                return;
            }

            // create shape combine command buffer if it hasn't been created already
            if (_bufCombineShapes == null)
            {
                _bufCombineShapes = new CommandBuffer();
                cam.AddCommandBuffer(CameraEvent.AfterEverything, _bufCombineShapes);
                _bufCombineShapes.name = "Combine Shapes";

                var cams = OceanRenderer.Instance.Builder._shapeCameras;
                for (int L = cams.Length - 2; L >= 0; L--)
                {
                    // accumulate shape data down the LOD chain - combine L+1 into L
                    var mat = cams[L].GetComponent<WaveDataCam>()._combineMaterial;
                    _bufCombineShapes.Blit(cams[L + 1].targetTexture, cams[L].targetTexture, mat);
                }
            }
        }

        void RemoveCommandBuffers()
        {
            if( _bufOceanDepth != null )
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _bufOceanDepth);
                _bufOceanDepth = null;
            }

            if (_bufCombineShapes != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterEverything, _bufCombineShapes);
                _bufCombineShapes = null;
            }
        }

        void OnEnable()
        {
            RemoveCommandBuffers();
        }

        void OnDisable()
        {
            RemoveCommandBuffers();
        }

        public void ApplyMaterialParams( int shapeSlot, IPropertyWrapper properties)
        {
            ApplyMaterialParams(shapeSlot, properties, true, true);
        }

        public void ApplyMaterialParams(int shapeSlot, IPropertyWrapper properties, bool applyWaveHeights, bool blendOut)
        {
            if (applyWaveHeights)
            {
                properties.SetTexture("_WD_Sampler_" + shapeSlot.ToString(), cam.targetTexture);
            }

            if (_rtOceanDepth != null)
            {
                properties.SetTexture("_WD_OceanDepth_Sampler_" + shapeSlot.ToString(), _rtOceanDepth);
            }

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = _lodIndex == _lodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            properties.SetVector("_WD_Params_" + shapeSlot.ToString(), new Vector3(_renderData._texelWidth, _renderData._textureRes, shapeWeight));

            properties.SetVector("_WD_Pos_" + shapeSlot.ToString(), new Vector2(_renderData._posSnapped.x, _renderData._posSnapped.z));
            properties.SetFloat("_WD_LodIdx_" + shapeSlot.ToString(), _lodIndex);
        }

        Camera _camera; Camera cam { get { return _camera != null ? _camera : (_camera = GetComponent<Camera>()); } }
    }
}
