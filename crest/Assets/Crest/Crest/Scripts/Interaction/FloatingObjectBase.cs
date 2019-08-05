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

        SamplingData _samplingData = new SamplingData();

        /// <summary>
        /// The ocean data has horizontal displacements. This represents the displacement that lands at this object position.
        /// </summary>
        public virtual Vector3 CalculateDisplacementToObject()
        {
            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var samplingRect = new Rect(transform.position.x, transform.position.z, 0f, 0f);

            collProvider.GetSamplingData(ref samplingRect, ObjectWidth, _samplingData);

            // Invert the displacement - find position that displaces to current position
            Vector3 position = transform.position, undispPos;
            if (!collProvider.ComputeUndisplacedPosition(ref position, _samplingData, out undispPos))
            {
                // If we couldn't get wave shape, assume flat water at sea level
                undispPos = position;
                undispPos.y = OceanRenderer.Instance.SeaLevel;
            }

            // Compute the displacement at that position
            Vector3 waterSurfaceVel, displacement, result = Vector3.zero;
            bool dispValid, velValid;
            collProvider.SampleDisplacementVel(ref undispPos, _samplingData, out displacement, out dispValid, out waterSurfaceVel, out velValid);
            if (dispValid)
            {
                result = displacement;
            }

            collProvider.ReturnSamplingData(_samplingData);

            return result;
        }
    }
}
