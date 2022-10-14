// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Debug draw crosses in an area around the GameObject on the water surface.
    /// </summary>
    [ExecuteDuringEditMode]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_DEBUG + "Visualise Collision Area")]
    public class VisualiseCollisionArea : CustomMonoBehaviour
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

        [SerializeField]
        bool _useDisplacements;

        [SerializeField]
        bool _useNormals;

        float[] _resultHeights;
        Vector3[] _resultDisps;
        Vector3[] _resultNorms;

        Vector3[] _samplePositions;

        void Update()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.CollisionProvider == null)
            {
                return;
            }

            if (_resultHeights == null || _resultHeights.Length != _steps * _steps)
            {
                _resultHeights = new float[_steps * _steps];
            }
            if (_resultDisps == null || _resultDisps.Length != _steps * _steps)
            {
                _resultDisps = new Vector3[_steps * _steps];
            }
            if (_resultNorms == null || _resultNorms.Length != _steps * _steps)
            {
                _resultNorms = new Vector3[_steps * _steps];

                for (int i = 0; i < _resultNorms.Length; i++)
                {
                    _resultNorms[i] = Vector3.up;
                }
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

            if (_useDisplacements)
            {
                if (collProvider.RetrieveSucceeded(collProvider.Query(GetHashCode(), _objectWidth, _samplePositions, _resultDisps, _useNormals ? _resultNorms : null, null)))
                {
                    for (int i = 0; i < _steps; i++)
                    {
                        for (int j = 0; j < _steps; j++)
                        {
                            var result = _samplePositions[j * _steps + i];
                            result.y = OceanRenderer.Instance.SeaLevel;
                            result += _resultDisps[j * _steps + i];

                            var norm = _useNormals ? _resultNorms[j * _steps + i] : Vector3.up;

                            DebugDrawCross(result, norm, Mathf.Min(_stepSize / 4f, 1f), Color.green);
                        }
                    }
                }
            }
            else
            {
                if (collProvider.RetrieveSucceeded(collProvider.Query(GetHashCode(), _objectWidth, _samplePositions, _resultHeights, _useNormals ? _resultNorms : null, null)))
                {
                    for (int i = 0; i < _steps; i++)
                    {
                        for (int j = 0; j < _steps; j++)
                        {
                            var result = _samplePositions[j * _steps + i];
                            result.y = _resultHeights[j * _steps + i];

                            var norm = _useNormals ? _resultNorms[j * _steps + i] : Vector3.up;

                            DebugDrawCross(result, norm, Mathf.Min(_stepSize / 4f, 1f), Color.green);
                        }
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

        public static void DebugDrawCross(Vector3 pos, Vector3 up, float r, Color col, float duration = 0f)
        {
            up.Normalize();
            var right = Vector3.Normalize(Vector3.Cross(up, Vector3.forward));
            var forward = Vector3.Cross(up, right);
            Debug.DrawLine(pos - up * r, pos + up * r, col, duration);
            Debug.DrawLine(pos - right * r, pos + right * r, col, duration);
            Debug.DrawLine(pos - forward * r, pos + forward * r, col, duration);
        }
    }
}
