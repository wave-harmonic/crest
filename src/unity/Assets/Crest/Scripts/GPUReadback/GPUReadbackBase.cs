// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public interface IReadbackSettingsProvider
    {
        void GetMinMaxGridSizes(out float minGridSize, out float maxGridSize);
    }

    /// <summary>
    /// Base class for reading back GPU data of a particular type to the CPU.
    /// </summary>
    public class GPUReadbackBase<LodDataType> : MonoBehaviour, IFloatingOrigin
        where LodDataType : LodDataMgr
    {
        public bool _doReadback = true;

        protected LodDataType _lodComponent;

        /// <summary>
        /// Minimum floating object width. The larger the objects that will float, the lower the resolution of the read data.
        /// If an object is small, the highest resolution LODs will be sample for physics. This is an optimisation. Set to 0
        /// to disable this optimisation and always copy high res data.
        /// </summary>
        protected float _minGridSize = 0f;
        /// <summary>
        /// Similar to the minimum width, but this setting will exclude the larger LODs from being copied. Set to 0 to disable
        /// this optimisation and always copy low res data.
        /// </summary>
        protected float _maxGridSize = 0f;

        protected IReadbackSettingsProvider _settingsProvider;

        protected virtual bool CanUseLastTwoLODs { get { return true; } }

        protected class PerLodData
        {
            public ReadbackData _resultData;
            public ReadbackData _resultDataPrevFrame;
            public Queue<ReadbackRequest> _requests = new Queue<ReadbackRequest>();

            public int _lastUpdateFrame = -1;
            public bool _activelyBeingRendered = true;
        }
        SortedListCachedArrays<float, PerLodData> _perLodData = new SortedListCachedArrays<float, PerLodData>();

        protected struct ReadbackRequest
        {
            public AsyncGPUReadbackRequest _request;
            public LodTransform.RenderData _renderData;
            public float _time;
        }

        const int MAX_REQUESTS = 4;

        TexFormat _textureFormat = TexFormat.NotSet;

        /// <summary>
        /// When we do a readback we will get the rendered state for the previous time. This is associated with Time.time
        /// at the previous frame. This member stores this value.
        /// </summary>
        float _prevFrameTime = 0f;

        protected virtual void Start()
        {
            _lodComponent = OceanRenderer.Instance.GetComponent<LodDataType>();
            if (OceanRenderer.Instance.CurrentLodCount <= (CanUseLastTwoLODs ? 0 : 1))
            {
                Debug.LogError("No data components of type " + typeof(LodDataType).Name + " found in the scene. Disabling GPU readback.", this);
                enabled = false;
                return;
            }

            SetTextureFormat(_lodComponent.TextureFormat);
        }

        protected virtual void Update()
        {
            UpdateGridSizes();

            ProcessRequestsInternal(true);

            _prevFrameTime = Time.time;
        }

        void UpdateGridSizes()
        {
            if (_settingsProvider != null)
            {
                _settingsProvider.GetMinMaxGridSizes(out _minGridSize, out _maxGridSize);
            }

            // Grid sizes are on powers of two - make sure at least one grid is always included
            _maxGridSize = Mathf.Max(_maxGridSize, 1.999f * _minGridSize);
            Debug.Assert(_maxGridSize > 0f);
        }

        public void ProcessRequests()
        {
            // can process any newly arrived requests but don't queue up new ones
            ProcessRequestsInternal(false);
        }

        void ProcessRequestsInternal(bool runningFromUpdate)
        {
            var ocean = OceanRenderer.Instance;
            int lodCount = ocean.CurrentLodCount;

            // When viewer changes altitude, lods will start/stop updating. Mark ones that are/arent being rendered!
            for (int i = 0; i < _perLodData.KeyArray.Length; i++)
            {
                int lastUsableIndex = CanUseLastTwoLODs ? (lodCount - 1) : (lodCount - 3);

                _perLodData.ValueArray[i]._activelyBeingRendered =
                    _perLodData.KeyArray[i] >= ocean._lods[0]._renderData._texelWidth &&
                    _perLodData.KeyArray[i] <= ocean._lods[lastUsableIndex]._renderData._texelWidth;

                if (!_perLodData.ValueArray[i]._activelyBeingRendered)
                {
                    // It would be cleaner to destroy these. However they contain a NativeArray with a non-negligible amount of data
                    // which we don't want to alloc and dealloc willy nilly, so just mark as invalid by setting time to -1.
                    _perLodData.ValueArray[i]._resultData._time = -1f;
                    _perLodData.ValueArray[i]._resultDataPrevFrame._time = -1f;
                }
            }

            foreach (var lt in ocean._lods)
            {
                // Don't add uninitialised data
                if (lt._renderData._texelWidth == 0f) continue;

                if (lt._renderData._texelWidth >= _minGridSize && (lt._renderData._texelWidth <= _maxGridSize || _maxGridSize == 0f))
                {
                    var tex = _lodComponent.DataTexture(lt.LodIndex);
                    if (tex == null) continue;

                    if (!_perLodData.ContainsKey(lt._renderData._texelWidth))
                    {
                        var resultData = new PerLodData();
                        resultData._resultData = new ReadbackData();
                        resultData._resultDataPrevFrame = new ReadbackData();

                        // create native arrays
                        Debug.Assert(_textureFormat != TexFormat.NotSet, "ReadbackLodData: Texture format must be set.", this);
                        var num = ((int)_textureFormat) * tex.width * tex.height;
                        if (!resultData._resultData._data.IsCreated || resultData._resultData._data.Length != num)
                        {
                            resultData._resultData._data = new NativeArray<ushort>(num, Allocator.Persistent);
                            resultData._resultDataPrevFrame._data = new NativeArray<ushort>(num, Allocator.Persistent);
                        }

                        _perLodData.Add(lt._renderData._texelWidth, resultData);
                    }

                    var lodData = _perLodData[lt._renderData._texelWidth];

                    if (lodData._activelyBeingRendered)
                    {
                        // Only enqueue new requests at beginning of update turns out to be a good time to sample the textures to
                        // ensure everything in the frame is done.
                        if (runningFromUpdate)
                        {
                            EnqueueReadbackRequest(tex, lt._renderData, _prevFrameTime);
                        }

                        ProcessArrivedRequests(lodData);
                    }
                }
            }
        }

        /// <summary>
        /// Request current contents of cameras shape texture. queue pattern inspired by: https://github.com/keijiro/AsyncCaptureTest
        /// </summary>
        void EnqueueReadbackRequest(RenderTexture target, LodTransform.RenderData renderData, float previousFrameTime)
        {
            if (!_doReadback)
            {
                return;
            }

            var lodData = _perLodData[renderData._texelWidth];

            // only queue up requests while time is advancing
            if (previousFrameTime <= lodData._resultData._time)
            {
                return;
            }

            if (lodData._requests.Count < MAX_REQUESTS)
            {
                lodData._requests.Enqueue(
                    new ReadbackRequest
                    {
                        _request = AsyncGPUReadback.Request(target),
                        _renderData = renderData,
                        _time = previousFrameTime,
                    }
                );
            }
        }

        void ProcessArrivedRequests(PerLodData lodData)
        {
            var requests = lodData._requests;

            if (!lodData._activelyBeingRendered)
            {
                // Dump all requests :/. No point processing these, and we have marked the data as being invalid and don't
                // want any new data coming in and stomping the valid flag.
                requests.Clear();
                return;
            }

            // Physics stuff may call update from FixedUpdate() - therefore check if this component was already
            // updated this frame.
            if (lodData._lastUpdateFrame == Time.frameCount)
            {
                return;
            }
            lodData._lastUpdateFrame = Time.frameCount;

            // remove any failed readback requests
            for (int i = 0; i < MAX_REQUESTS && requests.Count > 0; i++)
            {
                var request = requests.Peek();
                if (request._request.hasError)
                {
                    requests.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // process current request queue
            if (requests.Count > 0)
            {
                var request = requests.Peek();
                if (request._request.done)
                {
                    requests.Dequeue();

                    // Eat up any more completed requests to squeeze out latency wherever possible
                    ReadbackRequest nextRequest;
                    while (requests.Count > 0 && (nextRequest = requests.Peek())._request.done)
                    {
                        // Has error will be true if data already destroyed and is therefore unusable
                        if (!nextRequest._request.hasError)
                        {
                            request = nextRequest;
                        }
                        requests.Dequeue();
                    }

                    UnityEngine.Profiling.Profiler.BeginSample("Copy out readback data");

                    var result = lodData._resultData;
                    var resultLast = lodData._resultDataPrevFrame;

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
            foreach (var lodData in _perLodData.Values)
            {
                if (lodData == null || lodData._resultData == null) continue;
                if (lodData._resultData._data.IsCreated) lodData._resultData._data.Dispose();
                if (lodData._resultDataPrevFrame._data.IsCreated) lodData._resultDataPrevFrame._data.Dispose();
            }

            _perLodData.Clear();
        }

        public class ReadbackData
        {
            public NativeArray<ushort> _data;
            public LodTransform.RenderData _renderData;
            public float _time;

            public bool Valid { get { return _time >= 0f; } }

            public bool SampleARGB16(ref Vector3 i_worldPos, out Vector3 o_displacement)
            {
                if (!Valid) { o_displacement = Vector3.zero; return false; }

                float xOffset = i_worldPos.x - _renderData._posSnapped.x;
                float zOffset = i_worldPos.z - _renderData._posSnapped.z;
                float r = _renderData._texelWidth * _renderData._textureRes / 2f;
                if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
                {
                    // outside of this collision data
                    o_displacement = Vector3.zero;
                    return false;
                }

                var u = 0.5f + 0.5f * xOffset / r;
                var v = 0.5f + 0.5f * zOffset / r;
                var x = Mathf.FloorToInt(u * _renderData._textureRes);
                var y = Mathf.FloorToInt(v * _renderData._textureRes);
                var idx = 4 * (y * (int)_renderData._textureRes + x);

                o_displacement.x = Mathf.HalfToFloat(_data[idx + 0]);
                o_displacement.y = Mathf.HalfToFloat(_data[idx + 1]);
                o_displacement.z = Mathf.HalfToFloat(_data[idx + 2]);

                return true;
            }

            public bool InterpolateARGB16(ref Vector3 i_worldPos, out Vector3 o_displacement)
            {
                if (!Valid) { o_displacement = Vector3.zero; return false; }

                float xOffset = i_worldPos.x - _renderData._posSnapped.x;
                float zOffset = i_worldPos.z - _renderData._posSnapped.z;
                float r = _renderData._texelWidth * _renderData._textureRes / 2f;
                if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
                {
                    // outside of this collision data
                    o_displacement = Vector3.zero;
                    return false;
                }

                var u = 0.5f + 0.5f * xOffset / r;
                var v = 0.5f + 0.5f * zOffset / r;
                float u_texels = Mathf.Max(u * _renderData._textureRes - 0.5f, 0f);
                float v_texels = Mathf.Max(v * _renderData._textureRes - 0.5f, 0f);

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
                o_displacement.x = Mathf.Lerp(Mathf.Lerp(x00, x01, xf), Mathf.Lerp(x10, x11, xf), zf);
                o_displacement.y = Mathf.Lerp(Mathf.Lerp(y00, y01, xf), Mathf.Lerp(y10, y11, xf), zf);
                o_displacement.z = Mathf.Lerp(Mathf.Lerp(z00, z01, xf), Mathf.Lerp(z10, z11, xf), zf);

                return true;
            }

            public bool SampleRG16(ref Vector3 i_worldPos, out Vector2 flow)
            {
                if (!Valid) { flow = Vector2.zero; return false; }

                float xOffset = i_worldPos.x - _renderData._posSnapped.x;
                float zOffset = i_worldPos.z - _renderData._posSnapped.z;
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

        /// <summary>
        /// Returns result of GPU readback of a LOD data. Do not hold onto the returned reference across frames.
        /// </summary>
        protected PerLodData GetData(float gridSize)
        {
            return _perLodData[gridSize];
        }

        /// <summary>
        /// Returns result of GPU readback of a LOD data. Do not hold onto the returned reference across frames.
        /// </summary>
        protected PerLodData GetData(Rect sampleAreaXZ, float minSpatialLength)
        {
            PerLodData lastCandidate = null;

            for (int i = 0; i < _perLodData.KeyArray.Length; i++)
            {
                var lodData = _perLodData.ValueArray[i];
                if (!lodData._activelyBeingRendered || lodData._resultData._time == -1f)
                {
                    continue;
                }

                // Check that the region of interest is covered by this data
                var wdcRect = lodData._resultData._renderData.RectXZ;
                // Shrink rect by 1 texel border - this is to make finite differences fit as well
                float texelWidth = _perLodData.KeyArray[i];
                wdcRect.x += texelWidth; wdcRect.y += texelWidth;
                wdcRect.width -= 2f * texelWidth; wdcRect.height -= 2f * texelWidth;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                {
                    continue;
                }

                // This data covers our required area, so store it as a potential candidate
                lastCandidate = lodData;

                // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
                // in the last LOD - then this is the best we can do.
                float minWavelength = texelWidth * OceanRenderer.Instance._minTexelsPerWave;
                if (minSpatialLength / minWavelength > 2f)
                {
                    continue;
                }

                // A good match - return immediately
                return lodData;
            }

            // We didnt get a perfect match, but pick the next best candidate
            return lastCandidate;
        }

        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData)
        {
            Debug.Assert(i_samplingData._minSpatialLength >= 0f && i_samplingData._tag != null);

            var sampleAreaXZ = new Rect(i_worldPos.x, i_worldPos.z, 0f, 0f);

            bool oneWasInRect = false;
            bool wavelengthsLargeEnough = false;

            foreach (var gridSize_lodData in _perLodData)
            {
                if (!gridSize_lodData.Value._activelyBeingRendered || gridSize_lodData.Value._resultData._time == -1f)
                {
                    continue;
                }

                // Check that the region of interest is covered by this data
                var wdcRect = gridSize_lodData.Value._resultData._renderData.RectXZ;
                // Shrink rect by 1 texel border - this is to make finite differences fit as well
                float texelWidth = gridSize_lodData.Key;
                wdcRect.x += texelWidth; wdcRect.y += texelWidth;
                wdcRect.width -= 2f * texelWidth; wdcRect.height -= 2f * texelWidth;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                {
                    continue;
                }
                oneWasInRect = true;

                // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
                // in the last LOD - then this is the best we can do.
                float minWavelength = texelWidth * OceanRenderer.Instance._minTexelsPerWave;
                if (i_samplingData._minSpatialLength / minWavelength > 2f)
                {
                    continue;
                }
                wavelengthsLargeEnough = true;

                return AvailabilityResult.DataAvailable;
            }

            if (!oneWasInRect)
            {
                return AvailabilityResult.NoDataAtThisPosition;
            }
            if (!wavelengthsLargeEnough)
            {
                return AvailabilityResult.NoLODsBigEnoughToFilterOutWavelengths;
            }
            // Should not get here.
            return AvailabilityResult.ValidationFailed;
        }

        public void GetStats(out int count, out int minQueueLength, out int maxQueueLength)
        {
            minQueueLength = MAX_REQUESTS;
            maxQueueLength = 0;
            count = 0;

            foreach (var gridSize_lodData in _perLodData)
            {
                if (!gridSize_lodData.Value._activelyBeingRendered)
                {
                    continue;
                }

                count++;

                var queueLength = gridSize_lodData.Value._requests.Count;
                minQueueLength = Mathf.Min(queueLength, minQueueLength);
                maxQueueLength = Mathf.Max(queueLength, maxQueueLength);
            }

            if (minQueueLength == MAX_REQUESTS) minQueueLength = -1;
            if (maxQueueLength == 0) maxQueueLength = -1;
        }

        public bool GetSamplingData(ref Rect i_displacedSamplingArea, float i_minSpatialLength, SamplingData o_samplingData)
        {
            o_samplingData._minSpatialLength = i_minSpatialLength;

            Rect undisplacedRect = new Rect(
                i_displacedSamplingArea.xMin - OceanRenderer.Instance.MaxHorizDisplacement,
                i_displacedSamplingArea.yMin - OceanRenderer.Instance.MaxHorizDisplacement,
                i_displacedSamplingArea.width + 2f * OceanRenderer.Instance.MaxHorizDisplacement,
                i_displacedSamplingArea.height + 2f * OceanRenderer.Instance.MaxHorizDisplacement
                );
            o_samplingData._tag = GetData(undisplacedRect, i_minSpatialLength);

            return o_samplingData._tag != null;
        }

        public void ReturnSamplingData(SamplingData i_data)
        {
            i_data._tag = null;
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            foreach (var pld in _perLodData)
            {
                pld.Value._resultData._renderData._posSnapped -= newOrigin;
                pld.Value._resultDataPrevFrame._renderData._posSnapped -= newOrigin;

                // manually update each request
                Queue<ReadbackRequest> newRequests = new Queue<ReadbackRequest>();
                while (pld.Value._requests.Count > 0)
                {
                    var req = pld.Value._requests.Dequeue();
                    req._renderData._posSnapped -= newOrigin;
                    newRequests.Enqueue(req);
                }
                pld.Value._requests = newRequests;
            }
        }
    }
}
