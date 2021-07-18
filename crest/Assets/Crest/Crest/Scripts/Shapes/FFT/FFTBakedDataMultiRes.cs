using System;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    // TODO: these fields are useful in the inspector, but they should be read only (grayed out), how? property drawer?
    // Huw: there's something for this in crest, my collaborator dale will know.
    [Serializable]
    public struct FFTBakedDataParametersMultiRes
    {
        public float _period;
        public int _frameCount;
        public int _textureResolution;
        public int _firstLod;
        public int _lodCount;
    }

    [PreferBinarySerialization]
    public class FFTBakedDataMultiRes : ScriptableObject
    {
        // One frame contains multiple levels of detail
        //   width = _textureResolution
        //   height = _textureResolution * _lodCount
        // -------
        // | LOD |
        // |  0  |
        // |-----|
        // | LOD |
        // |  1  |
        // |-----|
        // ~     ~
        // |-----|
        // | LOD | N == _lodCount
        // |  N  |
        // -------
        public FFTBakedDataParametersMultiRes _parameters;
        [NonSerialized] public NativeArray<half> _framesFlattenedNative;
        public string _framesFileName;

        public half _smallestValue;
        public half _largestValue;

        public void OnEnable()
        {
            if (_framesFileName == null) // means we just created this object and we haven't initialized yet
                return;

            LoadFrames();
        }

        // Note that this is called when entering play mode, so it has to be as fast as possible
        private void LoadFrames()
        {
            if (_framesFlattenedNative.Length > 0) // already loaded
                return;

            var asset = Resources.Load(_framesFileName) as TextAsset; // TextAsset is used for custom binary data
            if (asset == null)
                Debug.LogError("Failed to load baked frames from Resources");

            var stream = new MemoryStream(asset.bytes);

            using (BinaryReader reader = new BinaryReader(stream))
            {
                // half uses ushort for its value under the hood
                var fileSize = _parameters._textureResolution * _parameters._textureResolution * _parameters._lodCount * _parameters._frameCount * sizeof(ushort);
                var bytesArray = reader.ReadBytes(fileSize);
                _framesFlattenedNative = new NativeArray<byte>(bytesArray, Allocator.Persistent).Reinterpret<half>(sizeof(byte));
            }
            Resources.UnloadAsset(asset);
        }

        public void Initialize(float period, int textureResolution, int firstLod, int lodCount, float worldSize, int frameCount, half smallestValue, half largestValue, string framesFileName)
        {
            _parameters = new FFTBakedDataParametersMultiRes()
            {
                _period = period,
                _frameCount = frameCount,
                _textureResolution = textureResolution,
                _firstLod = firstLod,
                _lodCount = lodCount,
                //_worldSize = worldSize
            };

            _framesFileName = framesFileName;
            _smallestValue = smallestValue;
            _largestValue = largestValue;
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            LoadFrames();
        }

        public void OnDisable()
        {
            if (_framesFlattenedNative.IsCreated)
                _framesFlattenedNative.Dispose();
        }

        public void OnDestroy()
        {
            if (_framesFlattenedNative.IsCreated)
                _framesFlattenedNative.Dispose();
        }

        struct SpatialInterpolationData
        {
            public float4 _alphaU;
            public float4 _alphaV;
            public int4 _U0;
            public int4 _V0;
            public int4 _U1;
            public int4 _V1;
            public int4 _indexBase;
        }

        // 0.462ms - 4096
        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateSamplingData(float4 x, float4 z, ref SpatialInterpolationData lerpData, in FFTBakedDataParametersMultiRes parameters, int lodIdx)
        {
            float worldSize = 0.5f * (1 << lodIdx);

            // 0-1 uv
            var u01 = x / worldSize;

            u01 = math.select(1f - (math.abs(u01) % 1f), u01 % 1f, u01 >= 0f);

            var v01 = z / worldSize;

            // Inversion differs compared to u, because cpu texture data stored from top left,
            // rather than gpu (top right)
            //v01 = math.select(math.abs(v01) % 1f, 1f - (v01 % 1f), v01 >= 0f);
            // Huw: unreverted this after making spectrum highly directional and testing different
            // angles. if this holds then could compute u and v together
            v01 = math.select(1f - (math.abs(v01) % 1f), v01 % 1f, v01 >= 0f);

            // uv in texels
            var uTexels = u01 * parameters._textureResolution;
            var vTexels = v01 * parameters._textureResolution;

            // offset for texel center
            uTexels -= 0.5f;
            vTexels -= 0.5f;
            uTexels = math.select(uTexels, uTexels + parameters._textureResolution, uTexels < 0f);
            vTexels = math.select(vTexels, vTexels + parameters._textureResolution, vTexels < 0f);

            lerpData._alphaU = uTexels % 1f;
            lerpData._alphaV = vTexels % 1f;
            lerpData._U0 = (int4)uTexels;
            lerpData._V0 = (int4)vTexels;
            lerpData._U1 = (lerpData._U0 + 1) % parameters._textureResolution;
            lerpData._V1 = (lerpData._V0 + 1) % parameters._textureResolution;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 SampleHeightBurst(float4 x, float4 z, float4 t, FFTBakedDataParametersMultiRes parameters, in NativeArray<half> framesFlattened)
        {
            // Temporal lerp
            var t01 = t / parameters._period;

            t01 = math.select(1f - (math.abs(t01) % 1f), t01 % 1f, t01 >= 0f);
            var f0 = (int4)(t01 * parameters._frameCount);
            var f1 = (f0 + 1) % parameters._frameCount;
            var alphaT = t01 * parameters._frameCount - f0;

            // sum up waves for all lods
            float4 result = 0f;
            for (var lod = parameters._firstLod; lod < parameters._lodCount; lod++)
            {
                // Spatial lerp data
                SpatialInterpolationData lerpData = new SpatialInterpolationData();
                CalculateSamplingData(x, z, ref lerpData, in parameters, lod);

                var h0 = SampleHeightBurst(ref lerpData, lod, f0, parameters, in framesFlattened);
                var h1 = SampleHeightBurst(ref lerpData, lod, f1, parameters, in framesFlattened);

                result += math.lerp(h0, h1, alphaT);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 SampleHeightBurst(ref SpatialInterpolationData lerpData, int lodIdx, int4 frameIndex, FFTBakedDataParametersMultiRes parameters, in NativeArray<half> framesFlattened)
        {
            // lookup 4 values
            var textureResolution2 = parameters._textureResolution * parameters._textureResolution;
            var lodOffset = lodIdx - parameters._firstLod;
            var indexBase = frameIndex * textureResolution2 * parameters._lodCount + textureResolution2 * lodOffset;

            var h00 = ElementsAt(framesFlattened, indexBase + lerpData._V0 * parameters._textureResolution + lerpData._U0);
            var h10 = ElementsAt(framesFlattened, indexBase + lerpData._V0 * parameters._textureResolution + lerpData._U1);
            var h01 = ElementsAt(framesFlattened, indexBase + lerpData._V1 * parameters._textureResolution + lerpData._U0);
            var h11 = ElementsAt(framesFlattened, indexBase + lerpData._V1 * parameters._textureResolution + lerpData._U1);

            // lerp u direction first
            var h_0 = math.lerp(h00, h10, lerpData._alphaU);
            var h_1 = math.lerp(h01, h11, lerpData._alphaU);

            // lerp v direction
            return math.lerp(h_0, h_1, lerpData._alphaV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static half4 ElementsAt(NativeArray<half> array, int4 indices) =>
            new half4(array[indices[0]], array[indices[1]], array[indices[2]], array[indices[3]]);
    }
}
