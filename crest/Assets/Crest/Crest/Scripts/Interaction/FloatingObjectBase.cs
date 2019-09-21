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
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _resultDisp = new Vector3[1];

        /// <summary>
        /// The ocean data has horizontal displacements. This represents the displacement that lands at this object position.
        /// </summary>
        public virtual Vector3 CalculateDisplacementToObject()
        {
            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var samplingRect = new Rect(transform.position.x, transform.position.z, 0f, 0f);

            collProvider.GetSamplingData(ref samplingRect, ObjectWidth, _samplingData);

            _queryPos[0] = transform.position;
            collProvider.Query(GetInstanceID(), _samplingData, _queryPos, null, _resultDisp, null);

            collProvider.ReturnSamplingData(_samplingData);

            return _resultDisp[0];
        }
    }
}
