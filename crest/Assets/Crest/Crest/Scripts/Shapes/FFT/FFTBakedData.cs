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
        /*[HideInInspector]*/ public int _frameCount;
        /*[HideInInspector]*/ public int _textureResolution;
        public float _worldSize;

        public int _frameToPreview = 0;

        public void Initialize(float period, float timeResolution, float[][] frames, int textureResolution, float worldSize)
        {
            _period = period;
            _timeResolution = timeResolution;
            _textureResolution = textureResolution;
            _worldSize = worldSize;
            _framesFlattened = frames.SelectMany(x => x).ToArray();
            _frameCount = frames.Length;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        float SampleHeight(float x, float z, int frameIndex)
        {
            // 0-1 uv
            var u = x / _worldSize % 1f;
            if (u < 0f) u += 1f;
            var v = z / _worldSize % 1f;
            if (v < 0f) v += 1f;

            // uv in texels
            var uTexels = u * _textureResolution;
            var vTexels = v * _textureResolution;

            // offset for texel center
            uTexels -= 0.5f;
            vTexels -= 0.5f;
            if (uTexels < 0f) uTexels += _textureResolution;
            if (vTexels < 0f) vTexels += _textureResolution;

            var alphaU = uTexels % 1f;
            var alphaV = vTexels % 1f;
            var iU0 = (int)uTexels;
            var iV0 = (int)vTexels;
            var iU1 = (iU0 + 1) % _textureResolution;
            var iV1 = (iV0 + 1) % _textureResolution;

            // lookup 4 values
            var indexBase = frameIndex * _textureResolution * _textureResolution;
            var h00 = _framesFlattened[indexBase + iV0 * _textureResolution + iU0];
            var h10 = _framesFlattened[indexBase + iV0 * _textureResolution + iU1];
            var h01 = _framesFlattened[indexBase + iV1 * _textureResolution + iU0];
            var h11 = _framesFlattened[indexBase + iV1 * _textureResolution + iU1];

            // lerp u direction first
            var h_0 = Mathf.Lerp(h00, h10, alphaU);
            var h_1 = Mathf.Lerp(h01, h11, alphaU);

            // lerp v direction
            return Mathf.Lerp(h_0, h_1, alphaV);
        }

        public float SampleHeight(float x, float z, float t)
        {
            if (Random.value < 0.5f)
                return 4f * Mathf.Sin(2f * 3.141593f * x / 64f - 2f * t);

            var t01 = (t / _period) % 1f;
            var f0 = (int)(t01 * _frameCount);
            var f1 = (f0 + 1) % _frameCount;
            var alphaT = t01 * _frameCount - f0;

            var h0 = SampleHeight(x, z, f0);
            var h1 = SampleHeight(x, z, f1);

            return Mathf.Lerp(h0, h1, alphaT);
        }
    }
}
