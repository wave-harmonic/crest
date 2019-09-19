// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CollProviderCompute : MonoBehaviour
{
    public ComputeShader _shader;

    readonly static int s_maxQueryCount = 4096;
    static int s_kernelHandle;

    ComputeBuffer _computeBufQueries;
    ComputeBuffer _computeBufResults;

    Dictionary<int, List<Vector3>> _queries = new Dictionary<int, List<Vector3>>();

    NativeArray<Vector3> _queryResults;

    bool _doQuery = true;

    public void Query(int guid, Vector3 queryPosition)
    {
        List<Vector3> queries;
        if (!_queries.TryGetValue(guid, out queries))
        {
            _queries.Add(guid, queries = new List<Vector3>());
        }

        queries.Add(queryPosition);
    }

    void LateUpdate()
    {
        if (_doQuery)
        {
            Debug.Log("LateUpdate");

            var queries = new Vector3[s_maxQueryCount];
            for (int i = 0; i < s_maxQueryCount; i++)
            {
                queries[i] = new Vector3(i, i + 1, 2 * i);
            }

            _computeBufQueries.SetData(queries);

            _shader.SetBuffer(s_kernelHandle, "_QueryPositions", _computeBufQueries);
            _shader.SetBuffer(s_kernelHandle, "_ResultDisplacements", _computeBufResults);
            _shader.Dispatch(s_kernelHandle, s_maxQueryCount / 64, 1, 1);

            AsyncGPUReadback.Request(_computeBufResults, DataArrived);

            Debug.Log(Time.frameCount + ": request created");

            _doQuery = false;
        }
    }

    void DataArrived(AsyncGPUReadbackRequest req)
    {
        if (req.done)
        {
            var data = req.GetData<Vector3>();
            data.CopyTo(_queryResults);
        }

        Debug.Log(Time.frameCount + ": queryResult: " + _queryResults[0]);
    }

    private void OnEnable()
    {
        s_kernelHandle = _shader.FindKernel("CSMain");

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
