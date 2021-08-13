// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsFoam", menuName = "Crest/Foam Sim Settings", order = 10000)]
    [HelpURL(HELP_URL)]
    public class SimSettingsFoam : SimSettingsBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public const string HELP_URL = Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#id5";

        [Header("General settings")]
        [Range(0f, 20f), Tooltip("Speed at which foam fades/dissipates.")]
        public float _foamFadeRate = 0.8f;

        [Header("Whitecaps")]
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated from waves. This setting should be balanced with the Foam Fade Rate setting.")]
        public float _waveFoamStrength = 1f;
        [Range(0f, 1f), Tooltip("How much of the waves generate foam. Higher values will lower the threshold for foam generation, giving a larger area.")]
        public float _waveFoamCoverage = 0.55f;

        [Header("Shoreline")]
        [Range(0.01f, 3f), Tooltip("Foam will be generated in water shallower than this depth. Controls how wide the band of foam at the shoreline will be.")]
        public float _shorelineFoamMaxDepth = 0.65f;
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated in shallow water. This setting should be balanced with the fade rate.")]
        public float _shorelineFoamStrength = 2f;

        [Header("Developer settings")]
        [Tooltip("The render texture format to use for the foam simulation. This is mostly for debugging and should be left at its default.")]
        public GraphicsFormat _renderTextureGraphicsFormat = GraphicsFormat.R16_SFloat;
        [Range(15f, 200f), Tooltip("Frequency to run the foam sim, in updates per second. Lower frequencies can be more efficient but may lead to visible jitter. Default is 30 updates per second.")]
        public float _simulationFrequency = 30f;

        public override void AddToSettingsHash(ref int settingsHash)
        {
            base.AddToSettingsHash(ref settingsHash);
            Hashy.AddInt((int)_renderTextureGraphicsFormat, ref settingsHash);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SimSettingsFoam), true), CanEditMultipleObjects]
    class SimSettingsFoamEditor : SimSettingsBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Online Help Page"))
            {
                Application.OpenURL(SimSettingsFoam.HELP_URL);
            }
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
