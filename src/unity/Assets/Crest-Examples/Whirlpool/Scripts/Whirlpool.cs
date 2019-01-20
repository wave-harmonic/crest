// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class Whirlpool : MonoBehaviour
    {
        [Range(0, 1000), SerializeField]
        float _amplitude = 10f;
        [Range(0, 1000), SerializeField]
        float _radius = 20f;
        [Range(0, 1000), SerializeField]
        float _eyeRadius = 1f;
        [Range(0, 1000), SerializeField]
        float _maxSpeed = 70f;

        [SerializeField]
        bool _createDisplacement = true;
        [SerializeField]
        bool _createFlow = true;
        [SerializeField]
        bool _createDynWavesDampen = true;

        Material _flowMaterial;
        Material _displacementMaterial;
        Material _dampDynWavesMaterial;

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

            _displacementMaterial = new Material(Shader.Find("Ocean/Inputs/Animated Waves/Whirlpool"));
            if (_createDisplacement)
            {
                AddInput<RegisterAnimWavesInput>(_displacementMaterial, _radius);
            }

            _flowMaterial = new Material(Shader.Find("Ocean/Inputs/Flow/Whirlpool"));
            if (_createFlow)
            {
                AddInput<RegisterFlowInput>(_flowMaterial, _radius);
            }

            _dampDynWavesMaterial = new Material(Shader.Find("Ocean/Inputs/Dynamic Waves/Dampen Circle"));
            if (_createDynWavesDampen)
            {
                AddInput<RegisterDynWavesInput>(_dampDynWavesMaterial, _radius);
            }

            UpdateMaterials();
        }

        void AddInput<RegisterInputType>(Material material, float radius) where RegisterInputType : Component
        {
            var input = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(input.GetComponent<Collider>());
            input.name = typeof(RegisterInputType).Name;
            input.transform.parent = transform;
            input.transform.position = new Vector3(0f, -100f, 0f);
            input.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            input.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            input.GetComponent<Renderer>().material = material;
            input.AddComponent<RegisterInputType>();
        }

        void Update()
        {
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(0, _amplitude);
            UpdateMaterials();
        }
    }
}
