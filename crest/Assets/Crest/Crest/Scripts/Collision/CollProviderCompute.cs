// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Potential improvements
// - Min shape length
// - Half return values

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CollProviderCompute : MonoBehaviour
{
    readonly static string s_shaderName = "ProcessCollisionQueries";
    readonly static string s_kernelName = "CSMain";
    readonly static int s_maxRequests = 4;
    readonly static int s_maxGuids = 64;

    ComputeShader _shaderProcessQueries;
    Crest.PropertyWrapperComputeStandalone _wrapper;

    readonly static int s_maxQueryCount = 4096;
    // Must match value in compute shader
    readonly static int s_computeGroupSize = 64;
    public static bool s_useComputeCollQueries = true;

    readonly static int sp_queryPositions = Shader.PropertyToID("_QueryPositions");
    readonly static int sp_ResultDisplacements = Shader.PropertyToID("_ResultDisplacements");
    readonly static int sp_LD_TexArray_AnimatedWaves = Shader.PropertyToID("_LD_TexArray_AnimatedWaves");
    readonly static int sp_MeshScaleLerp = Shader.PropertyToID("_MeshScaleLerp");
    readonly static int sp_SliceCount = Shader.PropertyToID("_SliceCount");
    
    readonly static float s_finiteDiffDx = 0.1f;

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
    public bool UpdateQueryPoints(int guid, Vector3[] queryPoints, Vector3[] queryNormals)
    {
        var segmentRetrieved = false;
        Vector2Int segment;

        // We'll send in 3 points to get normals
        var countPts = (queryPoints != null ? queryPoints.Length : 0);
        var countNorms = (queryNormals != null ? queryNormals.Length : 0);
        var countTotal = countPts + countNorms * 3;

        if (_srq.Current._segments.TryGetValue(guid, out segment))
        {
            var segmentSize = segment.y - segment.x + 1;
            if (segmentSize == countTotal)
            {
                segmentRetrieved = true;
            }
            else
            {
                _srq.Current._segments.Remove(guid);
            }
        }

        if (countTotal == 0)
        {
            // No query data
            return false;
        }

        if (!segmentRetrieved)
        {
            if (_srq.Current._segments.Count >= s_maxGuids)
            {
                Debug.LogError("Too many guids registered with CollProviderCompute. Increase s_maxGuids.", this);
                return false;
            }

            segment.x = _srq.Current._numQueries;
            segment.y = segment.x + countTotal - 1;
            _srq.Current._segments.Add(guid, segment);

            _srq.Current._numQueries += countTotal;

            //Debug.Log("Added points for " + guid);
        }

        for (int pointi = 0; pointi < countPts; pointi++)
        {
            _queryPositionsXZ[pointi + segment.x].x = queryPoints[pointi].x;
            _queryPositionsXZ[pointi + segment.x].y = queryPoints[pointi].z;
        }

        // To compute each normal, post 3 query points
        for (int normi = 0; normi < countNorms; normi++)
        {
            var arrIdx = segment.x + countPts + 3 * normi;

            _queryPositionsXZ[arrIdx + 0].x = queryNormals[normi].x;
            _queryPositionsXZ[arrIdx + 0].y = queryNormals[normi].z;
            _queryPositionsXZ[arrIdx + 1].x = queryNormals[normi].x + s_finiteDiffDx;
            _queryPositionsXZ[arrIdx + 1].y = queryNormals[normi].z;
            _queryPositionsXZ[arrIdx + 2].x = queryNormals[normi].x;
            _queryPositionsXZ[arrIdx + 2].y = queryNormals[normi].z + s_finiteDiffDx;
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
    /// Copy out the result displacements and normals, if queried.
    /// </summary>
    public bool RetrieveResults(int guid, Vector3[] disps, Vector3[] normals)
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

        var countPts = (disps != null ? disps.Length : 0);
        var countNorms = (normals != null ? normals.Length : 0);
        var countTotal = countPts + countNorms * 3;

        if (countPts > 0)
        {
            _queryResults.Slice(segment.x, countPts).CopyTo(disps);
        }

        if (countNorms > 0)
        {
            int firstNorm = segment.x + countPts;

            var dx = -Vector3.right * s_finiteDiffDx;
            var dz = -Vector3.forward * s_finiteDiffDx;
            for (int i = 0; i < countNorms; i++)
            {
                var p = _queryResults[firstNorm + 3 * i + 0];
                var px = dx + _queryResults[firstNorm + 3 * i + 1];
                var pz = dz + _queryResults[firstNorm + 3 * i + 2];

                normals[i] = Vector3.Cross(p - px, p - pz).normalized;
                normals[i].y *= -1f;
            }
        }

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
        for (int i = segment.x; i <= segment.y; i++)
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

        var count = results.Length;
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

            _shaderProcessQueries.SetBuffer(s_kernelHandle, sp_queryPositions, _computeBufQueries);
            _shaderProcessQueries.SetBuffer(s_kernelHandle, sp_ResultDisplacements, _computeBufResults);

            Crest.OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_wrapper);

            _shaderProcessQueries.SetTexture(s_kernelHandle, sp_LD_TexArray_AnimatedWaves, Crest.OceanRenderer.Instance._lodDataAnimWaves.DataTexture);

            // LOD 0 is blended in/out when scale changes, to eliminate pops
            var needToBlendOutShape = Crest.OceanRenderer.Instance.ScaleCouldIncrease;
            var meshScaleLerp = needToBlendOutShape ? Crest.OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;
            _shaderProcessQueries.SetFloat(sp_MeshScaleLerp, meshScaleLerp);

            _shaderProcessQueries.SetFloat(sp_SliceCount, Crest.OceanRenderer.Instance.CurrentLodCount);

            var numGroups = (int)Mathf.Ceil((float)_srq.Current._numQueries / (float)s_computeGroupSize) * s_computeGroupSize;
            _shaderProcessQueries.Dispatch(s_kernelHandle, numGroups, 1, 1);

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

        _shaderProcessQueries = Resources.Load<ComputeShader>(s_shaderName);
        s_kernelHandle = _shaderProcessQueries.FindKernel(s_kernelName);
        _wrapper = new Crest.PropertyWrapperComputeStandalone(_shaderProcessQueries, s_kernelHandle);

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
