using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering; // async readback experimental prior to 2018.2
using Unity.Collections;
using UnityEngine.Profiling;

namespace Crest
{
    /// <summary>
    /// Class that handles asynchronously copying LODData back from the GPU to access on the CPU.
    /// </summary>
    public class ReadbackLodData : MonoBehaviour
    {
        struct ReadbackRequest
        {
            public AsyncGPUReadbackRequest _request;
            public LodTransform.RenderData _renderData;
        }

        Queue<ReadbackRequest> _requests = new Queue<ReadbackRequest>();
        const int MAX_REQUESTS = 8;

        // data
        public struct Result
        {
            public NativeArray<ushort> _data;
            public LodTransform.RenderData _renderData;
            public float _time;

            public bool SampleARGB16(ref Vector3 in__worldPos, out Vector3 displacement)
            {
                float xOffset = in__worldPos.x - _renderData._posSnapped.x;
                float zOffset = in__worldPos.z - _renderData._posSnapped.z;
                float r = _renderData._texelWidth * _renderData._textureRes / 2f;
                if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
                {
                    // outside of this collision data
                    displacement = Vector3.zero;
                    return false;
                }

                var u = 0.5f + 0.5f * xOffset / r;
                var v = 0.5f + 0.5f * zOffset / r;
                var x = Mathf.FloorToInt(u * _renderData._textureRes);
                var y = Mathf.FloorToInt(v * _renderData._textureRes);
                var idx = 4 * (y * (int)_renderData._textureRes + x);

                displacement.x = Mathf.HalfToFloat(_data[idx + 0]);
                displacement.y = Mathf.HalfToFloat(_data[idx + 1]);
                displacement.z = Mathf.HalfToFloat(_data[idx + 2]);

                return true;
            }

            public bool SampleRG16(ref Vector3 in__worldPos, out Vector2 flow)
            {
                float xOffset = in__worldPos.x - _renderData._posSnapped.x;
                float zOffset = in__worldPos.z - _renderData._posSnapped.z;
                float r = _renderData._texelWidth * _renderData._textureRes / 2f;
                if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
                {
                    // outside of this collision data
                    flow = Vector3.zero;
                    return false;
                }

                var u = 0.5f + 0.5f * xOffset / r;
                var v = 0.5f + 0.5f * zOffset / r;
                var x = Mathf.FloorToInt(u * _renderData._textureRes);
                var y = Mathf.FloorToInt(v * _renderData._textureRes);
                var idx = 2 * (y * (int)_renderData._textureRes + x);
                flow.x = Mathf.HalfToFloat(_data[idx + 0]);
                flow.y = Mathf.HalfToFloat(_data[idx + 1]);

                return true;
            }
        }
        public Result _result;

        public bool _active = true;

        public enum TexFormat
        {
            NotSet = 0,
            RHalf = 1,
            RGHalf = 2,
            RGBAHalf = 4,
        }
        [SerializeField] private TexFormat _textureFormat = TexFormat.NotSet;

        public void SetTextureFormat(RenderTextureFormat fmt)
        {
            switch(fmt)
            {
                case RenderTextureFormat.RHalf:
                    _textureFormat = TexFormat.RHalf;
                    break;
                case RenderTextureFormat.RGHalf:
                    _textureFormat = TexFormat.RGHalf;
                    break;
                case RenderTextureFormat.ARGBHalf:
                    _textureFormat = TexFormat.RGBAHalf;
                    break;
                default:
                    Debug.LogError("Unsupported texture format for readback: " + fmt.ToString(), this);
                    break;
            }
        }

        private void Update()
        {
            if (_active)
            {
                Debug.Assert(_textureFormat != TexFormat.NotSet, "ReadbackLodData: Texture format must be set.", this);

                UpdateReadback(Cam, LT._renderData);
            }
        }

        /// <summary>
        /// Request current contents of cameras shape texture.
        /// </summary>
        public void UpdateReadback(Camera cam, LodTransform.RenderData renderData)
        {
            // queue pattern inspired by: https://github.com/keijiro/AsyncCaptureTest

            // beginning of update turns out to be a good time to sample the textures to ensure everything in the frame is done.
            EnqueueReadbackRequest(cam.targetTexture, renderData);

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

            // create array to hold data if we don't have one already
            var num = ((int)_textureFormat) * cam.targetTexture.width * cam.targetTexture.height;
            if (!_result._data.IsCreated || _result._data.Length != num)
            {
                _result._data = new NativeArray<ushort>(num, Allocator.Persistent);
            }

            // process current request queue
            if (_requests.Count > 0)
            {
                var request = _requests.Peek();
                if (request._request.done)
                {
                    _requests.Dequeue();

                    // eat up any more completed requests to squeeze out latency wherever possible
                    ReadbackRequest nextRequest;
                    while (_requests.Count > 0 && (nextRequest = _requests.Peek())._request.done)
                    {
                        request = nextRequest;
                        _requests.Dequeue();
                    }

                    Profiler.BeginSample("Copy out readback data");

                    var data = request._request.GetData<ushort>();
                    data.CopyTo(_result._data);
                    _result._renderData = request._renderData;
                    
                    Profiler.EndSample();
                }
            }
        }

        public void EnqueueReadbackRequest(RenderTexture target, LodTransform.RenderData renderData)
        {
            if (_requests.Count < MAX_REQUESTS)
            {
                _requests.Enqueue(
                    new ReadbackRequest
                    {
                        _request = AsyncGPUReadback.Request(target),
                        _renderData = renderData
                    }
                );
            }
        }

        public Rect DataRectXZ
        {
            get
            {
                float w = _result._renderData._texelWidth * _result._renderData._textureRes;
                return new Rect(_result._renderData._posSnapped.x - w / 2f, _result._renderData._posSnapped.z - w / 2f, w, w);
            }
        }

        public int ReadbackRequestsQueued { get { return _requests.Count; } }

        void OnDisable()
        {
            // free native array when component removed or destroyed
            if (_result._data.IsCreated)
            {
                _result._data.Dispose();
            }
        }

        LodTransform _lt; LodTransform LT { get { return _lt ?? (_lt = GetComponent<LodTransform>()); } }
        Camera _cam; Camera Cam { get { return _cam ?? (_cam = GetComponent<Camera>()); } }
    }
}
