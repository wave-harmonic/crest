// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
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

        // debug use
        public static bool _shapeCombinePass = true;
        public static bool _readbackCollData = true;
        public static float _copyCollDataTime = 0f;

        struct CollisionRequest
        {
            public AsyncGPUReadbackRequest _request;
            public RenderData _renderData;
        }
        Queue<CollisionRequest> _requests = new Queue<CollisionRequest>();

        public bool _useAsync = true;
        const int MAX_REQUESTS = 8;
        public int _successCount = 0;
        public int _errorCount = 0;
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
            // beginning of update turns out to be a good time to sample the displacement textures. i had
            // issues doing this in post render because there is a follow up pass for the lod0 camera which
            // combines shape textures, and this pass was not included.
            EnqueueReadbackRequest(cam.targetTexture);

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

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (_readbackCollData)
            {
                Profiler.BeginSample("Clean requests");
                for (int i = 0; i < MAX_REQUESTS && _requests.Count > 0; i++)
                {
                    var request = _requests.Peek();
                    if (request._request.hasError)
                    {
                        ++_errorCount;
                        _requests.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }
                Profiler.EndSample();

                Profiler.BeginSample("Create coll data");
                var num = 4 * cam.targetTexture.width * cam.targetTexture.height;
                if (!_collDataNative.IsCreated || _collDataNative.Length != num)
                {
                    _collDataNative = new NativeArray<ushort>(num, Allocator.Persistent);
                }
                Profiler.EndSample();

                if (_requests.Count > 0)
                {
                    var request = _requests.Peek();
                    if (request._request.done)
                    {
                        Profiler.BeginSample("Copy out data");
                        ++_successCount;
                        Profiler.BeginSample("Get data");
                        var data = request._request.GetData<ushort>();
                        Profiler.EndSample();
                        data.CopyTo(_collDataNative);
                        _collRenderData = request._renderData;
                        Profiler.EndSample();

                        _requests.Dequeue();
                    }
                }
            }

            if (_lodIndex == 0)
            {
                {
                    if (_marker1 == null)
                    {
                        _marker1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(_marker1.GetComponent<Collider>());
                    }

                    var query = Camera.main.transform.position + Camera.main.transform.forward * 10f;
                    query.y = 0f;
                    var disp = SampleDisplacement(query);
                    Debug.DrawLine(query, query + disp);
                    _marker1.transform.position = query + disp;
                }
                {
                    if (_marker2 == null)
                    {
                        _marker2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(_marker2.GetComponent<Collider>());
                    }

                    var query = 5f * Vector3.forward + Camera.main.transform.position + Camera.main.transform.forward * 10f;
                    query.y = 0f;
                    var disp = SampleDisplacement(query);
                    Debug.DrawLine(query, query + disp);
                    _marker2.transform.position = query + disp;
                }
                {
                    if (_marker3 == null)
                    {
                        _marker3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(_marker3.GetComponent<Collider>());
                    }

                    var query = 5f * Vector3.right + Camera.main.transform.position + Camera.main.transform.forward * 10f;
                    query.y = 0f;
                    var disp = SampleDisplacement(query);
                    Debug.DrawLine(query, query + disp);
                    _marker3.transform.position = query + disp;
                }
            }

            _copyCollDataTime = sw.ElapsedMilliseconds;
        }

        GameObject _marker1, _marker2, _marker3;

        private void OnDestroy()
        {
            if (_collDataNative.IsCreated)
            {
                _collDataNative.Dispose();
            }
        }

        Vector3 SampleDisplacement(Vector3 worldPos)
        {
            Profiler.BeginSample("SampleDisplacement");

            float xOffset = worldPos.x - _collRenderData._posSnapped.x;
            float zOffset = worldPos.z - _collRenderData._posSnapped.z;
            float r = _collRenderData._texelWidth * _collRenderData._textureRes / 2f;
            if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
            {
                return Vector3.zero;
            }

            var u = 0.5f + 0.5f * xOffset / r;
            var v = 0.5f + 0.5f * zOffset / r;
            var rt = cam.targetTexture;
            var x = Mathf.FloorToInt(u * rt.width);
            var y = Mathf.FloorToInt(v * rt.height);
            var idx = 4 * (y * rt.width + x);

            Vector3 sample;
            sample.x = Mathf.HalfToFloat(_collDataNative[idx + 0]);
            sample.y = Mathf.HalfToFloat(_collDataNative[idx + 1]);
            sample.z = Mathf.HalfToFloat(_collDataNative[idx + 2]);

            Profiler.EndSample();

            return sample;
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

        public void EnqueueReadbackRequest(RenderTexture target)
        {
            if (_useAsync)
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
        }

        //private void OnPostRender()
        //{
        //    if (_lodIndex == 0)
        //    {
        //        foreach(var wdc in OceanRenderer.Instance.Builder._shapeWDCs)
        //        {
        //            wdc.EnqueueReadbackRequest(wdc.GetComponent<Camera>().targetTexture);
        //        }
        //    }
        //}

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
