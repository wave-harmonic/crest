using UnityEngine;

namespace Crest
{
    public class GPUReadbackDisps : GPUReadbackBase<LodDataAnimatedWaves>, ICollProvider
    {
        #region ICollProvider
        public bool ComputeUndisplacedPosition(ref Vector3 in__worldPos, out Vector3 undisplacedWorldPos)
        {
            throw new System.NotImplementedException();
        }

        public bool PrewarmForSamplingArea(Rect areaXZ)
        {
            throw new System.NotImplementedException();
        }

        public bool PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleDisplacement(ref Vector3 in__worldPos, out Vector3 displacement)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleDisplacement(ref Vector3 in__worldPos, out Vector3 displacement, float minSpatialLength)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleDisplacementInArea(ref Vector3 in__worldPos, out Vector3 displacement)
        {
            throw new System.NotImplementedException();
        }

        public void SampleDisplacementVel(ref Vector3 in__worldPos, out Vector3 displacement, out bool displacementValid, out Vector3 displacementVel, out bool velValid, float minSpatialLength)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleHeight(ref Vector3 in__worldPos, out float height)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleHeightInArea(ref Vector3 in__worldPos, out float height)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal)
        {
            throw new System.NotImplementedException();
        }

        public bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal, float minSpatialLength)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}
