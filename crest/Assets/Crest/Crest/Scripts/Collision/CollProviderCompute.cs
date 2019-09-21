// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// TODO min shape length

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CollProviderCompute : MonoBehaviour
{
    readonly static int s_maxRequests = 4;
    readonly static int s_maxGuids = 64;

    public ComputeShader _shader;
    Crest.PropertyWrapperComputeStandalone _wrapper;

    readonly static int s_maxQueryCount = 4096;
    // Must match value in compute shader
    readonly static int s_computeGroupSize = 64;
    public static bool s_useComputeCollQueries = true;

    static int s_kernelHandle;

    ComputeBuffer _computeBufQueries;
    ComputeBuffer _computeBufResults;

    Vector2[] _queryPositionsXZ = new Vector2[s_maxQueryCount];

    class SegmentRegistrar
    {
        public Dictionary<int, Vector2Int> _segments = new Dictionary<int, Vector2Int>();
        public int _numQueries = 0;
    }

    class SegmentRegistrarQueue
    {
        // Requests in flight plus 2 held values, plus one current
        readonly static int s_poolSize = s_maxRequests + 2 + 1;

        SegmentRegistrar[] _segments = new SegmentRegistrar[s_poolSize];

        public int _segmentRelease = 0;
        public int _segmentAcquire = 0;

        public SegmentRegistrar Current => _segments[_segmentAcquire];

        public SegmentRegistrarQueue()
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i] = new SegmentRegistrar();
            }
        }

        public void AcquireNew()
        {
            _segmentAcquire = (_segmentAcquire + 1) % _segments.Length;

            // The last index should never increment and land on the first index - it should only happen the other way around.
            Debug.Assert(_segmentAcquire != _segmentRelease, "Segment registrar scratch exhausted.");
        }

        public void ReleaseLast()
        {
            _segmentRelease = (_segmentRelease + 1) % _segments.Length;
        }

        public void RemoveRegistrations(int key)
        {
            // Remove the guid for all of the next spare segment registrars. However, don't touch the ones that are being
            // used for active requests.
            int i = _segmentAcquire;
            while (true)
            {
                if (_segments[i]._segments.ContainsKey(key))
                {
                    _segments[i]._segments.Remove(key);
                }

                i = (i + 1) % _segments.Length;

                if (i == _segmentRelease)
                {
                    break;
                }
            }
        }

        public void ClearAvailable()
        {
            // Extreme approach - flush all segments for next spare registrars (but don't touch ones being used for active requests)
            int i = _segmentAcquire;
            while (true)
            {
                _segments[i]._segments.Clear();
                _segments[i]._numQueries = 0;

                i = (i + 1) % _segments.Length;

                if (i == _segmentRelease)
                {
                    break;
                }
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i]._numQueries = 0;
                _segments[i]._segments.Clear();
            }
        }
    }

    SegmentRegistrarQueue _srq = new SegmentRegistrarQueue();
    

    NativeArray<Vector3> _queryResults;
    float _queryResultsTime = -1f;
    Dictionary<int, Vector2Int> _resultSegments;

    NativeArray<Vector3> _queryResultsLast;
    float _queryResultsTimeLast = -1f;
    Dictionary<int, Vector2Int> _resultSegmentsLast;

    public static CollProviderCompute Instance { get; private set; }

    struct ReadbackRequest
    {
        public AsyncGPUReadbackRequest _request;
        public float _dataTimestamp;
        public Dictionary<int, Vector2Int> _segments;
    }

    List<ReadbackRequest> _requests = new List<ReadbackRequest>();

    /// <summary>
    /// Takes a unique request ID and some world space XZ positions, and computes the displacement vector that lands at this position,
    /// to a good approximation. The world space height of the water at that position is then SeaLevel + displacement.y.
    /// </summary>
    public bool UpdateQueryPoints(int guid, Vector3[] queryPoints)
    {
        var segmentRetrieved = false;
        Vector2Int segment;

        if (_srq.Current._segments.TryGetValue(guid, out segment))
        {
            var segmentSize = segment.y - segment.x + 1;
            if (segmentSize == queryPoints.Length)
            {
                segmentRetrieved = true;
            }
            else
            {
                _srq.Current._segments.Remove(guid);
            }
        }

        if (!segmentRetrieved)
        {
            if (_srq.Current._segments.Count >= s_maxGuids)
            {
                Debug.LogError("Too many guids registered with CollProviderCompute. Increase s_maxGuids.", this);
                return false;
            }

            segment.x = _srq.Current._numQueries;
            segment.y = segment.x + queryPoints.Length - 1;
            _srq.Current._segments.Add(guid, segment);

            _srq.Current._numQueries += queryPoints.Length;

            //Debug.Log("Added points for " + guid);
        }

        for (int i = segment.x; i <= segment.y; i++)
        {
            _queryPositionsXZ[i].x = queryPoints[i - segment.x].x;
            _queryPositionsXZ[i].y = queryPoints[i - segment.x].z;
        }

        return true;
    }

    /// <summary>
    /// Signal that we're no longer servicing queries. Note this leaves an air bubble in the query buffer.
    /// </summary>
    public void RemoveQueryPoints(int guid)
    {
        _srq.RemoveRegistrations(guid);
    }

    /// <summary>
    /// Remove air bubbles from the query array. Currently this lazily just nukes all the registered
    /// query IDs so they'll be recreated next time (generating garbage). TODO..
    /// </summary>
    public void CompactQueryStorage()
    {
        _srq.ClearAvailable();
    }

    /// <summary>
    /// Copy out the result displacements.
    /// </summary>
    public bool RetrieveResults(int guid, ref Vector3[] results)
    {
        if (_resultSegments == null)
        {
            return false;
        }

        // Check if there are results that came back for this guid
        Vector2Int segment;
        if (!_resultSegments.TryGetValue(guid, out segment))
        {
            // Guid not found - no result
            return false;
        }

        _queryResults.Slice(segment.x, segment.y - segment.x + 1).CopyTo(results);

        return true;
    }

    /// <summary>
    /// Copy out just water heights
    /// </summary>
    public bool RetrieveResultHeights(int guid, ref float[] heights)
    {
        if (_resultSegments == null)
        {
            return false;
        }

        // Check if there are results that came back for this guid
        Vector2Int segment;
        if (!_resultSegments.TryGetValue(guid, out segment))
        {
            // Guid not found - no result
            return false;
        }

        var seaLevel = Crest.OceanRenderer.Instance.SeaLevel;
        for(int i = segment.x; i <= segment.y; i++)
        {
            heights[i - segment.x] = seaLevel + _queryResults[i].y;
        }

        return true;
    }

    /// <summary>
    /// Compute time derivative of the displacements by calculating difference from last query. More complicated than it would seem - results
    /// may not be available in one or both of the results, or the query locations in the array may change.
    /// </summary>
    public bool ComputeVelocities(int guid, ref Vector3[] results)
    {
        // Need at least 2 returned results to do finite difference
        if (_queryResultsTime < 0f || _queryResultsTimeLast < 0f)
        {
            return false;
        }

        Vector2Int segment;
        if (!_resultSegments.TryGetValue(guid, out segment))
        {
            return false;
        }

        Vector2Int segmentLast;
        if (!_resultSegmentsLast.TryGetValue(guid, out segmentLast))
        {
            return false;
        }

        if ((segment.y - segment.x) != (segmentLast.y - segmentLast.x))
        {
            // Number of queries changed - can't handle that
            return false;
        }

        var dt = _queryResultsTime - _queryResultsTimeLast;
        if (dt < 0.0001f)
        {
            return false;
        }

        var count = segment.y - segment.x + 1;
        for (var i = 0; i < count; i++)
        {
            results[i] = (_queryResults[i + segment.x] - _queryResultsLast[i + segmentLast.x]) / dt;
        }

        return true;
    }

    // This needs to run before OceanRenderer.LateUpdate, because the latter will change the LOD positions/scales, while we will read
    // the last frames displacements.
    void Update()
    {
        if (_srq.Current._numQueries > 0)
        {
            _computeBufQueries.SetData(_queryPositionsXZ, 0, 0, _srq.Current._numQueries);

            _shader.SetBuffer(s_kernelHandle, "_QueryPositions", _computeBufQueries);
            _shader.SetBuffer(s_kernelHandle, "_ResultDisplacements", _computeBufResults);

            Crest.OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_wrapper);

            _shader.SetTexture(s_kernelHandle, "_LD_TexArray_AnimatedWaves", Crest.OceanRenderer.Instance._lodDataAnimWaves.DataTexture);

            // LOD 0 is blended in/out when scale changes, to eliminate pops
            var needToBlendOutShape = Crest.OceanRenderer.Instance.ScaleCouldIncrease;
            var meshScaleLerp = needToBlendOutShape ? Crest.OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;
            _shader.SetFloat("_MeshScaleLerp", meshScaleLerp);

            var numGroups = (int)Mathf.Ceil((float)_srq.Current._numQueries / (float)s_computeGroupSize) * s_computeGroupSize;
            _shader.Dispatch(s_kernelHandle, numGroups, 1, 1);

            // Remove oldest requests if we have hit the limit
            while (_requests.Count >= s_maxQueryCount)
            {
                _requests.RemoveAt(0);
            }

            ReadbackRequest request;
            request._dataTimestamp = Time.time - Time.deltaTime;
            request._request = AsyncGPUReadback.Request(_computeBufResults, DataArrived);
            request._segments = _srq.Current._segments;

            _requests.Add(request);
            //Debug.Log(Time.frameCount + ": request created for " + _numQueries + " queries.");

            _srq.AcquireNew();
        }
    }

    /// <summary>
    /// Called when a compute buffer has been read back from the GPU to the CPU.
    /// </summary>
    void DataArrived(AsyncGPUReadbackRequest req)
    {
        // Can get callbacks after disable, so detect this.
        if (!_queryResults.IsCreated)
        {
            _requests.Clear();
            return;
        }

        // Remove any error requests
        for (int i = _requests.Count - 1; i >= 0; --i)
        {
            if (_requests[i]._request.hasError)
            {
                _requests.RemoveAt(i);
                _srq.ReleaseLast();
            }
        }

        // Find the last request that was completed
        var lastDoneIndex = _requests.Count - 1;
        for (; lastDoneIndex >= 0 && !_requests[lastDoneIndex]._request.done; --lastDoneIndex)
        {
        }

        // If there is a completed request, process it
        if (lastDoneIndex >= 0)
        {
            // Update "last" results
            Swap(ref _queryResults, ref _queryResultsLast);
            _queryResultsTimeLast = _queryResultsTime;
            _resultSegmentsLast = _resultSegments;

            var data = _requests[lastDoneIndex]._request.GetData<Vector3>();
            data.CopyTo(_queryResults);
            _queryResultsTime = _requests[lastDoneIndex]._dataTimestamp;
            _resultSegments = _requests[lastDoneIndex]._segments;
        }

        // Remove all the requests up to the last completed one
        for (int i = lastDoneIndex; i >= 0; --i)
        {
            _requests.RemoveAt(i);
            _srq.ReleaseLast();
        }
    }

    void Swap<T>(ref T a, ref T b)
    {
        var temp = b;
        b = a;
        a = temp;
    }

    private void OnEnable()
    {
        Debug.Assert(Instance == null);
        Instance = this;

        s_kernelHandle = _shader.FindKernel("CSMain");
        _wrapper = new Crest.PropertyWrapperComputeStandalone(_shader, s_kernelHandle);

        _computeBufQueries = new ComputeBuffer(s_maxQueryCount, 8, ComputeBufferType.Default);
        _computeBufResults = new ComputeBuffer(s_maxQueryCount, 12, ComputeBufferType.Default);

        _queryResults = new NativeArray<Vector3>(s_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _queryResultsLast = new NativeArray<Vector3>(s_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    }

    private void OnDisable()
    {
        Instance = null;

        _computeBufQueries.Dispose();
        _computeBufResults.Dispose();

        _queryResults.Dispose();
        _queryResultsLast.Dispose();

        _srq.ClearAll();
    }

    void PlaceMarkerCube(ref GameObject marker, Vector3 query, Vector3 disp)
    {
        if (marker == null)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(marker.GetComponent<Collider>());
        }

        query.y = 0f;

        Debug.DrawLine(query, query + disp);
        marker.transform.position = query + disp;
    }
}
