// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Debug draw crosses in an area around the GameObject on the water surface.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_DEBUG + "Visualise Collision Area")]
    public class VisualiseCollisionArea : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [SerializeField]
        float _objectWidth = 0f;

        [SerializeField]
        float _stepSize = 5f;

        [SerializeField]
        int _steps = 10;

        float[] _resultHeights;

        Vector3[] _samplePositions;

        void Update()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.CollisionProvider == null)
            {
                return;
            }

            var cp = OceanRenderer.Instance.CollisionProvider as CollProviderBakedFFT;

            if (_resultHeights == null || _resultHeights.Length != _steps * _steps)
            {
                _resultHeights = new float[_steps * _steps];
            }
            if (_samplePositions == null || _samplePositions.Length != _steps * _steps)
            {
                _samplePositions = new Vector3[_steps * _steps];
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;

            for (int i = 0; i < _steps; i++)
            {
                for (int j = 0; j < _steps; j++)
                {
                    _samplePositions[j * _steps + i] = new Vector3(((i + 0.5f) - _steps / 2f) * _stepSize, 0f, ((j + 0.5f) - _steps / 2f) * _stepSize);
                    _samplePositions[j * _steps + i].x += transform.position.x;
                    _samplePositions[j * _steps + i].z += transform.position.z;
                }
            }

            //NativeArray<float3> na = new NativeArray<float3>(_samplePositions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //for (int i = 0; i < _samplePositions.Length; i++)
            //{
            //    na[i] = _samplePositions[i];
            //}
            //NativeArray<float> results = new NativeArray<float>(_resultHeights.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            if (collProvider.RetrieveSucceeded(cp.QueryBurst(GetHashCode()/*, _objectWidth*/, _samplePositions, _resultHeights)))
            {
                for (int i = 0; i < _steps; i++)
                {
                    for (int j = 0; j < _steps; j++)
                    {
                        var result = _samplePositions[j * _steps + i];
                        result.y = _resultHeights[j * _steps + i];

                        DebugDrawCross(result, Mathf.Min(_stepSize / 4f, 1f), Color.green);
                    }
                }
            }
        }

        public static void DebugDrawCross(Vector3 pos, float r, Color col, float duration = 0f)
        {
            Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col, duration);
            Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col, duration);
            Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col, duration);
        }
    }
}
