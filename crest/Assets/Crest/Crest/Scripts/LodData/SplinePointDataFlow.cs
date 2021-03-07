using Crest.Spline;
using UnityEngine;

namespace Crest
{
    public class SplinePointDataFlow : MonoBehaviour, ISplinePointCustomData
    {
        public const float k_defaultSpeed = 1f;
        public float _flowSpeed = k_defaultSpeed;

        public Vector2 GetData()
        {
            return new Vector2(_flowSpeed, 0f);
        }
    }
}
