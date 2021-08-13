// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Base class for scripts that provide the time to the ocean system. See derived classes for examples.
    /// </summary>
    public interface ITimeProvider
    {
        float CurrentTime { get; }
        float DeltaTime { get; }

        // Delta time used for dynamics such as the ripple sim
        float DeltaTimeDynamics { get; }
    }

    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "time-providers.html" + Internal.Constants.HELP_URL_RP)]
    public abstract class TimeProviderBase : MonoBehaviour, ITimeProvider
    {
        public abstract float CurrentTime { get; }
        public abstract float DeltaTime { get; }
        public abstract float DeltaTimeDynamics { get; }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TimeProviderBase), editorForChildClasses: true), CanEditMultipleObjects]
    class TimeProviderBaseEditor : Editor { }
#endif
}
