using System;
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

        public half[] _framesFlattened;
        [NonSerialized] public NativeArray<half> _framesFlattenedNative;

        public half _smallestValue;
        public half _largestValue;

        // Note - we dont use the 4th channel and should remove it if possible.
        // also, FPI will only need X&Z, not Y, so it may be best to store X&Z in one
        // array and Y in a separate one.
        public const int kFloatsPerPoint = 4;

        public void OnEnable()
        {
            InitData();
        }

        // Note that this is called when entering play mode, so it has to be as fast as possible
        private void InitData()
        {
            // Already loaded, or no data yet
            if (_framesFlattenedNative.Length > 0 || _framesFlattened == null)
                return;

            //Debug.Log(_framesFlattened.Length * 4);
            _framesFlattenedNative = new NativeArray<half>(_framesFlattened, Allocator.Persistent);
        }

        public void Initialize(float period, int textureResolution, int firstLod, int lodCount, float worldSize, int frameCount, half smallestValue, half largestValue, half[] framesFlattened)
        {
            _parameters = new FFTBakedDataParametersMultiRes()
            {
                _period = period,
                _frameCount = frameCount,
                _textureResolution = textureResolution,
                _firstLod = firstLod,
                _lodCount = lodCount,
            };

            _framesFlattened = framesFlattened;
            _smallestValue = smallestValue;
            _largestValue = largestValue;
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            InitData();
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
        public static float4 SampleHeightXZT(float4 x, float4 z, float4 t, FFTBakedDataParametersMultiRes parameters, in NativeArray<half> framesFlattened)
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
                SampleDisplacementFromXZ(x, z, lod, f0, parameters, in framesFlattened, out var _, out var dispY0, out var _);
                SampleDisplacementFromXZ(x, z, lod, f1, parameters, in framesFlattened, out var _, out var dispY1, out var _);

                result += math.lerp(dispY0, dispY1, alphaT);
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 InterpolateData(in NativeArray<half> framesFlattened, in SpatialInterpolationData lerpData, in FFTBakedDataParametersMultiRes parameters, in int lodIdx, in int4 frameIndex, in int channelIdx)
        {
            // lookup 4 values
            var textureResolution2 = parameters._textureResolution * parameters._textureResolution;
            var lodOffset = lodIdx - parameters._firstLod;
            var indexBase = kFloatsPerPoint * frameIndex * textureResolution2 * parameters._lodCount + kFloatsPerPoint * textureResolution2 * lodOffset;

            // It may be that we can do a bunch of these calculations just once and store in SpatialInterpolationData
            var rowLength = kFloatsPerPoint * parameters._textureResolution;
            var v00 = ElementsAt(framesFlattened, indexBase + lerpData._V0 * rowLength + lerpData._U0 * kFloatsPerPoint + channelIdx);
            var v10 = ElementsAt(framesFlattened, indexBase + lerpData._V0 * rowLength + lerpData._U1 * kFloatsPerPoint + channelIdx);
            var v01 = ElementsAt(framesFlattened, indexBase + lerpData._V1 * rowLength + lerpData._U0 * kFloatsPerPoint + channelIdx);
            var v11 = ElementsAt(framesFlattened, indexBase + lerpData._V1 * rowLength + lerpData._U1 * kFloatsPerPoint + channelIdx);

            // Lerp u direction first
            var v_0 = math.lerp(v00, v10, lerpData._alphaU);
            var v_1 = math.lerp(v01, v11, lerpData._alphaU);

            // Lerp v direction
            return math.lerp(v_0, v_1, lerpData._alphaV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SampleDisplacementFromXZ(in float4 x, in float4 z, in int lodIdx, in int4 frameIndex, in FFTBakedDataParametersMultiRes parameters, in NativeArray<half> framesFlattened, out float4 dispX, out float4 dispY, out float4 dispZ)
        {
            // Spatial lerp data
            SpatialInterpolationData lerpData = new SpatialInterpolationData();
            CalculateSamplingData(x, z, ref lerpData, in parameters, lodIdx);

            dispX = InterpolateData(framesFlattened, lerpData, parameters, lodIdx, frameIndex, 0);
            dispY = InterpolateData(framesFlattened, lerpData, parameters, lodIdx, frameIndex, 1);
            dispZ = InterpolateData(framesFlattened, lerpData, parameters, lodIdx, frameIndex, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static half4 ElementsAt(NativeArray<half> array, int4 indices) =>
            new half4(array[indices[0]], array[indices[1]], array[indices[2]], array[indices[3]]);
    }
}
