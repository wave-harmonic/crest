using UnityEngine;

namespace Crest
{
    public class CollProviderNull : ICollProvider
    {
        public AvailabilityResult CheckAvailability(ref Vector3 in__worldPos, float minSpatialLength)
        {
            return AvailabilityResult.DataAvailable;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 in__worldPos, out Vector3 undisplacedWorldPos, float minSpatialLength = 0)
        {
            undisplacedWorldPos = in__worldPos;
            undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            return true;
        }

        public bool SampleDisplacement(ref Vector3 in__worldPos, out Vector3 displacement, float minSpatialLength = 0)
        {
            displacement = Vector3.zero;
            return true;
        }

        public bool SampleDisplacementInArea(ref Vector3 in__worldPos, out Vector3 displacement)
        {
            displacement = Vector3.zero;
            return true;
        }

        public void SampleDisplacementVel(ref Vector3 in__worldPos, out Vector3 displacement, out bool displacementValid, out Vector3 displacementVel, out bool velValid, float minSpatialLength = 0)
        {
            displacement = Vector3.zero;
            displacementValid = true;
            displacementVel = Vector3.zero;
            velValid = true;
        }

        public void SampleDisplacementVelInArea(ref Vector3 in__worldPos, out Vector3 displacement, out bool displacementValid, out Vector3 displacementVel, out bool velValid)
        {
            displacement = Vector3.zero;
            displacementValid = true;
            displacementVel = Vector3.zero;
            velValid = true;
        }

        public bool SampleHeight(ref Vector3 in__worldPos, out float height, float minSpatialLength = 0)
        {
            height = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal, float minSpatialLength = 0)
        {
            normal = Vector3.up;
            return true;
        }

        public bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 normal)
        {
            normal = Vector3.up;
            return true;
        }
    }
}
