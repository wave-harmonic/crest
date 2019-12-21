// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Default time provider - sets the ocean time to Unity's game time.
    /// </summary>
    public class TimeProviderDefault : TimeProviderBase
    {
        public override float CurrentTime
        {
            get
            {
#if UNITY_EDITOR
                if(UnityEditor.EditorApplication.isPlaying)
                {
                    return Time.time;
                }
                else
                {
                    return (float)UnityEditor.EditorApplication.timeSinceStartup;
                }
#else
                return Time.time;
#endif
            }
        }
        public override float DeltaTime => Time.deltaTime;
    }
}
