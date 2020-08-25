// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    [ExecuteAlways]
    public partial class AssignLayer : MonoBehaviour
    {
        [SerializeField]
        string _layerName = "";

        private void Awake()
        {
            enabled = false;

#if UNITY_EDITOR
            if (EditorApplication.isPlaying && !Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                return;
            }
#endif

            gameObject.layer = LayerMask.NameToLayer(_layerName);
        }
    }

#if UNITY_EDITOR
    public partial class AssignLayer : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            if (string.IsNullOrEmpty(_layerName))
            {
                showMessage
                (
                    "Layer name required by AssignLayer script.",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            if (LayerMask.NameToLayer(_layerName) < 0)
            {
                showMessage
                (
                    $"Layer <i>{_layerName}</i> does not exist in the project, please add it.",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            return true;
        }
    }

    [CustomEditor(typeof(AssignLayer)), CanEditMultipleObjects]
    class AssignLayerEditor : ValidatedEditor { }
#endif
}
