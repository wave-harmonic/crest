// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Base class for objects that float on water.
    /// </summary>
    public abstract partial class FloatingObjectBase : MonoBehaviour
    {
        public abstract float ObjectWidth { get; }
        public abstract bool InWater { get; }
        public abstract Vector3 Velocity { get; }
    }

#if UNITY_EDITOR
    public abstract partial class FloatingObjectBase : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (ocean != null && ocean._simSettingsAnimatedWaves != null && ocean._simSettingsAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.None)
            {
                showMessage
                (
                    "<i>Collision Source</i> in <i>Animated Waves Settings</i> is set to <i>None</i>. The floating objects in the scene will use a flat horizontal plane.",
                    "Set the <i>Collision Source</i> to <i>ComputeShaderQueries</i> to incorporate waves into physics.",
                    ValidatedHelper.MessageType.Warning, ocean,
                    SimSettingsAnimatedWaves.FixSetCollisionSourceToCompute
                );

                isValid = false;
            }

            var rbs = GetComponentsInChildren<Rigidbody>();
            if (rbs.Length != 1)
            {
                showMessage
                (
                    $"Expected to have one rigidbody on floating object, currently has {rbs.Length} object(s).",
                    "Remove additional <i>Rigidbody</i> components.",
                    ValidatedHelper.MessageType.Error, this
                );
            }

            return isValid;
        }
    }

    [CustomEditor(typeof(FloatingObjectBase), true), CanEditMultipleObjects]
    class FloatingObjectBaseEditor : ValidatedEditor { }
#endif
}
