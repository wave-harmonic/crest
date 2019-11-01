using UnityEngine;

namespace Crest
{
    public class AssignLayer : MonoBehaviour, IValidated
    {
        [SerializeField]
        string _layerName = "";

        private void Awake()
        {
            enabled = false;

            if (!Validate(OceanRenderer.Instance))
            {
                return;
            }

            gameObject.layer = LayerMask.NameToLayer(_layerName);
        }

        public bool Validate(OceanRenderer ocean)
        {
            if (string.IsNullOrEmpty(_layerName))
            {
                Debug.LogError("Validation: Layer name required by AssignLayer script. Click this error to see the script in question.", this);
                return false;
            }
            
            if (LayerMask.NameToLayer(_layerName) < 0)
            {
                Debug.LogError("Validation: Layer " + _layerName + " does not exist in the project, please add it.", this);
                return false;
            }

            return true;
        }
    }
}
