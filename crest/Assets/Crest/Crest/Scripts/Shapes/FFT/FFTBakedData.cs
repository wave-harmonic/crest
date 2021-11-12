// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Potential optimisations:
// - Store only 3 channels instead of 4, and store x&z displacement together separately from y as the
//   access patterns differ.
// - Potentially ensure lod count always multiple of 2 and 2x unroll the loops over lods. Maybe even
//   specialise the code for different LOD counts?
// - Wind speed can limit spectrum, this could be taken into account to limit number of lods in baked data
// - The period of the waves in each slice differ. Small slices will have smaller periods. Large slices likely
//   need less time samples. The required sample count could likely be reduced by sampling the period of each
//   slice by the frame count rather than sampling all slices with the same time samples. This would however
//   add an extra calculation during sampling which may slow down query time.
// - The queries could be made async and essentially free, by logging all queries and then processing them in
//   one batch. This would likely also improve performance. An example of this async setup is in PR 157.
// - We could potentially avoid starting a job for less than ~10 or so queries, assuming we can run the query
//   code directly and still be Burst compiled.

#if CREST_UNITY_MATHEMATICS

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Bake params.
    /// </summary>
    [Serializable]
    public struct FFTBakedDataParameters
    {
        public float _period;
        public int _frameCount;
        public int _textureResolution;
        public int _firstLod;
        public int _lodCount;
        public float _windSpeed;
    }

    /// <summary>
    /// The generated FFT slices stored for a bunch of time values. Used to give collision shape for CPU.
    /// </summary>
    [PreferBinarySerialization]
    public class FFTBakedData : ScriptableObject
    {
        public FFTBakedDataParameters _parameters;

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

        [SerializeField]
        half[] _framesFlattened;
        [NonSerialized]
        public NativeArray<half> _framesFlattenedNative;

        public half _smallestValue;
        public half _largestValue;

        // Note - we dont use the 4th channel and should remove it if possible.
        // also, FPI will only need X&Z, not Y, so it may be best to store X&Z in one
        // array and Y in a separate one.
        public const int kFloatsPerPoint = 4;

        // More iterations mean better match for choppy waves, but comes with more expense
        const int kIterationCount = 3;

        public void OnEnable()
        {
            InitData();
        }

        // Note that this is called when entering play mode, so it has to be as fast as possible
        private void InitData()
        {
            // Already loaded, or no data yet
            if (_framesFlattenedNative.Length > 0 || _framesFlattened == null)
            {
                return;
            }

            _framesFlattenedNative = new NativeArray<half>(_framesFlattened, Allocator.Persistent);
        }

        public void Initialize(float period, int textureResolution, int firstLod, int lodCount, float windSpeed, int frameCount, half smallestValue, half largestValue, half[] framesFlattened)
        {
            _parameters = new FFTBakedDataParameters()
            {
                _period = period,
                _frameCount = frameCount,
                _textureResolution = textureResolution,
                _firstLod = firstLod,
                _lodCount = lodCount,
                _windSpeed = windSpeed,
            };

            _framesFlattened = framesFlattened;
            _smallestValue = smallestValue;
            _largestValue = largestValue;

            InitData();
        }

        public void OnDisable()
        {
            if (_framesFlattenedNative.IsCreated)
            {
                _framesFlattenedNative.Dispose();
            }
        }

        public void OnDestroy()
        {
            if (_framesFlattenedNative.IsCreated)
            {
                _framesFlattenedNative.Dispose();
            }
        }

        /// <summary>
        /// Indices and interpolation weights required to perform bilinear interpolation.
        /// </summary>
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

        /// <summary>
        /// Computes indices and interpolation weights required to perform bilinear interpolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateSamplingData(float4 x, float4 z, ref SpatialInterpolationData lerpData, in FFTBakedDataParameters parameters, int lodIdx)
        {
            float worldSize = 0.5f * (1 << lodIdx);

            // 0-1 UV. Ensure we land in the positive quadrant. FPI moves the query point around
            // so we need to do this here.
            var u01 = x / worldSize;
            u01 = math.select(1f - (math.abs(u01) % 1f), u01 % 1f, u01 >= 0f);
            var v01 = z / worldSize;
            v01 = math.select(1f - (math.abs(v01) % 1f), v01 % 1f, v01 >= 0f);

            // UV in texels
            var uTexels = u01 * parameters._textureResolution;
            var vTexels = v01 * parameters._textureResolution;

            // Offset for texel center
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

        /// <summary>
        /// Takes a position x & z and a time t and evaluates the water height relative to sea level. It does an iteration
        /// to invert the displacement to get the correct height at this position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 SampleHeightXZT(float4 x, float4 z, float4 t, FFTBakedDataParameters parameters, in NativeArray<half> framesFlattened)
        {
            // Temporal lerp
            var t01 = t / parameters._period;

            t01 = math.select(1f - (math.abs(t01) % 1f), t01 % 1f, t01 >= 0f);
            var f0 = (int4)(t01 * parameters._frameCount);
            var f1 = (f0 + 1) % parameters._frameCount;
            var alphaT = t01 * parameters._frameCount - f0;

            // Compute height at each time and interpolate
            var dispY0 = SampleHeightXZWithInvert(x, z, f0, parameters, in framesFlattened);
            var dispY1 = SampleHeightXZWithInvert(x, z, f1, parameters, in framesFlattened);

            return math.lerp(dispY0, dispY1, alphaT);
        }

        /// <summary>
        /// Takes a position x & z and a frame index and evaluates the water height relative to sea level. It does an iteration
        /// to invert the displacement to get the correct height at this position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 SampleHeightXZWithInvert(float4 x, float4 z, int4 frameIndex, FFTBakedDataParameters parameters, in NativeArray<half> framesFlattened)
        {
            float4 displacedX = x;
            float4 displacedZ = z;

            // Fixed point iteration to search for the undisplaced position
            for (int i = 0; i < kIterationCount; i++)
            {
                SampleDisplacementFromXZ(x, z, frameIndex, parameters, framesFlattened, out var dispX, out _, out var dispZ);
                x = displacedX - dispX;
                z = displacedZ - dispZ;
            }

            SampleDisplacementFromXZ(x, z, frameIndex, parameters, framesFlattened, out _, out var dispY, out _);

            return dispY;
        }

        /// <summary>
        /// Performs bilinear interpolation of the specified channel at the given frame index, using the precalculated interpolation
        /// data to give indices and interpolation weights.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 InterpolateData(in NativeArray<half> framesFlattened, in SpatialInterpolationData lerpData, in FFTBakedDataParameters parameters, in int lodIdx, in int4 frameIndex, in int channelIdx)
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

        /// <summary>
        /// Takes a position x & z and a frame index and evaluates the water displacement relative to sea level. This does not
        /// invert the displacements - it assumes the input position is the undisplaced position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SampleDisplacementFromXZ(in float4 x, in float4 z, in int4 frameIndex, in FFTBakedDataParameters parameters, in NativeArray<half> framesFlattened, out float4 dispX, out float4 dispY, out float4 dispZ)
        {
            dispX = dispY = dispZ = float4.zero;

            // Spatial lerp data
            var lastLod = parameters._firstLod + parameters._lodCount;
            for (var lodIdx = parameters._firstLod; lodIdx < lastLod; lodIdx++)
            {
                SpatialInterpolationData lerpData = new SpatialInterpolationData();
                CalculateSamplingData(x, z, ref lerpData, in parameters, lodIdx);

                dispX += InterpolateData(framesFlattened, lerpData, parameters, lodIdx, frameIndex, 0);
                dispY += InterpolateData(framesFlattened, lerpData, parameters, lodIdx, frameIndex, 1);
                dispZ += InterpolateData(framesFlattened, lerpData, parameters, lodIdx, frameIndex, 2);
            }
        }

        /// <summary>
        /// Takes a position x & z and a time t and evaluates the water displacement relative to sea level. This does not
        /// invert the displacements - it assumes the input position is the undisplaced position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SampleDisplacementXZT(float4 x, float4 z, float4 t, FFTBakedDataParameters parameters, in NativeArray<half> framesFlattened, out float4 dispX, out float4 dispY, out float4 dispZ)
        {
            // Temporal lerp
            var t01 = t / parameters._period;

            t01 = math.select(1f - (math.abs(t01) % 1f), t01 % 1f, t01 >= 0f);
            var f0 = (int4)(t01 * parameters._frameCount);
            var f1 = (f0 + 1) % parameters._frameCount;
            var alphaT = t01 * parameters._frameCount - f0;

            // Compute height at each time and interpolate
            SampleDisplacementFromXZ(x, z, f0, parameters, in framesFlattened, out var dispX0, out var dispY0, out var dispZ0);
            SampleDisplacementFromXZ(x, z, f1, parameters, in framesFlattened, out var dispX1, out var dispY1, out var dispZ1);

            dispX = math.lerp(dispX0, dispX1, alphaT);
            dispY = math.lerp(dispY0, dispY1, alphaT);
            dispZ = math.lerp(dispZ0, dispZ1, alphaT);
        }

        /// <summary>
        /// Helper that returns a vector of the values of array at the provided indices.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static half4 ElementsAt(NativeArray<half> array, int4 indices) =>
            new half4(array[indices[0]], array[indices[1]], array[indices[2]], array[indices[3]]);
    }

#if UNITY_EDITOR
    /// <summary>
    /// FFTBakedData inspector makes all fields disabled as they should not be edited manually.
    /// </summary>
    [CustomEditor(typeof(FFTBakedData))]
    public class FFTBakedDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_parameters"));

            var targetData = target as FFTBakedData;
            EditorGUILayout.FloatField("Smallest height", targetData._smallestValue);
            EditorGUILayout.FloatField("Largest height", targetData._largestValue);

            GUI.enabled = true;
        }
    }
#endif // UNITY_EDITOR
}

#endif // CREST_UNITY_MATHEMATICS
