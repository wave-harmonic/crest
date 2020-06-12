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
    }

#if UNITY_EDITOR
    public partial class SimSettingsBase : IValidated
    {
        public virtual bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage) => true;
    }

    [CustomEditor(typeof(SimSettingsAnimatedWaves), true), CanEditMultipleObjects]
    class SimSettingsBaseEditor : ValidatedEditor { }
#endif
}
