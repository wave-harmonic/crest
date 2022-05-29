// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Base class for simulation settings.
    /// </summary>
    public partial class SimSettingsBase : ScriptableObject
    {
        /// <summary>
        /// Adds anything that requires a rebuild to the provided settings hash.
        /// </summary>
        public virtual void AddToSettingsHash(ref int settingsHash)
        {
            // Intentionally left empty.
        }
    }

#if UNITY_EDITOR
    public partial class SimSettingsBase : IValidated
    {
        public virtual bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage) => true;
    }

    [CustomEditor(typeof(SimSettingsBase), true), CanEditMultipleObjects]
    class SimSettingsBaseEditor : ValidatedEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Online Help Page"))
            {
                var targetType = target.GetType();
                var helpAttribute = (CrestHelpURLAttribute)System.Attribute.GetCustomAttribute(targetType, typeof(CrestHelpURLAttribute));
                Debug.AssertFormat(helpAttribute != null, "Crest: Could not get CrestHelpURL attribute from {0}.", targetType);
                Application.OpenURL(helpAttribute.URL);
            }
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
