// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Provides out-scattering based on the camera's underwater depth. It scales down environmental lighting
    /// (directional light, reflections, ambient etc) with the underwater depth. This works with vanilla lighting, but
    /// uncommon or custom lighting will require a custom solution (use this for reference).
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_EXAMPLE + "Underwater Environmental Lighting")]
    public class UnderwaterEnvironmentalLighting : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Tooltip("How much this effect applies. Values less than 1 attenuate light less underwater. Value of 1 is physically based."), SerializeField, Range(0f, 3f)]
        float _weight = 1f;

        Light _primaryLight;
        float _lightIntensity;
        float _ambientIntensity;
        float _reflectionIntensity;
        float _fogDensity;

        float _averageDensity = 0f;

        public const float DEPTH_OUTSCATTER_CONSTANT = 0.25f;

        bool _isInitialised = false;

        void OnEnable()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            // Check to make sure the property exists. We might be using a test material.
            if (!OceanRenderer.Instance.OceanMaterial.HasProperty("_DepthFogDensity"))
            {
                enabled = false;
                return;
            }

            _primaryLight = OceanRenderer.Instance._primaryLight;

            // Store lighting settings
            if (_primaryLight)
            {
                _lightIntensity = _primaryLight.intensity;
            }
            _ambientIntensity = RenderSettings.ambientIntensity;
            _reflectionIntensity = RenderSettings.reflectionIntensity;
            _fogDensity = RenderSettings.fogDensity;

            Color density = OceanRenderer.Instance.OceanMaterial.GetColor("_DepthFogDensity");
            _averageDensity = (density.r + density.g + density.b) / 3f;

            _isInitialised = true;
        }

        void OnDisable()
        {
            if (!_isInitialised)
            {
                return;
            }

            // Restore lighting settings
            if (_primaryLight)
            {
                _primaryLight.intensity = _lightIntensity;
            }
            RenderSettings.ambientIntensity = _ambientIntensity;
            RenderSettings.reflectionIntensity = _reflectionIntensity;
            RenderSettings.fogDensity = _fogDensity;

            _isInitialised = false;
        }

        void LateUpdate()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            float depthMultiplier = Mathf.Exp(_averageDensity *
                Mathf.Min(OceanRenderer.Instance.ViewerHeightAboveWater * DEPTH_OUTSCATTER_CONSTANT, 0f) *
                _weight);

            // Darken environmental lighting when viewer underwater
            if (_primaryLight)
            {
                _primaryLight.intensity = Mathf.Lerp(0, _lightIntensity, depthMultiplier);
            }
            RenderSettings.ambientIntensity = Mathf.Lerp(0, _ambientIntensity, depthMultiplier);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0, _reflectionIntensity, depthMultiplier);
            RenderSettings.fogDensity = Mathf.Lerp(0, _fogDensity, depthMultiplier);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UnderwaterEnvironmentalLighting))]
    public class UnderwaterEnvironmentalLightingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This is an example component that will likely require modification to work " +
                "correctly with your project. It implements out-scattering when underwater. It does so, primarily, " +
                "by changing the intensity of the primary light. The deeper underwater, the less intense the light. " +
                "There may be unsuitable performance costs or required features to be enabled.", MessageType.Info);
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
