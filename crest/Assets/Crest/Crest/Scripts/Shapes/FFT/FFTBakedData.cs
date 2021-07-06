using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    [PreferBinarySerialization] // improves filesize almost 4-fold, doesn't seem to impact editor performance
    public class FFTBakedData : ScriptableObject
    {
        // TODO: these fields are useful in the inspector, but they should be read only (grayed out), how? property drawer?
        // Huw: there's something for this in crest, my collaborator dale will know.
        [SerializeField] private float _period;
        [SerializeField] private float _timeResolution;
        [HideInInspector] public float[] _framesFlattened;
        /*[HideInInspector]*/
        public int _frameCount;
        /*[HideInInspector]*/
        public int _textureResolution;
        public float _worldSize;
        [HideInInspector] public float _smallestValue;
        [HideInInspector] public float _largestValue;

        // public int _frameToPreview = 0;

        public void Initialize(float period, float timeResolution, float[][] frames, int textureResolution, float worldSize)
        {
            _period = period;
            _timeResolution = timeResolution;
            _textureResolution = textureResolution;
            _worldSize = worldSize;
            _framesFlattened = frames.SelectMany(x => x).ToArray();
            _frameCount = frames.Length;
            _smallestValue = _framesFlattened.Min();
            _largestValue = _framesFlattened.Max();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        struct SpatialInterpolationData
        {
            public float _alphaU;
            public float _alphaV;
            public int _U0;
            public int _V0;
            public int _U1;
            public int _V1;
        }

        void CalculateSamplingData(float x, float z, ref SpatialInterpolationData lerpData)
        {
            // 0-1 uv
            var u01 = x / _worldSize;
            if (u01 >= 0f)
            {
                u01 = u01 % 1f;
            }
            else
            {
                u01 = 1f - (Mathf.Abs(u01) % 1f);
            }

            var v01 = z / _worldSize;
            if (v01 >= 0f)
            {
                // Inversion differs compared to u, because cpu texture data stored from top left,
                // rather than gpu (top right)
                v01 = 1f - (v01 % 1f);
            }
            else
            {
                v01 = Mathf.Abs(v01) % 1f;
            }

            // uv in texels
            var uTexels = u01 * _textureResolution;
            var vTexels = v01 * _textureResolution;

            // offset for texel center
            uTexels -= 0.5f;
            vTexels -= 0.5f;
            if (uTexels < 0f) uTexels += _textureResolution;
            if (vTexels < 0f) vTexels += _textureResolution;

            lerpData._alphaU = uTexels % 1f;
            lerpData._alphaV = vTexels % 1f;
            lerpData._U0 = (int)uTexels;
            lerpData._V0 = (int)vTexels;
            lerpData._U1 = (lerpData._U0 + 1) % _textureResolution;
            lerpData._V1 = (lerpData._V0 + 1) % _textureResolution;
        }

        float SampleHeight(ref SpatialInterpolationData lerpData, int frameIndex)
        {
            // lookup 4 values
            var indexBase = frameIndex * _textureResolution * _textureResolution;
            var h00 = _framesFlattened[indexBase + lerpData._V0 * _textureResolution + lerpData._U0];
            var h10 = _framesFlattened[indexBase + lerpData._V0 * _textureResolution + lerpData._U1];
            var h01 = _framesFlattened[indexBase + lerpData._V1 * _textureResolution + lerpData._U0];
            var h11 = _framesFlattened[indexBase + lerpData._V1 * _textureResolution + lerpData._U1];

            // lerp u direction first
            var h_0 = Mathf.Lerp(h00, h10, lerpData._alphaU);
            var h_1 = Mathf.Lerp(h01, h11, lerpData._alphaU);

            // lerp v direction
            return Mathf.Lerp(h_0, h_1, lerpData._alphaV);
        }

        public float SampleHeight(float x, float z, float t)
        {
            // Validation sine waves
            //if (x < 0f)
            //    return 4f * Mathf.Sin(2f * (z - t * 8f) * Mathf.PI / _worldSize);
            //if (z < 0f)
            //    return 4f * Mathf.Sin(2f * (x - t * 8f) * Mathf.PI / _worldSize);

            // Temporal lerp
            var t01 = t / _period;
            if (t01 >= 0f)
            {
                t01 = t01 % 1f;
            }
            else
            {
                t01 = 1f - (Mathf.Abs(t01) % 1f);
            }
            var f0 = (int)(t01 * _frameCount);
            var f1 = (f0 + 1) % _frameCount;
            var alphaT = t01 * _frameCount - f0;

            // Spatial lerp data
            SpatialInterpolationData lerpData = new SpatialInterpolationData();
            CalculateSamplingData(x, z, ref lerpData);

            var h0 = SampleHeight(ref lerpData, f0);
            var h1 = SampleHeight(ref lerpData, f1);

            return Mathf.Lerp(h0, h1, alphaT);
        }
    }
}
