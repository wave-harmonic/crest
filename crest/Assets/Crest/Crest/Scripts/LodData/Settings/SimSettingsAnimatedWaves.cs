﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsAnimatedWaves", menuName = "Crest/Animated Waves Sim Settings", order = 10000)]
    [HelpURL(k_HelpURL)]
    public partial class SimSettingsAnimatedWaves : SimSettingsBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public const string k_HelpURL = Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#animated-waves-settings";

        [Tooltip("How much waves are dampened in shallow water."), SerializeField, Range(0f, 1f)]
        float _attenuationInShallows = 0.95f;
        public float AttenuationInShallows => _attenuationInShallows;

        [Tooltip("Any water deeper than this will receive full wave strength. The lower the value, the less effective the depth cache will be at attenuating very large waves. Set to the maximum value (1,000) to disable.")]
        [SerializeField, Range(1f, LodDataMgrSeaFloorDepth.k_DepthBaseline)]
        float _shallowsMaxDepth = LodDataMgrSeaFloorDepth.k_DepthBaseline;
        public float MaximumAttenuationDepth => _shallowsMaxDepth;

        public enum CollisionSources
        {
            None,
            GerstnerWavesCPU,
            ComputeShaderQueries,
            BakedFFT
        }

        [Tooltip("Where to obtain ocean shape on CPU for physics / gameplay."), SerializeField]
        CollisionSources _collisionSource = CollisionSources.ComputeShaderQueries;
        public CollisionSources CollisionSource { get => _collisionSource; set => _collisionSource = value; }

        [Tooltip("Maximum number of wave queries that can be performed when using ComputeShaderQueries.")]
        [Predicated("_collisionSource", true, (int)CollisionSources.ComputeShaderQueries), SerializeField, DecoratedField]
        int _maxQueryCount = QueryBase.MAX_QUERY_COUNT_DEFAULT;
        public int MaxQueryCount => _maxQueryCount;

        [Tooltip("Whether to use a graphics shader for combining the wave cascades together. Disabling this uses a compute shader instead which doesn't need to copy back and forth between targets, but it may not work on some GPUs, in particular pre-DX11.3 hardware, which do not support typed UAV loads. The fail behaviour is a flat ocean."), SerializeField]
        bool _pingPongCombinePass = true;
        public bool PingPongCombinePass => _pingPongCombinePass;

        [Tooltip("The render texture format to use for the wave simulation. It should only be changed if you need more precision. See the documentation for information.")]
        public GraphicsFormat _renderTextureGraphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;

#if CREST_UNITY_MATHEMATICS
        [Predicated("_collisionSource", true, (int)CollisionSources.BakedFFT), DecoratedField]
        public FFTBakedData _bakedFFTData;
#endif // CREST_UNITY_MATHEMATICS

        public override void AddToSettingsHash(ref int settingsHash)
        {
            base.AddToSettingsHash(ref settingsHash);
            Hashy.AddInt((int)_renderTextureGraphicsFormat, ref settingsHash);
            Hashy.AddBool(Helpers.IsMotionVectorsEnabled(), ref settingsHash);
            Hashy.AddInt((int)_collisionSource, ref settingsHash);
        }

        /// <summary>
        /// Provides ocean shape to CPU.
        /// </summary>
        public ICollProvider CreateCollisionProvider()
        {
            ICollProvider result = null;

            switch (_collisionSource)
            {
                case CollisionSources.None:
                    result = new CollProviderNull();
                    break;
                case CollisionSources.GerstnerWavesCPU:
                    result = FindObjectOfType<ShapeGerstnerBatched>();
                    break;
                case CollisionSources.ComputeShaderQueries:
                    if (!OceanRenderer.RunningWithoutGPU)
                    {
                        result = new QueryDisplacements();
                    }
                    else
                    {
                        Debug.LogError("Crest: Compute shader queries not supported in headless/batch mode. To resolve, assign an Animated Wave Settings asset to the OceanRenderer component and set the Collision Source to be a CPU option.");
                    }
                    break;
#if CREST_UNITY_MATHEMATICS
                case CollisionSources.BakedFFT:
                    result = new CollProviderBakedFFT(_bakedFFTData);
                    break;
#endif // CREST_UNITY_MATHEMATICS
            }

            if (result == null)
            {
                // This should not be hit, but can be if compute shaders aren't loaded correctly.
                // They will print out appropriate errors. Don't just return null and have null reference
                // exceptions spamming the logs.
                return new CollProviderNull();
            }

            return result;
        }

        public IFlowProvider CreateFlowProvider(OceanRenderer ocean)
        {
            // Flow is GPU only, and can only be queried using the compute path
            if (ocean._lodDataFlow != null)
            {
                return new QueryFlow();
            }

            return new FlowProviderNull();
        }
    }

