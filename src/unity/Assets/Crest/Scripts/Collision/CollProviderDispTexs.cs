// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// This collision provider reads back the displacement textures from the GPU. This means all shape is automatically
    /// included and the shape is relatively cheap to read. Be aware however that there is a ~2 frame latency involved for
    /// this collision provider type.
    /// </summary>
    public class CollProviderDispTexs : ICollProvider
    {
        int _areaLod = -1;

        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            int lod = WaveDataCam.SuggestCollisionLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            return OceanRenderer.Instance.Builder._shapeWDCs[lod].CollData.SampleDisplacement(ref worldPos, ref displacement);
        }

        public bool SampleHeight(ref Vector3 worldPos, ref float height)
        {
            int lod = WaveDataCam.SuggestCollisionLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            height = OceanRenderer.Instance.Builder._shapeWDCs[lod].CollData.GetHeight(ref worldPos);
            return true;
        }

        public void PrewarmForSamplingArea(Rect areaXZ)
        {
            _areaLod = WaveDataCam.SuggestCollisionLOD(areaXZ);
        }
        public void PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            _areaLod = WaveDataCam.SuggestCollisionLOD(areaXZ, minSpatialLength);
        }
        public bool SampleDisplacementInArea(ref Vector3 worldPos, ref Vector3 displacement)
        {
            return OceanRenderer.Instance.Builder._shapeWDCs[_areaLod].CollData.SampleDisplacement(ref worldPos, ref displacement);
        }
        public bool SampleHeightInArea(ref Vector3 worldPos, ref float height)
        {
            height = OceanRenderer.Instance.Builder._shapeWDCs[_areaLod].CollData.GetHeight(ref worldPos);
            return true;
        }
    }
}
