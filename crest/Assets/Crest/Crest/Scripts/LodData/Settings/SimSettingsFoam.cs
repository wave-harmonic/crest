// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsFoam", menuName = "Crest/Foam Sim Settings", order = 10000)]
    public class SimSettingsFoam : SimSettingsBase
    {
        [Range(0f, 20f), Tooltip("Speed at which foam fades/dissipates.")]
        public float _foamFadeRate = 0.8f;
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated from waves.")]
        public float _waveFoamStrength = 1f;
        [Range(0f, 1f), Tooltip("How much of the waves generate foam.")]
        public float _waveFoamCoverage = 0.8f;
        [Range(0.01f, 3f), Tooltip("Foam will be generated in water shallower than this depth.")]
        public float _shorelineFoamMaxDepth = 0.65f;
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated in shallow water.")]
        public float _shorelineFoamStrength = 2f;
        [Tooltip("The rendertexture format to use for the foam simulation")]
        public GraphicsFormat _renderTextureGraphicsFormat = GraphicsFormat.R16_SFloat;
        [Range(15f, 200f), Tooltip("Frequency to run the foam sim, in updates per second. Lower frequencies can be more efficient but may lead to visible jitter. Default is 30 updates per second.")]
        public float _simulationFrequency = 30f;
    }
}
