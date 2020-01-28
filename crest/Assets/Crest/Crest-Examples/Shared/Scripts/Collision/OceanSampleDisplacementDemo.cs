// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Attach this script to any GameObject and it will create three collision probes in front of the camera
/// </summary>
public class OceanSampleDisplacementDemo : MonoBehaviour
{
    public bool _trackCamera = true;

    [Range(0f, 32f)]
    public float _minGridSize = 0f;

    GameObject[] _markerObjects = new GameObject[3];



    float _samplesRadius = 5f;

    void Update()
    {

        if (OceanRenderer.Instance == null)
        {
            return;
        }

        NativeArray<Vector3> markerPos = new NativeArray<Vector3>(3, Allocator.Temp);
        NativeArray<Vector3> resultDisps = new NativeArray<Vector3>(3, Allocator.Temp);
        NativeArray<Vector3> resultNorms = new NativeArray<Vector3>(3, Allocator.Temp);
        NativeArray<Vector3> resultVels = new NativeArray<Vector3>(3, Allocator.Temp);

        if (_trackCamera)
        {
            var height = Mathf.Abs(Camera.main.transform.position.y - OceanRenderer.Instance.SeaLevel);
            var lookAngle = Mathf.Max(Mathf.Abs(Camera.main.transform.forward.y), 0.001f);
            var offset = height / lookAngle;
            markerPos[0] = Camera.main.transform.position + Camera.main.transform.forward * offset;
            markerPos[1] = Camera.main.transform.position + Camera.main.transform.forward * offset + _samplesRadius * Vector3.right;
            markerPos[2] = Camera.main.transform.position + Camera.main.transform.forward * offset + _samplesRadius * Vector3.forward;
        }


        var collProvider = OceanRenderer.Instance.CollisionProvider;

        var status = collProvider.Query(GetHashCode(), _minGridSize, markerPos, resultDisps, resultNorms, resultVels);

        if (collProvider.RetrieveSucceeded(status))
        {
            for (int i = 0; i < resultDisps.Length; i++)
            {
                if (_markerObjects[i] == null)
                {
                    _markerObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(_markerObjects[i].GetComponent<Collider>());
                    _markerObjects[i].transform.localScale = Vector3.one * 0.5f;
                }

                var query = markerPos[i];
                query.y = OceanRenderer.Instance.SeaLevel;

                var disp = resultDisps[i];

                var pos = query;
                pos.y = disp.y;
                Debug.DrawLine(pos, pos - disp);
                _markerObjects[i].transform.position = pos;

                _markerObjects[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, resultNorms[i]);
            }

            for (var i = 0; i < resultNorms.Length; i++)
            {
                Debug.DrawLine(_markerObjects[i].transform.position, _markerObjects[i].transform.position + resultNorms[i], Color.blue);
            }

            for (var i = 0; i < resultVels.Length; i++)
            {
                Debug.DrawLine(_markerObjects[i].transform.position, _markerObjects[i].transform.position + resultVels[i], Color.green);
            }
        }


        resultVels.Dispose();
        resultNorms.Dispose();
        resultDisps.Dispose();
        markerPos.Dispose();
    }
}
