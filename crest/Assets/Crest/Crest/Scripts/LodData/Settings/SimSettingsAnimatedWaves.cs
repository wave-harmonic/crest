// Crest Ocean System

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

        public enum CollisionSources
        {
            None,
            GerstnerWavesCPU,
            ComputeShaderQueries,
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


        public override void AddToSettingsHash(ref int settingsHash)
        {
            base.AddToSettingsHash(ref settingsHash);
            Hashy.AddInt((int)_renderTextureGraphicsFormat, ref settingsHash);
            Hashy.AddBool(Helpers.IsMotionVectorsEnabled(), ref settingsHash);
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
    }

#if UNITY_EDITOR
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
#endif
#endif
}
