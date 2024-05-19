﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
using UnityEngine;

namespace Crest.Examples
{
    /// <summary>
    /// Attach this script to any GameObject and it will create three collision probes in front of the camera
    /// </summary>
    [AddComponentMenu(Crest.Internal.Constants.MENU_PREFIX_EXAMPLE + "Ocean Sample Displacement Demo")]
    public class OceanSampleDisplacementDemo : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public bool _trackCamera = true;

        [UnityEngine.Range(0f, 32f)]
        public float _minGridSize = 0f;

        GameObject[] _markerObjects = new GameObject[3];
#if CREST_BURST_QUERY
        NativeArray<Vector3> _markerPos;
        NativeArray<Vector3> _resultDisps;
        NativeArray<Vector3> _resultNorms;
        NativeArray<Vector3> _resultVels;
#else
        Vector3[] _markerPos = new Vector3[3];
        Vector3[] _resultDisps = new Vector3[3];
        Vector3[] _resultNorms = new Vector3[3];
        Vector3[] _resultVels = new Vector3[3];
#endif

        float _samplesRadius = 5f;

#if CREST_BURST_QUERY
        void Start()
        {
            _markerPos = new NativeArray<Vector3>(3, Allocator.Persistent);
            _resultDisps = new NativeArray<Vector3>(3, Allocator.Persistent);
            _resultNorms = new NativeArray<Vector3>(3, Allocator.Persistent);
            _resultVels = new NativeArray<Vector3>(3, Allocator.Persistent);
        }
#endif

        void Update()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            if (_trackCamera)
            {
                var height = Mathf.Abs(Camera.main.transform.position.y - OceanRenderer.Instance.SeaLevel);
                var lookAngle = Mathf.Max(Mathf.Abs(Camera.main.transform.forward.y), 0.001f);
                var offset = height / lookAngle;
                _markerPos[0] = Camera.main.transform.position + Camera.main.transform.forward * offset;
                _markerPos[1] = Camera.main.transform.position + Camera.main.transform.forward * offset + _samplesRadius * Vector3.right;
                _markerPos[2] = Camera.main.transform.position + Camera.main.transform.forward * offset + _samplesRadius * Vector3.forward;
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;

#if CREST_BURST_QUERY
            var status = collProvider.Query(GetHashCode(), _minGridSize, ref _markerPos, ref _resultDisps, ref _resultNorms, ref _resultVels);
#else
            var status = collProvider.Query(GetHashCode(), _minGridSize, _markerPos, _resultDisps, _resultNorms, _resultVels);
#endif

            if (collProvider.RetrieveSucceeded(status))
            {
                for (int i = 0; i < _resultDisps.Length; i++)
                {
                    if (_markerObjects[i] == null)
                    {
                        _markerObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Helpers.Destroy(_markerObjects[i].GetComponent<Collider>());
                        _markerObjects[i].transform.localScale = Vector3.one * 0.5f;
                    }

                    var query = _markerPos[i];
                    query.y = OceanRenderer.Instance.SeaLevel;

                    var disp = _resultDisps[i];

                    var pos = query;
                    pos.y = disp.y;
                    Debug.DrawLine(pos, pos - disp);
                    _markerObjects[i].transform.position = pos;

                    _markerObjects[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, _resultNorms[i]);
                }

                for (var i = 0; i < _resultNorms.Length; i++)
                {
                    Debug.DrawLine(_markerObjects[i].transform.position, _markerObjects[i].transform.position + _resultNorms[i], Color.blue);
                }

                for (var i = 0; i < _resultVels.Length; i++)
                {
                    Debug.DrawLine(_markerObjects[i].transform.position, _markerObjects[i].transform.position + _resultVels[i], Color.green);
                }
            }
        }
    }
}
