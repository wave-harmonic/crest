// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace OceanResearch
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

        public RenderTexture _rtOceanDepth;
        CommandBuffer _bufOceanDepth = null;
        Material _matOceanDepth;

        int _shapeRes = -1;

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posContinuous;
            public Vector3 _posSnapped;
            public Vector3 _posSnappedLast;
        }
        public RenderData _renderData = new RenderData();

        void Start()
        {
            cam.depthTextureMode = DepthTextureMode.None;

            _matOceanDepth = new Material( Shader.Find( "Ocean/Ocean Depth" ) );
        }

        private void Update()
        {
            _renderData._posSnappedLast = _renderData._posSnapped;
        }

        // script execution order ensures this runs after CircleOffset
        void LateUpdate()
        {
            // ensure camera size matches geometry size
            cam.orthographicSize = 2f * Mathf.Abs( transform.lossyScale.x );
            bool flip = transform.lossyScale.z < 0f;
            transform.localEulerAngles = new Vector3( flip ? -90f : 90f, 0f, 0f );

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
            _renderData._posContinuous = transform.position;
            _renderData._posSnapped = _renderData._posContinuous
                - new Vector3( Mathf.Repeat( _renderData._posContinuous.x, _renderData._texelWidth ), 0f, Mathf.Repeat( _renderData._posContinuous.z, _renderData._texelWidth ) );

            // set projection matrix to snap to texels
            cam.ResetProjectionMatrix();
            Matrix4x4 P = cam.projectionMatrix, T = new Matrix4x4();
            T.SetTRS( new Vector3( _renderData._posContinuous.x - _renderData._posSnapped.x, _renderData._posContinuous.z - _renderData._posSnapped.z ), Quaternion.identity, Vector3.one );
            P = P * T;
            cam.projectionMatrix = P;

            UpdateCommandBuffer();
        }

        void UpdateCommandBuffer()
        {
            if( !_rtOceanDepth )
            {
                _rtOceanDepth = new RenderTexture( cam.targetTexture.width, cam.targetTexture.height, 0 );
                _rtOceanDepth.name = gameObject.name + "_oceanDepth";
                _rtOceanDepth.format = RenderTextureFormat.RFloat;
                _rtOceanDepth.useMipMap = false;
                _rtOceanDepth.anisoLevel = 0;
            }

            if( _bufOceanDepth == null )
            {
                _bufOceanDepth = new CommandBuffer();
                cam.AddCommandBuffer( CameraEvent.BeforeForwardOpaque, _bufOceanDepth );
                _bufOceanDepth.name = "Ocean Depth";
            }

            _bufOceanDepth.Clear();

            _bufOceanDepth.SetRenderTarget( _rtOceanDepth );
            var gos = GameObject.FindGameObjectsWithTag( "OceanDepth" );
            foreach( var go in gos )
            {
                var r = go.GetComponent<Renderer>();
                if( !r ) continue;

                _bufOceanDepth.ClearRenderTarget( false, true, Color.red * 100.0f );
                _bufOceanDepth.DrawRenderer( r, _matOceanDepth );
            }
        }

        void RemoveCommandBuffer()
        {
            if( _bufOceanDepth == null ) return;
            cam.RemoveCommandBuffer( CameraEvent.BeforeForwardOpaque, _bufOceanDepth );
            _bufOceanDepth = null;
        }

        void OnEnable()
        {
            RemoveCommandBuffer();
        }
        void OnDisable()
        {
            RemoveCommandBuffer();
        }

        public void ApplyMaterialParams( int shapeSlot, Material mat )
        {
            mat.SetTexture( "_WD_Sampler_" + shapeSlot.ToString(), cam.targetTexture );
            mat.SetTexture( "_WD_OceanDepth_Sampler_" + shapeSlot.ToString(), _rtOceanDepth );

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = _lodIndex == _lodCount - 1 && OceanRenderer.Instance.ScaleCouldHalve;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            mat.SetVector( "_WD_Params_" + shapeSlot.ToString(), new Vector3( _renderData._texelWidth, _renderData._textureRes, shapeWeight ) );

            mat.SetVector( "_WD_Pos_" + shapeSlot.ToString(), new Vector2( _renderData._posSnapped.x, _renderData._posSnapped.z ) );
            mat.SetVector( "_WD_Pos_Cont_" + shapeSlot.ToString(), new Vector2( _renderData._posContinuous.x, _renderData._posContinuous.z ) );
            mat.SetInt( "_WD_LodIdx_" + shapeSlot.ToString(), _lodIndex );
        }

        private void OnPostRender()
        {
            // accumulate sim lod data when simulations stop rendering.

            // in ocean builder, we set camera depths to ensure the LOD0 camera renders last. so if this is the camera for LOD0, we know its safe now
            // to start combining sim results.
            if( OceanRenderer.Instance.Builder.GetShapeCamIndex( cam ) == 0 )
            {
                ShapeWaveSim.Instance.OnShapeCamerasFinishedRendering();
            }
        }

        Camera _camera; Camera cam { get { return _camera != null ? _camera : (_camera = GetComponent<Camera>()); } }
    }
}
