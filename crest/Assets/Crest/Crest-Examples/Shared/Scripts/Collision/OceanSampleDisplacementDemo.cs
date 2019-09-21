// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

/// <summary>
/// Attach this script to any GameObject and it will create three collision probes in front of the camera
/// </summary>
public class OceanSampleDisplacementDemo : MonoBehaviour
{
    public bool _trackCamera = true;

    GameObject[] _markerObjects = new GameObject[3];
    Vector3[] _markerPos = new Vector3[3];
    Vector3[] _resultDisps = new Vector3[3];
    Vector3[] _resultNorms = new Vector3[3];
    Vector3[] _resultVels = new Vector3[3];

    SamplingData _samplingData = new SamplingData();

    void Update()
    {
        float r = 5f;
        if (_trackCamera)
        {
            var height = Mathf.Abs(Camera.main.transform.position.y - OceanRenderer.Instance.SeaLevel);
            var lookAngle = Mathf.Max(Mathf.Abs(Camera.main.transform.forward.y), 0.001f);
            var offset = height / lookAngle;
            _markerPos[0] = Camera.main.transform.position + Camera.main.transform.forward * offset;
            _markerPos[1] = Camera.main.transform.position + Camera.main.transform.forward * offset + r * Vector3.right;
            _markerPos[2] = Camera.main.transform.position + Camera.main.transform.forward * offset + r * Vector3.forward;
        }

        if (CollProviderCompute.Instance == null || OceanRenderer.Instance == null)
        {
            return;
        }

        var status = CollProviderCompute.Instance.Query(GetInstanceID(), _markerPos, _markerPos, _resultDisps, _resultNorms);

        if (CollProviderCompute.RetrieveSucceeded(status))
        {
            for (int i = 0; i < _resultDisps.Length; i++)
            {
                if (_markerObjects[i] == null)
                {
                    _markerObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(_markerObjects[i].GetComponent<Collider>());
                    _markerObjects[i].transform.localScale = Vector3.one * 0.5f;
                }

                var query = _markerPos[i];
                query.y = OceanRenderer.Instance.SeaLevel;

                var disp = _resultDisps[i];

                var pos = query;
                pos.y = disp.y;
                Debug.DrawLine(pos, pos - disp);
                _markerObjects[i].transform.position = pos;
            }

            for (var i = 0; i < _resultNorms.Length; i++)
            {
                Debug.DrawLine(_markerObjects[i].transform.position, _markerObjects[i].transform.position + _resultNorms[i], Color.blue);
            }
        }

        if (CollProviderCompute.Instance.ComputeVelocities(GetInstanceID(), ref _resultVels))
        {
            for (var i = 0; i < _resultVels.Length; i++)
            {
                Debug.DrawLine(_markerObjects[i].transform.position, _markerObjects[i].transform.position + _resultVels[i], Color.green);
            }
        }
    }
}
