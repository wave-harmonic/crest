// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

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

        // debug use
        public static bool _shapeCombinePass = true;
        public static bool _readbackCollData = true;

        // read shape textures back to the CPU for collision purposes
        public bool _readbackShapeForCollision = true;

        struct CollisionRequest
        {
            public AsyncGPUReadbackRequest _request;
            public RenderData _renderData;
        }
        Queue<CollisionRequest> _requests = new Queue<CollisionRequest>();
        const int MAX_REQUESTS = 8;

        // collision data
        NativeArray<ushort> _collDataNative;
        RenderData _collRenderData;

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
            // request current contents of this shape texture
            if (_readbackShapeForCollision)
            {
                UpdateShapeReadback();
            }

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

        void UpdateShapeReadback()
        {
            // shape textures are read back to the CPU for collision purposes. this uses an experimental API which
            // will hopefully be settled in future unity versions.
            // queue pattern inspired by: https://github.com/keijiro/AsyncCaptureTest

            // beginning of update turns out to be a good time to sample the displacement textures. i had
            // issues doing this in post render because there is a follow up pass for the lod0 camera which
            // combines shape textures, and this pass was not included.
            EnqueueReadbackRequest(cam.targetTexture);

            if (!_readbackCollData)
                return;

            // remove any failed readback requests
            for (int i = 0; i < MAX_REQUESTS && _requests.Count > 0; i++)
            {
                var request = _requests.Peek();
                if (request._request.hasError)
                {
                    _requests.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // create array to hold collision data if we don't have one already
            var num = 4 * cam.targetTexture.width * cam.targetTexture.height;
            if (!_collDataNative.IsCreated || _collDataNative.Length != num)
            {
                _collDataNative = new NativeArray<ushort>(num, Allocator.Persistent);
            }

            // process current request queue
            if (_requests.Count > 0)
            {
                var request = _requests.Peek();
                if (request._request.done)
                {
                    _requests.Dequeue();

                    // eat up any more completed requests to squeeze out latency wherever possible
                    CollisionRequest nextRequest;
                    while(_requests.Count > 0 && (nextRequest = _requests.Peek())._request.done)
                    {
                        request = nextRequest;
                        _requests.Dequeue();
                    }

                    Profiler.BeginSample("Copy out collision data");

                    var data = request._request.GetData<ushort>();
                    data.CopyTo(_collDataNative);
                    _collRenderData = request._renderData;

                    Profiler.EndSample();
                }
            }
        }

        public Rect CollisionDataRectXZ
        {
            get
            {
                float w = _collRenderData._texelWidth * _collRenderData._textureRes;
                return new Rect(_collRenderData._posSnapped.x - w / 2f, _collRenderData._posSnapped.z - w / 2f, w, w);
            }
        }

        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            Profiler.BeginSample("SampleDisplacement");

            float xOffset = worldPos.x - _collRenderData._posSnapped.x;
            float zOffset = worldPos.z - _collRenderData._posSnapped.z;
            float r = _collRenderData._texelWidth * _collRenderData._textureRes / 2f;
            if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
            {
                // outside of this collision data
                return false;
            }

            var u = 0.5f + 0.5f * xOffset / r;
            var v = 0.5f + 0.5f * zOffset / r;
            var rt = cam.targetTexture;
            var x = Mathf.FloorToInt(u * rt.width);
            var y = Mathf.FloorToInt(v * rt.height);
            var idx = 4 * (y * rt.width + x);

            displacement.x = Mathf.HalfToFloat(_collDataNative[idx + 0]);
            displacement.y = Mathf.HalfToFloat(_collDataNative[idx + 1]);
            displacement.z = Mathf.HalfToFloat(_collDataNative[idx + 2]);

            Profiler.EndSample();

            return true;
        }

        /// <summary>
        /// Get position on ocean plane that displaces horizontally to the given position.
        /// </summary>
        public Vector3 GetPositionDisplacedToPositionExpensive(ref Vector3 displacedWorldPos)
        {
            // fixed point iteration - guess should converge to location that displaces to the target position

            var guess = displacedWorldPos;

            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            for (int i = 0; i < 4; i++)
            {
                var disp = Vector3.zero;
                SampleDisplacement(ref guess, ref disp);
                var error = guess + disp - displacedWorldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }
            guess.y = OceanRenderer.Instance.SeaLevel;
            return guess;
        }

        public float GetHeightExpensive(ref Vector3 worldPos)
        {
            var posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            var undisplacedPos = GetPositionDisplacedToPositionExpensive(ref posFlatland);

            var disp = Vector3.zero;
            SampleDisplacement(ref undisplacedPos, ref disp);

            return posFlatland.y + disp.y;
        }

        public void EnqueueReadbackRequest(RenderTexture target)
        {
            if (_requests.Count < MAX_REQUESTS)
            {
                _requests.Enqueue(
                    new CollisionRequest
                    {
                        _request = AsyncGPUReadback.Request(cam.targetTexture),
                        _renderData = _renderData
                    }
                );
            }
        }

        public float MaxWavelength()
        {
            float oceanBaseScale = OceanRenderer.Instance.transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, _lodIndex);
            float maxTexelSize = maxDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            return 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;
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
            ApplyMaterialParams(0, new PropertyWrapperMaterial(_combineMaterial));
            if (_lodIndex > 0)
            {
                var wdcs = OceanRenderer.Instance.Builder._shapeWDCs;
                ApplyMaterialParams(1, new PropertyWrapperMaterial(wdcs[_lodIndex - 1]._combineMaterial));
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
                    var mat = OceanRenderer.Instance.Builder._shapeWDCs[L]._combineMaterial;
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

        private void OnDestroy()
        {
            if (_collDataNative.IsCreated)
            {
                _collDataNative.Dispose();
            }
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
