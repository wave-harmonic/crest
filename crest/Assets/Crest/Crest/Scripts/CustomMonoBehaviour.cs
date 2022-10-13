using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Implements custom behaviours common to all components.
    /// </summary>
    public abstract class CustomMonoBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!runInEditMode)
            {
                // TryAndEnableEditMode will immediately call Awake and OnEnable so we must not do this in OnValidate as
                // there are many restrictions which Unity will produce warnings for:
                // https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
                // Coroutines are not an option as will throw errors if not active.
                Invoke("TryAndEnableEditMode", 0);
            }
        }

#pragma warning disable IDE0051
        void TryAndEnableEditMode()
#pragma warning restore IDE0051
        {
            // Workaround to ExecuteAlways also executing during building which is often not what we want.
            runInEditMode = !UnityEditor.BuildPipeline.isBuildingPlayer;
        }
#endif
    }
}

