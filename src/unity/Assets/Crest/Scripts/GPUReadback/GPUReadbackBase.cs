using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class GPUReadbackBase<LodDataType> : MonoBehaviour where LodDataType : LodData
    {
        [SerializeField] protected float _minGridSize = 4f;
        [SerializeField] protected float _maxGridSize = 4f;
        [SerializeField] protected LodDataType[] _lodData;

        SortedList<float, ReadbackResults> _results = new SortedList<float, ReadbackResults>();
        //SortedList<float, ReadbackResult> _resultLast = new SortedList<float, ReadbackResult>();

        struct ReadbackRequest
        {
            public AsyncGPUReadbackRequest _request;
            public LodTransform.RenderData _renderData;
            public float _time;
        }

        Queue<ReadbackRequest> _requests = new Queue<ReadbackRequest>();
        const int MAX_REQUESTS = 16;

        float _prevTime;
        int _updateFrame;
        TexFormat _textureFormat = TexFormat.NotSet;

        protected virtual void Start()
        {
            _lodData = OceanRenderer.Instance.GetComponentsInChildren<LodDataType>();
            _prevTime = Time.time;

            SetTextureFormat(_lodData[0].TextureFormat);
        }

        protected virtual void Update()
        {
            foreach(var data in _lodData)
            {
                var lt = data.LodTransform;
                if (lt._renderData._texelWidth >= _minGridSize && lt._renderData._texelWidth <= _maxGridSize)
                {
                    var cam = lt.GetComponent<Camera>();

                    if (!_results.ContainsKey(lt._renderData._texelWidth))
                    {
                        var resultData = new ReadbackResults();
                        resultData._result = new ReadbackResult();
                        resultData._resultLast = new ReadbackResult();

                        // create native arrays
                        Debug.Assert(_textureFormat != TexFormat.NotSet, "ReadbackLodData: Texture format must be set.", this);
                        var num = ((int)_textureFormat) * cam.targetTexture.width * cam.targetTexture.height;
                        if (!resultData._result._data.IsCreated || resultData._result._data.Length != num)
                        {
                            resultData._result._data = new NativeArray<ushort>(num, Allocator.Persistent);
                            resultData._resultLast._data = new NativeArray<ushort>(num, Allocator.Persistent);
                        }

                        _results.Add(lt._renderData._texelWidth, resultData);
                    }

                    UpdateReadback(cam.targetTexture, lt._renderData);
                    ProcessRequests(cam);
                }
            }
        }

        /// <summary>
        /// Request current contents of cameras shape texture.
        /// </summary>
        public void UpdateReadback(RenderTexture target, LodTransform.RenderData renderData)
        {
            // queue pattern inspired by: https://github.com/keijiro/AsyncCaptureTest

            // beginning of update turns out to be a good time to sample the textures to ensure everything in the frame is done.
            EnqueueReadbackRequest(target, renderData, _prevTime);
            _prevTime = Time.time;
        }

        public void EnqueueReadbackRequest(RenderTexture target, LodTransform.RenderData renderData, float time)
        {
            // only queue up requests while time is advancing
            if (time <= _results[renderData._texelWidth]._result._time)
            {
                return;
            }

            if (_requests.Count < MAX_REQUESTS)
            {
                _requests.Enqueue(
                    new ReadbackRequest
                    {
                        _request = AsyncGPUReadback.Request(target),
                        _renderData = renderData,
                        _time = time,
                    }
                );
            }
        }

        public void ProcessRequests(Camera cam)
        {
            // Physics stuff may call update from FixedUpdate() - therefore check if this component was already
            // updated this frame.
            if (_updateFrame == Time.frameCount)
            {
                return;
            }
            _updateFrame = Time.frameCount;

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

                    UnityEngine.Profiling.Profiler.BeginSample("Copy out readback data");

                    float gridSize = request._renderData._texelWidth;

                    var result = _results[gridSize]._result;
                    var resultLast = _results[gridSize]._resultLast;

                    // copy result into resultLast
                    resultLast._renderData = result._renderData;
                    resultLast._time = result._time;
                    Swap(ref result._data, ref resultLast._data);

                    // copy new data into result
                    var data = request._request.GetData<ushort>();
                    data.CopyTo(result._data);
                    result._renderData = request._renderData;
                    result._time = request._time;

                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
        }

        void Swap(ref NativeArray<ushort> arr1, ref NativeArray<ushort> arr2)
        {
            var temp = arr2;
            arr2 = arr1;
            arr1 = temp;
        }

        public enum TexFormat
        {
            NotSet = 0,
            RHalf = 1,
            RGHalf = 2,
            RGBAHalf = 4,
        }

        public void SetTextureFormat(RenderTextureFormat fmt)
        {
            switch (fmt)
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

        void OnDisable()
        {
            // free native array when component removed or destroyed
            foreach(var result in _results.Values)
            {
                if (result == null || result._result == null) continue;
                if (result._result._data.IsCreated) result._result._data.Dispose();
                if (result._resultLast._data.IsCreated) result._resultLast._data.Dispose();
            }
        }

        public class ReadbackResults
        {
            public ReadbackResult _result;
            public ReadbackResult _resultLast;
        }

        public class ReadbackResult
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

            public bool InterpolateARGB16(ref Vector3 in__worldPos, out Vector3 displacement)
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
                float u_texels = Mathf.Max(u * _renderData._textureRes, 0f);
                float v_texels = Mathf.Max(v * _renderData._textureRes, 0f);

                int width = (int)_renderData._textureRes;

                var x0 = Mathf.FloorToInt(u_texels);
                var x1 = Mathf.Min(x0 + 1, width - 1);
                var z0 = Mathf.FloorToInt(v_texels);
                var z1 = Mathf.Min(z0 + 1, width - 1);

                var idx00 = 4 * (z0 * width + x0);
                var idx01 = 4 * (z0 * width + x1);
                var idx10 = 4 * (z1 * width + x0);
                var idx11 = 4 * (z1 * width + x1);

                float x00 = Mathf.HalfToFloat(_data[idx00 + 0]);
                float y00 = Mathf.HalfToFloat(_data[idx00 + 1]);
                float z00 = Mathf.HalfToFloat(_data[idx00 + 2]);
                float x01 = Mathf.HalfToFloat(_data[idx01 + 0]);
                float y01 = Mathf.HalfToFloat(_data[idx01 + 1]);
                float z01 = Mathf.HalfToFloat(_data[idx01 + 2]);
                float x10 = Mathf.HalfToFloat(_data[idx10 + 0]);
                float y10 = Mathf.HalfToFloat(_data[idx10 + 1]);
                float z10 = Mathf.HalfToFloat(_data[idx10 + 2]);
                float x11 = Mathf.HalfToFloat(_data[idx11 + 0]);
                float y11 = Mathf.HalfToFloat(_data[idx11 + 1]);
                float z11 = Mathf.HalfToFloat(_data[idx11 + 2]);

                var xf = Mathf.Repeat(u_texels, 1f);
                var zf = Mathf.Repeat(v_texels, 1f);
                displacement.x = Mathf.Lerp(Mathf.Lerp(x00, x01, xf), Mathf.Lerp(x10, x11, xf), zf);
                displacement.y = Mathf.Lerp(Mathf.Lerp(y00, y01, xf), Mathf.Lerp(y10, y11, xf), zf);
                displacement.z = Mathf.Lerp(Mathf.Lerp(z00, z01, xf), Mathf.Lerp(z10, z11, xf), zf);

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

            public Rect DataRectXZ
            {
                get
                {
                    float w = _renderData._texelWidth * _renderData._textureRes;
                    return new Rect(_renderData._posSnapped.x - w / 2f, _renderData._posSnapped.z - w / 2f, w, w);
                }
            }
        }

        /// <summary>
        /// Returns result of GPU readback of a LOD data. Do not hold onto the returned reference across frames.
        /// </summary>
        /// <param name="sampleAreaXZ">The area of interest, can be 0 area.</param>
        /// <param name="minSpatialLength">Min spatial length is the minimum side length that you care about. For e.g.
        /// if a boat has dimensions 3m x 2m, set this to 2, and then suitable wavelengths will be preferred.</param>
        protected ReadbackResults GetData(Rect sampleAreaXZ, float minSpatialLength)
        {
            ReadbackResults lastCandidate = null;

            foreach (var frameResults in _results)
            {
                // Check that the region of interest is covered by this data
                var wdcRect = frameResults.Value._result.DataRectXZ;
                // Shrink rect by 1 texel border - this is to make finite differences fit as well
                float texelWidth = frameResults.Key;
                wdcRect.x += texelWidth; wdcRect.y += texelWidth;
                wdcRect.width -= 2f * texelWidth; wdcRect.height -= 2f * texelWidth;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                {
                    continue;
                }

                // This data covers our required area, so store it as a potential candidate
                lastCandidate = frameResults.Value;

                // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
                // in the last LOD - then this is the best we can do.
                float minWavelength = texelWidth * OceanRenderer.Instance._minTexelsPerWave;
                if (minSpatialLength / minWavelength > 2f)
                {
                    continue;
                }

                // A good match - return immediately
                return frameResults.Value;
            }

            // We didnt get a perfect match, but pick the next best candidate
            return lastCandidate;
        }
    }
}
