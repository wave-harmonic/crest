// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [ExecuteDuringEditMode]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_EXAMPLE + "Whirlpool")]
    public class Whirlpool : CustomMonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Range(0, 1000), SerializeField]
        float _amplitude = 20f;
        [Range(0, 1000), SerializeField]
        float _radius = 80f;
        [Range(0, 1000), SerializeField]
        float _eyeRadius = 1f;
        [Range(0, 1000), SerializeField]
        float _maxSpeed = 10f;

        [OnChange("CreateOrDestroyAnimatedWaves"), DecoratedField]
        [SerializeField]
        bool _createDisplacement = true;

        [OnChange("CreateOrDestroyFlow"), DecoratedField]
        [SerializeField]
        bool _createFlow = true;

        [OnChange("CreateOrDestroyDynamicWaves"), DecoratedField]
        [SerializeField]
        bool _createDynWavesDampen = true;

        Material _flowMaterial;
        Material _displacementMaterial;
        Material _dampDynWavesMaterial;

        GameObject _displacementInput;
        GameObject _flowInput;
        GameObject _dynamicWavesInput;

        public static class ShaderIDs
        {
            public static readonly int s_EyeRadiusProportion = Shader.PropertyToID("_EyeRadiusProportion");
            public static readonly int s_MaxSpeed = Shader.PropertyToID("_MaxSpeed");
            public static readonly int s_Radius = Shader.PropertyToID("_Radius");
            public static readonly int s_Amplitude = Shader.PropertyToID("_Amplitude");
        }

        private void UpdateMaterials()
        {
            if (_flowMaterial)
            {
                _flowMaterial.SetFloat(ShaderIDs.s_EyeRadiusProportion, _eyeRadius / _radius);
                _flowMaterial.SetFloat(ShaderIDs.s_MaxSpeed, _maxSpeed);
            }

            if (_displacementMaterial)
            {
                _displacementMaterial.SetFloat(ShaderIDs.s_Radius, _radius * 0.25f);
                _displacementMaterial.SetFloat(ShaderIDs.s_Amplitude, _amplitude);
            }
        }

        void UpdateInputs()
        {
            var scale = new Vector3(_radius, _radius, 1f);
            if (_displacementInput) _displacementInput.transform.localScale = scale;
            if (_flowInput) _flowInput.transform.localScale = scale;
            if (_dynamicWavesInput) _dynamicWavesInput.transform.localScale = scale;
        }

        void OnEnable()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            CreateOrDestroyAnimatedWaves();
            CreateOrDestroyFlow();
            CreateOrDestroyDynamicWaves();

            UpdateMaterials();

            LodDataMgrAnimWaves.RegisterUpdatable(this);
        }

        void OnDisable()
        {
            Helpers.Destroy(_displacementInput);
            Helpers.Destroy(_flowInput);
            Helpers.Destroy(_dynamicWavesInput);
            Helpers.Destroy(_displacementMaterial);
            Helpers.Destroy(_flowMaterial);
            Helpers.Destroy(_dampDynWavesMaterial);

            LodDataMgrAnimWaves.DeregisterUpdatable(this);
        }

        public void CrestUpdate(UnityEngine.Rendering.CommandBuffer buf)
        {
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(0f, _amplitude, 0f);
        }

        void CreateOrDestroy<RegisterInputType>(bool toggle, string shaderName, ref GameObject input, ref Material material) where RegisterInputType : RegisterLodDataInputBase
        {
            if (toggle)
            {
                material = new Material(Shader.Find(shaderName));
                material.hideFlags = HideFlags.HideAndDontSave;
                input = AddInput<RegisterInputType>(material, _radius);
            }
            else
            {
                Helpers.Destroy(input);
                Helpers.Destroy(material);
            }
        }

        GameObject AddInput<RegisterInputType>(Material material, float radius) where RegisterInputType : RegisterLodDataInputBase
        {
            var input = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Helpers.Destroy(input.GetComponent<Collider>());
            input.name = typeof(RegisterInputType).Name;
            input.transform.parent = transform;
            input.transform.localPosition = new Vector3(0f, 0f, 0f);
            input.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            input.transform.localScale = new Vector3(radius, radius, 1f);
            input.hideFlags = HideFlags.HideAndDontSave;
            input.GetComponent<Renderer>().sharedMaterial = material;
            input.AddComponent<RegisterInputType>();
            return input;
        }

        void CreateOrDestroyAnimatedWaves()
        {
            CreateOrDestroy<RegisterAnimWavesInput>(_createDisplacement, "Crest/Inputs/Animated Waves/Whirlpool", ref _displacementInput, ref _displacementMaterial);
        }

        void CreateOrDestroyFlow()
        {
            CreateOrDestroy<RegisterFlowInput>(_createFlow, "Crest/Inputs/Flow/Whirlpool", ref _flowInput, ref _flowMaterial);
        }

        void CreateOrDestroyDynamicWaves()
        {
            CreateOrDestroy<RegisterDynWavesInput>(_createDynWavesDampen, "Crest/Inputs/Dynamic Waves/Dampen Circle", ref _dynamicWavesInput, ref _dampDynWavesMaterial);
        }

        void Update()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            UpdateMaterials();
            UpdateInputs();
        }
    }
}
