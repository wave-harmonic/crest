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

        public enum TexFormat
        {
            NotSet = 0,
            RHalf = 1,
            RGHalf = 2,
            RGBAHalf = 4,
        }
        public TexFormat _textureFormat = TexFormat.RGBAHalf;

        Queue<ReadbackRequest> _requests = new Queue<ReadbackRequest>();
        const int MAX_REQUESTS = 8;

        // data
        public NativeArray<ushort> _dataNative;
        public LodTransform.RenderData _dataRenderData;

        public bool _active = true;

        private void Update()
        {
            if (_active)
            {
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
            if (!_dataNative.IsCreated || _dataNative.Length != num)
            {
                _dataNative = new NativeArray<ushort>(num, Allocator.Persistent);
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
                    data.CopyTo(_dataNative);
                    _dataRenderData = request._renderData;

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
                float w = _dataRenderData._texelWidth * _dataRenderData._textureRes;
                return new Rect(_dataRenderData._posSnapped.x - w / 2f, _dataRenderData._posSnapped.z - w / 2f, w, w);
            }
        }

        public int ReadbackRequestsQueued { get { return _requests.Count; } }

        void OnDisable()
        {
            // free native array when component removed or destroyed
            if (_dataNative.IsCreated)
            {
                _dataNative.Dispose();
            }
        }

        LodTransform _lt; LodTransform LT { get { return _lt ?? (_lt = GetComponent<LodTransform>()); } }
        Camera _cam; Camera Cam { get { return _cam ?? (_cam = GetComponent<Camera>()); } }
    }
}
