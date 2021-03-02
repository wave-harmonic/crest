using Crest.Spline;
using UnityEngine;

namespace Crest
{
    public class SplinePointDataFlow : MonoBehaviour, ISplinePointCustomData
    {
        public float _flowSpeed = 5f;

        public Vector2 GetData()
        {
            return new Vector2(_flowSpeed, 0f);
        }
    }
}
