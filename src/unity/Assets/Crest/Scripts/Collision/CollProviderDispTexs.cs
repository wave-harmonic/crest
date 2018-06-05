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
        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            int lod = SuggestCollisionLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            return OceanRenderer.Instance.Builder._shapeWDCs[lod].SampleDisplacement(ref worldPos, ref displacement);
        }

        public bool SampleHeight(ref Vector3 worldPos, ref float height)
        {
            int lod = SuggestCollisionLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            height = OceanRenderer.Instance.Builder._shapeWDCs[lod].GetHeight(ref worldPos);
            return true;
        }

        /// <summary>
        /// Returns index of lod that completely covers the sample area, and contains wavelengths that repeat no more than twice across the smaller
        /// spatial length. If no such lod available, returns -1. This means high frequency wavelengths are filtered out, and the lod index can
        /// be used for each sample in the sample area.
        /// </summary>
        int SuggestCollisionLOD(Rect sampleAreaXZ)
        {
            return SuggestCollisionLOD(sampleAreaXZ, Mathf.Min(sampleAreaXZ.width, sampleAreaXZ.height));
        }
        int SuggestCollisionLOD(Rect sampleAreaXZ, float minSpatialLength)
        {
            var wdcs = OceanRenderer.Instance.Builder._shapeWDCs;
            for (int lod = 0; lod < wdcs.Length; lod++)
            {
                // shape texture needs to completely contain sample area
                var wdc = wdcs[lod];
                var wdcRect = wdc.CollisionDataRectXZ;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                    continue;

                // the smallest wavelengths should repeat no more than twice across the smaller spatial length
                var minWL = wdc.MaxWavelength() / 2f;
                if (minWL < minSpatialLength / 2f)
                    continue;

                return lod;
            }

            return -1;
        }

        int _areaLod = -1;

        public void PrewarmForSamplingArea(Rect areaXZ)
        {
            _areaLod = SuggestCollisionLOD(areaXZ);
        }
        public void PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            _areaLod = SuggestCollisionLOD(areaXZ, minSpatialLength);
        }
        public bool SampleDisplacementInArea(ref Vector3 worldPos, ref Vector3 displacement)
        {
            return OceanRenderer.Instance.Builder._shapeWDCs[_areaLod].SampleDisplacement(ref worldPos, ref displacement);
        }
        public bool SampleHeightInArea(ref Vector3 worldPos, ref float height)
        {
            height = OceanRenderer.Instance.Builder._shapeWDCs[_areaLod].GetHeight(ref worldPos);
            return true;
        }
    }
}
