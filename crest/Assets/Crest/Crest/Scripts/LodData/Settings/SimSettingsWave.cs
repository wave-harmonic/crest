// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsDynamicWaves", menuName = "Crest/Dynamic Wave Sim Settings", order = 10000)]
    [HelpURL(HELP_URL)]
    public class SimSettingsWave : SimSettingsBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public const string HELP_URL = Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#simulation-settings";

        //[Header("Range")]
        [Range(0f, 32f), Tooltip("NOT CURRENTLY WORKING. The wave sim will not run if the simulation grid is smaller in resolution than this size. Useful to limit sim range for performance."),
            HideInInspector]
        public float _minGridSize = 0f;
        [Range(0f, 32f), Tooltip("NOT CURRENTLY WORKING. The wave sim will not run if the simulation grid is bigger in resolution than this size. Zero means no constraint/unlimited resolutions. Useful to limit sim range for performance."),
            HideInInspector]
        public float _maxGridSize = 0f;

        [Header("Simulation")]
        [Range(15f, 200f), Tooltip("Frequency to run the dynamic wave sim, in updates per second. Lower frequencies can be more efficient but may limit wave speed or lead to visible jitter. Default is 60 updates per second.")]
        public float _simulationFrequency = 60f;
        [Range(0f, 1f), Tooltip("How much energy is dissipated each frame. Helps sim stability, but limits how far ripples will propagate. Set this as large as possible/acceptable. Default is 0.05.")]
        public float _damping = 0.05f;
        [Range(0.1f, 1f), Tooltip("Stability control. Lower values means more stable sim, but may slow down some dynamic waves. This value should be set as large as possible until sim instabilities/flickering begin to appear. Default is 0.7.")]
        public float _courantNumber = 0.7f;

        [Header("Displacement Generation")]
        [Range(0f, 20f), Tooltip("Induce horizontal displacements to sharpen simulated waves.")]
        public float _horizDisplace = 3f;
        [Range(0f, 1f), Tooltip("Clamp displacement to help prevent self-intersection in steep waves. Zero means unclamped.")]
        public float _displaceClamp = 0.3f;

        [Range(0f, 64f), Tooltip("Multiplier for gravity. More gravity means dynamic waves will travel faster.")]
        public float _gravityMultiplier = 1f;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SimSettingsWave), true), CanEditMultipleObjects]
    class SimSettingsWaveEditor : SimSettingsBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Online Help Page"))
            {
                Application.OpenURL(SimSettingsWave.HELP_URL);
            }
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
