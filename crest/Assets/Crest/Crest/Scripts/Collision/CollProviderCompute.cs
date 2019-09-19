// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CollProviderCompute : MonoBehaviour
{
    public ComputeShader _shader;
    Crest.PropertyWrapperComputeStandalone _wrapper;

    readonly static int s_maxQueryCount = 4096;
    // Must match value in compute shader
    readonly static int s_computeGroupSize = 64;

    static int s_kernelHandle;

    ComputeBuffer _computeBufQueries;
    ComputeBuffer _computeBufResults;

    Vector3[] _queryPositions = new Vector3[s_maxQueryCount];

    Dictionary<int, Vector2Int> _segments = new Dictionary<int, Vector2Int>();

    NativeArray<Vector3> _queryResults;

    int _numQueries = 0;

    public bool UpdateQueryPoints(int guid, Vector3[] queryPoints)
    {
        var segmentRetrieved = false;
        Vector2Int segment;

        if (_segments.TryGetValue(guid, out segment))
        {
            var segmentSize = segment.y - segment.x + 1;
            if (segmentSize == queryPoints.Length)
            {
                segmentRetrieved = true;
            }
            else
            {
                _segments.Remove(guid);
            }
        }

        if (!segmentRetrieved)
        {
            segment.x = _numQueries;
            segment.y = segment.x + queryPoints.Length - 1;
            _segments.Add(guid, segment);

            _numQueries += queryPoints.Length;
        }


        for (int i = segment.x; i <= segment.y; i++)
        {
            _queryPositions[i] = queryPoints[i - segment.x];
        }

        return true;
    }

    public void RemoveQueryPoints(int guid)
    {
        if (_segments.ContainsKey(guid))
        {
            _segments.Remove(guid);
        }
    }

    public void CompactQueryStorage()
    {
        // Extreme approach - flush all segments, which will force them to recreate
        _segments.Clear();
        _numQueries = 0;
    }

    private void Update()
    {
        UpdateQueryPoints(GetInstanceID(), new Vector3[] { Vector3.zero, Vector3.forward });
    }

    void LateUpdate()
    {
        if (_numQueries > 0)
        {
            _computeBufQueries.SetData(_queryPositions, 0, 0, _numQueries);

            _shader.SetBuffer(s_kernelHandle, "_QueryPositions", _computeBufQueries);
            _shader.SetBuffer(s_kernelHandle, "_ResultDisplacements", _computeBufResults);

            Crest.OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_wrapper);

            _shader.SetTexture(s_kernelHandle, "_LD_TexArray_AnimatedWaves", Crest.OceanRenderer.Instance._lodDataAnimWaves.DataTexture);

            var numGroups = (int)Mathf.Ceil((float)_numQueries / (float)s_computeGroupSize) * s_computeGroupSize;
            _shader.Dispatch(s_kernelHandle, numGroups, 1, 1);

            AsyncGPUReadback.Request(_computeBufResults, DataArrived);

            Debug.Log(Time.frameCount + ": request created for " + _numQueries + " queries.");
        }
    }

    void DataArrived(AsyncGPUReadbackRequest req)
    {
        if (req.done && _queryResults.IsCreated)
        {
            var data = req.GetData<Vector3>();
            data.CopyTo(_queryResults);

            Debug.Log(Time.frameCount + ": queryResult: " + _queryResults[0]);
        }
    }

    private void OnEnable()
    {
        s_kernelHandle = _shader.FindKernel("CSMain");
        _wrapper = new Crest.PropertyWrapperComputeStandalone(_shader, s_kernelHandle);

        _computeBufQueries = new ComputeBuffer(s_maxQueryCount, 12, ComputeBufferType.Default);
        _computeBufResults = new ComputeBuffer(s_maxQueryCount, 12, ComputeBufferType.Default);

        _queryResults = new NativeArray<Vector3>(s_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    }

    private void OnDisable()
    {
        _computeBufQueries.Dispose();
        _computeBufResults.Dispose();

        _queryResults.Dispose();
    }
}
