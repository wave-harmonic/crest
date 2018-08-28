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
            int lod = LodDataAnimatedWaves.SuggestDataLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            return OceanRenderer.Instance.Builder._lodDataAnimWaves[lod].SampleDisplacement(ref worldPos, ref displacement);
        }

        public bool SampleHeight(ref Vector3 worldPos, ref float height)
        {
            int lod = LodDataAnimatedWaves.SuggestDataLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            height = OceanRenderer.Instance.Builder._lodDataAnimWaves[lod].GetHeight(ref worldPos);
            return true;
        }

        public void PrewarmForSamplingArea(Rect areaXZ)
        {
            _areaLod = LodDataAnimatedWaves.SuggestDataLOD(areaXZ);
        }
        public void PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            _areaLod = LodDataAnimatedWaves.SuggestDataLOD(areaXZ, minSpatialLength);
        }
        public bool SampleDisplacementInArea(ref Vector3 worldPos, ref Vector3 displacement)
        {
            return OceanRenderer.Instance.Builder._lodDataAnimWaves[_areaLod].SampleDisplacement(ref worldPos, ref displacement);
        }
        public bool SampleHeightInArea(ref Vector3 worldPos, ref float height)
        {
            height = OceanRenderer.Instance.Builder._lodDataAnimWaves[_areaLod].GetHeight(ref worldPos);
            return true;
        }

        public bool SampleNormal(ref Vector3 undisplacedWorldPos, ref Vector3 normal)
        {
            return SampleNormal(ref undisplacedWorldPos, ref normal, 0f);
        }
        public bool SampleNormal(ref Vector3 undisplacedWorldPos, ref Vector3 normal, float minSpatialLength)
        {
            // select lod. this now has a 1 texel buffer, so the finite differences below should all be valid.
            PrewarmForSamplingArea(new Rect(undisplacedWorldPos.x, undisplacedWorldPos.z, 0f, 0f), minSpatialLength);

            float gridSize = OceanRenderer.Instance.Builder._lodDataAnimWaves[_areaLod].LodTransform._renderData._texelWidth;

            Vector3 dispCenter = Vector3.zero;
            if (!SampleDisplacementInArea(ref undisplacedWorldPos, ref dispCenter)) return false;
            Vector3 undisplacedWorldPosX = undisplacedWorldPos + Vector3.right * gridSize;
            Vector3 dispX = Vector3.zero;
            if (!SampleDisplacementInArea(ref undisplacedWorldPosX, ref dispX)) return false;
            Vector3 undisplacedWorldPosZ = undisplacedWorldPos + Vector3.forward * gridSize;
            Vector3 dispZ = Vector3.zero;
            if (!SampleDisplacementInArea(ref undisplacedWorldPosZ, ref dispZ)) return false;

            normal = Vector3.Cross(dispZ + Vector3.forward * gridSize - dispCenter, dispX + Vector3.right * gridSize - dispCenter).normalized;

            return true;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 worldPos, ref Vector3 undisplacedWorldPos)
        {
            // fpi - guess should converge to location that displaces to the target position
            Vector3 guess = worldPos;
            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            Vector3 disp = Vector3.zero;
            for (int i = 0; i < 4 && SampleDisplacement(ref guess, ref disp); i++)
            {
                Vector3 error = guess + disp - worldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }

            undisplacedWorldPos = guess;
            undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;

            return true;
        }
    }
}