#if UNITY_EDITOR
    public partial class SimSettingsAnimatedWaves : IValidated
    {
        public override bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = base.Validate(ocean, showMessage);

            if (_collisionSource == CollisionSources.GerstnerWavesCPU && showMessage != ValidatedHelper.DebugLog)
            {
                showMessage
                (
                    "<i>Gerstner Waves CPU</i> has significant drawbacks. It does not include wave attenuation from " +
                    "water depth or any custom rendered shape. It does not support multiple " +
                    "<i>GerstnerWavesBatched</i> components including cross blending. Please read the user guide for more information.",
                    "Set collision source to ComputeShaderQueries",
                    ValidatedHelper.MessageType.Info, this
                );
            }
            else if (_collisionSource == CollisionSources.None)
            {
                showMessage
                (
                    "Collision Source in Animated Waves Settings is set to None. The floating objects in the scene will use a flat horizontal plane.",
                    "Set collision source to ComputeShaderQueries.",
                    ValidatedHelper.MessageType.Warning, this,
                    FixSetCollisionSourceToCompute
                );
            }
            else if (_collisionSource == CollisionSources.BakedFFT)
            {
#if CREST_UNITY_MATHEMATICS
                if (_bakedFFTData != null)
                {
                    if (!Mathf.Approximately(_bakedFFTData._parameters._windSpeed * 3.6f, ocean._globalWindSpeed))
                    {
                        showMessage
                        (
                            $"Wind speed on ocean component {ocean._globalWindSpeed} does not match wind speed of baked FFT data {_bakedFFTData._parameters._windSpeed * 3.6f}, collision shape may not match visual surface.",
                            $"Set global wind speed on ocean component to {_bakedFFTData._parameters._windSpeed * 3.6f}.",
                            ValidatedHelper.MessageType.Warning, ocean,
                            FixOceanWindSpeed
                        );
                    }
                }
#else // CREST_UNITY_MATHEMATICS
                showMessage
                (
                    "The <i>Unity Mathematics (com.unity.mathematics)</i> package is required for baked collisions.",
                    "Add the <i>Unity Mathematics</i> package.",
                    ValidatedHelper.MessageType.Error, this,
                    ValidatedHelper.FixAddMissingMathPackage
                );

                isValid = false;
#endif // CREST_UNITY_MATHEMATICS

#if !CREST_UNITY_BURST
                showMessage
                (
                    "The <i>Unity Burst (com.unity.burst)</i> package will greatly improve performance.",
                    "Add the <i>Unity Burst</i> package.",
                    ValidatedHelper.MessageType.Warning, this,
                    ValidatedHelper.FixAddMissingBurstPackage
                );
#endif // CREST_UNITY_BURST
            }

            return isValid;
        }

        internal static void FixSetCollisionSourceToCompute(SerializedObject settingsObject)
        {
            if (OceanRenderer.Instance != null && OceanRenderer.Instance._simSettingsAnimatedWaves != null)
            {
                Undo.RecordObject(OceanRenderer.Instance._simSettingsAnimatedWaves, "Set collision source to compute");
                OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource = CollisionSources.ComputeShaderQueries;
                EditorUtility.SetDirty(OceanRenderer.Instance._simSettingsAnimatedWaves);
            }
        }

#if CREST_UNITY_MATHEMATICS
        internal static void FixOceanWindSpeed(SerializedObject settingsObject)
        {
            if (OceanRenderer.Instance != null
                && OceanRenderer.Instance._simSettingsAnimatedWaves != null
                && OceanRenderer.Instance._simSettingsAnimatedWaves._bakedFFTData != null)
            {
                Undo.RecordObject(OceanRenderer.Instance, "Set global wind speed to match baked data");
                OceanRenderer.Instance._globalWindSpeed = OceanRenderer.Instance._simSettingsAnimatedWaves._bakedFFTData._parameters._windSpeed * 3.6f;
                EditorUtility.SetDirty(OceanRenderer.Instance);
            }
        }
#endif // CREST_UNITY_MATHEMATICS
    }

    [CustomEditor(typeof(SimSettingsAnimatedWaves), true), CanEditMultipleObjects]
    class SimSettingsAnimatedWavesEditor : SimSettingsBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Online Help Page"))
            {
                Application.OpenURL(SimSettingsAnimatedWaves.k_HelpURL);
            }
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif // UNITY_EDITOR
}
