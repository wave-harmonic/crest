// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Serialization;

namespace Crest
{
    public class Whirlpool : MonoBehaviour
    {
        [Range(0, 1000), SerializeField, FormerlySerializedAs("amplitude")]
        float _amplitude = 10f;
        [Range(0, 1000), SerializeField, FormerlySerializedAs("radius")]
        float _radius = 20f;
        [Range(0, 1000), SerializeField, FormerlySerializedAs("eyeRadius")]
        float _eyeRadius = 1f;
        [Range(0, 1000), SerializeField, FormerlySerializedAs("maxSpeed")]
        float _maxSpeed = 70f;

        private GameObject _flow;
        private GameObject _displacement;
        private Material _flowMaterial;
        private Material _displacementMaterial;

        private void UpdateMaterials()
        {
            _flowMaterial.SetFloat("_EyeRadiusProportion", _eyeRadius / _radius);
            _flowMaterial.SetFloat("_MaxSpeed", _maxSpeed);

            _displacementMaterial.SetFloat("_Radius", _radius * 0.25f);
            _displacementMaterial.SetFloat("_Amplitude", _amplitude);
        }

        void Start()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            _flow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(_flow.GetComponent<Collider>());
            _flow.name = "Flow";
            _flow.transform.parent = transform;
            _flow.transform.position = new Vector3(0f, -100f, 0f);
            _flow.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            _flow.transform.localScale = new Vector3(_radius * 2f, _radius * 2f, 1f);
            _flowMaterial = new Material(Shader.Find("Ocean/Inputs/Flow/Whirlpool"));
            {
                var rend = _flow.GetComponent<Renderer>();
                rend.material = _flowMaterial;
                rend.enabled = false;
            }
            _flow.AddComponent<RegisterFlowInput>();

            _displacement = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(_displacement.GetComponent<Collider>());
            _displacement.name = "Displacement";
            _displacement.transform.parent = transform;
            _displacement.transform.position = new Vector3(0f, -100f, 0f);
            _displacement.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            _displacement.AddComponent<RegisterAnimWavesInput>();
            _displacementMaterial = new Material(Shader.Find("Ocean/Inputs/Animated Waves/Whirlpool"));
            _displacement.GetComponent<Renderer>().material = _displacementMaterial;

            UpdateMaterials();
        }

        void Update()
        {
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(0, _amplitude);
            UpdateMaterials();
        }
    }
}
