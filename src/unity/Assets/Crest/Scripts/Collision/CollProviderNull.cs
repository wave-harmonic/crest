using UnityEngine;

namespace Crest
{
    public class CollProviderNull : ICollProvider
    {
        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, float minSpatialLength)
        {
            return AvailabilityResult.DataAvailable;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, out Vector3 undisplacedWorldPos, float minSpatialLength = 0)
        {
            undisplacedWorldPos = i_worldPos;
            undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            return true;
        }

        public bool SampleDisplacement(ref Vector3 i_worldPos, out Vector3 o_displacement, float minSpatialLength = 0)
        {
            o_displacement = Vector3.zero;
            return true;
        }

        public bool SampleDisplacementInArea(ref Vector3 i_worldPos, out Vector3 o_displacement)
        {
            o_displacement = Vector3.zero;
            return true;
        }

        public void SampleDisplacementVel(ref Vector3 i_worldPos, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid, float minSpatialLength = 0)
        {
            o_displacement = Vector3.zero;
            o_displacementValid = true;
            o_displacementVel = Vector3.zero;
            o_velValid = true;
        }

        public void SampleDisplacementVelInArea(ref Vector3 i_worldPos, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid)
        {
            o_displacement = Vector3.zero;
            o_displacementValid = true;
            o_displacementVel = Vector3.zero;
            o_velValid = true;
        }

        public bool SampleHeight(ref Vector3 i_worldPos, out float height, float minSpatialLength = 0)
        {
            height = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal, float minSpatialLength = 0)
        {
            o_normal = Vector3.up;
            return true;
        }

        public bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal)
        {
            o_normal = Vector3.up;
            return true;
        }
    }
}
