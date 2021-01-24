// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsFoam", menuName = "Crest/Foam Sim Settings", order = 10000)]
    [HelpURL("https://github.com/wave-harmonic/crest/blob/master/USERGUIDE.md#foam")]
    public class SimSettingsFoam : SimSettingsBase
    {
        [SerializeField]
#pragma warning disable 414
        string _helpURL = "https://github.com/wave-harmonic/crest/blob/master/USERGUIDE.md#foam";
#pragma warning restore 414

        [Header("General settings")]
        [Range(0f, 20f), Tooltip("Speed at which foam fades/dissipates.")]
        public float _foamFadeRate = 0.8f;

        [Header("Whitecaps")]
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated from waves. This setting should be balanced with the Foam Fade Rate setting.")]
        public float _waveFoamStrength = 1f;
        [Range(0f, 1f), Tooltip("How much of the waves generate foam. Higher values will lower the threshold for foam generation, giving a larger area.")]
        public float _waveFoamCoverage = 0.8f;

        [Header("Shoreline")]
        [Range(0.01f, 3f), Tooltip("Foam will be generated in water shallower than this depth. Controls how wide the band of foam at the shoreline will be.")]
        public float _shorelineFoamMaxDepth = 0.65f;
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated in shallow water. This setting should be balanced with the fade rate.")]
        public float _shorelineFoamStrength = 2f;

        [Header("Developer settings")]
        [Tooltip("The render texture format to use for the foam simulation. This is mostly for debugging and should be left at its default.")]
        public GraphicsFormat _renderTextureGraphicsFormat = GraphicsFormat.R16_SFloat;
    }
}
