// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Base class for objects that float on water.
    /// </summary>
    public abstract class FloatingObjectBase : MonoBehaviour
    {
        public abstract float ObjectWidth { get; }
        public abstract bool InWater { get; }
        public abstract Vector3 Velocity { get; }

        /// <summary>
        /// The ocean data has horizontal displacements. This represents the displacement that lands at this object position.
        /// </summary>
        public abstract Vector3 CalculateDisplacementToObject();
    }
}
