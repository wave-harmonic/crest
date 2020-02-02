// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsAnimatedWaves", menuName = "Crest/Animated Waves Sim Settings", order = 10000)]
    public class SimSettingsAnimatedWaves : SimSettingsBase
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
        [Header("Readback to CPU")]
        [Tooltip("Where to obtain ocean shape on CPU for physics / gameplay."), SerializeField]
        CollisionSources _collisionSource = CollisionSources.ComputeShaderQueries;
        public CollisionSources CollisionSource { get { return _collisionSource; } }

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
                    result = QueryDisplacements.Instance;
                    break;
            }

            if (result == null)
            {
                // this should not be hit - return null to create null ref exceptions
                Debug.Assert(false, "Could not create collision provider. Collision source = " + _collisionSource.ToString());
                return null;
            }

            return result;
        }
    }
}
