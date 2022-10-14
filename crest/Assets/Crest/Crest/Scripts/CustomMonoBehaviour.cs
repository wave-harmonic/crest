using UnityEditor;
using UnityEngine;

using Include = ExecuteDuringEditModeAttribute.Include;

namespace Crest
{
    /// <summary>
    /// Implements custom behaviours common to all components.
    /// </summary>
    public abstract class CustomMonoBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        bool _isFirstOnValidate = true;
        internal bool _isPrefabStageInstance;

        protected virtual void OnValidate()
        {
            if (_isFirstOnValidate)
            {
                var attribute = Helpers.GetCustomAttribute<ExecuteDuringEditModeAttribute>(GetType());

                var enableInEditMode = attribute != null;

                if (enableInEditMode)
                {
                    if (!attribute._including.HasFlag(Include.BuildPipeline))
                    {
                        // Do not execute when building the player.
                        enableInEditMode = !BuildPipeline.isBuildingPlayer;
                    }

                    // Components that use the singleton pattern are candidates for not executing in the prefab stage
                    // as a new instance will be created which could interfere with the scene stage instance.
                    if (!attribute._including.HasFlag(Include.PrefabStage))
                    {
#if UNITY_2021_2_OR_NEWER
                        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
                        var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
                        _isPrefabStageInstance = stage != null && gameObject.scene == stage.scene;

                        // Do not execute in prefab stage.
                        enableInEditMode = !_isPrefabStageInstance;
                    }
                }

                // TryAndEnableEditMode will immediately call Awake and OnEnable so we must not do this in OnValidate as
                // there are many restrictions which Unity will produce warnings for:
                // https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
                // Coroutines are not an option as they will throw errors if not active.
                if (enableInEditMode)
                {
                    Invoke("TryAndEnableEditMode", 0);
                }
            }

            _isFirstOnValidate = false;
        }

#pragma warning disable IDE0051
        void TryAndEnableEditMode()
#pragma warning restore IDE0051
        {
            // Workaround to ExecuteAlways also executing during building which is often not what we want.
            runInEditMode = true;
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Base editor. Needed as custom drawers require a custom editor to work.
    /// </summary>
    [CustomEditor(typeof(CustomMonoBehaviour), editorForChildClasses: true), CanEditMultipleObjects]
    public class CustomBaseEditor : ValidatedEditor
    {
        public override void OnInspectorGUI()
        {
            RenderBeforeInspectorGUI();
            base.OnInspectorGUI();
        }

        protected void RenderBeforeInspectorGUI()
        {
            var target = this.target as CustomMonoBehaviour;

            if (target._isPrefabStageInstance)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(Internal.Constants.k_NoPrefabModeSupportWarning, MessageType.Warning);
                EditorGUILayout.Space();
            }
        }
    }
#endif
}
