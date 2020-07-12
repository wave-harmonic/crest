// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsAnimatedWaves", menuName = "Crest/Animated Waves Sim Settings", order = 10000)]
    public partial class SimSettingsAnimatedWaves : SimSettingsBase
    {
        [Tooltip("How much waves are dampened in shallow water."), SerializeField, Range(0f, 1f)]
        float _attenuationInShallows = 0.95f;
        public float AttenuationInShallows { get { return _attenuationInShallows; } }

        public enum CollisionSources
        {
            None,
            GerstnerWavesCPU,
            ComputeShaderQueries,
        }

        [Tooltip("Where to obtain ocean shape on CPU for physics / gameplay."), SerializeField]
        CollisionSources _collisionSource = CollisionSources.ComputeShaderQueries;
        public CollisionSources CollisionSource { get { return _collisionSource; } }

        [Tooltip("Maximum number of wave queries that can be performed when using ComputeShaderQueries.")]
        [PredicatedField("_collisionSource", true, (int)CollisionSources.ComputeShaderQueries), SerializeField]
        int _maxQueryCount = QueryBase.MAX_QUERY_COUNT_DEFAULT;
        public int MaxQueryCount { get { return _maxQueryCount; } }

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
                    result = new QueryDisplacements();
                    break;
            }

            if (result == null)
            {
                // this should not be hit, but can be if compute shaders aren't loaded correctly.
                // they will print out appropriate errors, so we don't want to return just null and have null reference
                // exceptions spamming the logs.
                //Debug.LogError($"Could not create collision provider. Collision source = {_collisionSource.ToString()}", this);
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

            if (_collisionSource == CollisionSources.GerstnerWavesCPU)
            {
                showMessage
                (
                    "<i>Gerstner Waves CPU</i> has significant drawbacks. It does not include wave attenuation from " +
                    "water depth or any custom rendered shape. It does not support multiple " +
                    "<i>GerstnerWavesBatched</i> components including cross blending. Please read the user guide for more information.",
                    ValidatedHelper.MessageType.Info, this
                );
            }
            else if (_collisionSource == CollisionSources.None)
            {
                showMessage
                (
                    "Collision Source in Animated Waves Settings is set to None. The floating objects in the scene will use a flat horizontal plane.",
                    ValidatedHelper.MessageType.Warning, this
                );
            }

            return isValid;
        }
    }
#endif
}
