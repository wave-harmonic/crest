// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Base class for objects that float on water.
    /// </summary>
    public abstract class FloatingObjectBase : MonoBehaviour
    {
        public abstract Vector3 DisplacementToObject { get; set; }
        public abstract float ObjectWidth { get; }
        public abstract bool InWater { get; }
        public abstract Rigidbody RB { get; set; }
    }
}
