// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Default time provider - sets the ocean time to Unity's game time.
    /// </summary>
    public class TimeProviderDefault : ITimeProvider
    {
        public float CurrentTime
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    return Time.time;
                }
                else
                {
                    return (float)OceanRenderer.LastUpdateEditorTime;
                }
#else
                return Time.time;
#endif
            }
        }

        public float DeltaTime
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    return Time.deltaTime;
                }
                else
                {
                    return 1f / 20f;
                }
#else
                return Time.deltaTime;
#endif
                ;
            }

        }

        public float DeltaTimeDynamics => DeltaTime;
    }
}
