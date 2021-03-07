using Crest.Spline;
using UnityEngine;

namespace Crest
{
    public class SplinePointDataWeight : MonoBehaviour, ISplinePointCustomData
    {
        public float _weight = 1f;

        public Vector2 GetData()
        {
            return new Vector2(_weight, 0f);
        }
    }
}
