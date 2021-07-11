using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crest
{
    // TODO: these fields are useful in the inspector, but they should be read only (grayed out), how? property drawer?
    // Huw: there's something for this in crest, my collaborator dale will know.
    [Serializable]
    public struct FFTBakedDataParameters
    {
        public float _period;
        public int _frameCount;
        public int _textureResolution;
        public float _worldSize;
    }
    
    [PreferBinarySerialization]
    public class FFTBakedData : ScriptableObject 
    {
        public FFTBakedDataParameters _parameters;
        [NonSerialized] public NativeArray<half> _framesFlattenedNative;
        [HideInInspector] public string _framesFileName;
        
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
                var fileSize = _parameters._textureResolution * _parameters._textureResolution * _parameters._frameCount * sizeof(ushort); 
                var bytesArray = reader.ReadBytes(fileSize);
                _framesFlattenedNative = new NativeArray<byte>(bytesArray, Allocator.Persistent).Reinterpret<half>(sizeof(byte));
            }
            Resources.UnloadAsset(asset);
        }

        public void Initialize(float period, int textureResolution, float worldSize, int frameCount, half smallestValue, half largestValue, string framesFileName)
        {
            _parameters = new FFTBakedDataParameters()
            {
                _period = period,
                _frameCount = frameCount,
                _textureResolution = textureResolution,
                _worldSize = worldSize
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateSamplingData(float4 x, float4 z, ref SpatialInterpolationData lerpData, in FFTBakedDataParameters parameters)
        {
            // 0-1 uv
            var u01 = x / parameters._worldSize;

            u01 = math.select(u01 % 1f, 1f - (math.abs(u01) % 1f), u01 >= 0f);

            var v01 = z / parameters._worldSize;

            // Inversion differs compared to u, because cpu texture data stored from top left,
            // rather than gpu (top right)
            v01 = math.select(1f - (v01 % 1f), math.abs(v01) % 1f, v01 >= 0f);

            // uv in texels
            var uTexels = u01 * parameters._textureResolution;
            var vTexels = v01 * parameters._textureResolution;

            // offset for texel center
            uTexels -= 0.5f;
            vTexels -= 0.5f;
            uTexels = math.select(uTexels + parameters._textureResolution, uTexels, uTexels < 0f);
            vTexels = math.select(vTexels + parameters._textureResolution, vTexels, vTexels < 0f);

            lerpData._alphaU = uTexels % 1f;
            lerpData._alphaV = vTexels % 1f;
            lerpData._U0 = (int4)uTexels;
            lerpData._V0 = (int4)vTexels;
            lerpData._U1 = (lerpData._U0 + 1) % parameters._textureResolution;
            lerpData._V1 = (lerpData._V0 + 1) % parameters._textureResolution;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float4 SampleHeight(ref SpatialInterpolationData lerpData, int frameIndex)
        {
            // lookup 4 values
            var indexBase = frameIndex * _parameters._textureResolution * _parameters._textureResolution;
            var h00 = ElementsAt(_framesFlattenedNative, indexBase + lerpData._V0 * _parameters._textureResolution + lerpData._U0);
            var h10 = ElementsAt(_framesFlattenedNative, indexBase + lerpData._V0 * _parameters._textureResolution + lerpData._U1);
            var h01 = ElementsAt(_framesFlattenedNative, indexBase + lerpData._V1 * _parameters._textureResolution + lerpData._U0);
            var h11 = ElementsAt(_framesFlattenedNative, indexBase + lerpData._V1 * _parameters._textureResolution + lerpData._U1);

            // lerp u direction first
            var h_0 = math.lerp(h00, h10, lerpData._alphaU);
            var h_1 = math.lerp(h01, h11, lerpData._alphaU);

            // lerp v direction
            return math.lerp(h_0, h_1, lerpData._alphaV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 SampleHeight(float x, float z, float t)
        {
            // Validation sine waves
            //if (x < 0f)
            //    return 4f * Mathf.Sin(2f * (z - t * 8f) * Mathf.PI / _worldSize);
            //if (z < 0f)
            //    return 4f * Mathf.Sin(2f * (x - t * 8f) * Mathf.PI / _worldSize);

            // Temporal lerp
            var t01 = t / _parameters._period;
            if (t01 >= 0f)
            {
                t01 = t01 % 1f;
            }
            else
            {
                t01 = 1f - (math.abs(t01) % 1f);
            }
            var f0 = (int)(t01 * _parameters._frameCount);
            var f1 = (f0 + 1) % _parameters._frameCount;
            var alphaT = t01 * _parameters._frameCount - f0;

            // Spatial lerp data
            SpatialInterpolationData lerpData = new SpatialInterpolationData();
            CalculateSamplingData(x, z, ref lerpData, in _parameters);

            var h0 = SampleHeight(ref lerpData, f0);
            var h1 = SampleHeight(ref lerpData, f1);

            return math.lerp(h0, h1, alphaT);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 SampleHeightBurst(float4 x, float4 z, float4 t, FFTBakedDataParameters parameters, in NativeArray<half> framesFlattened)
        {
            // Temporal lerp
            var t01 = t / parameters._period;

            t01 = math.select(t01 % 1f, 1f - (math.abs(t01) % 1f), t01 >= 0f);
            var f0 = (int4)(t01 * parameters._frameCount);
            var f1 = (f0 + 1) % parameters._frameCount;
            var alphaT = t01 * parameters._frameCount - f0;
        
            // Spatial lerp data
            SpatialInterpolationData lerpData = new SpatialInterpolationData();
            CalculateSamplingData(x, z, ref lerpData, in parameters);
        
            var h0 = SampleHeightBurst(ref lerpData, f0, parameters._textureResolution, in framesFlattened);
            var h1 = SampleHeightBurst(ref lerpData, f1, parameters._textureResolution, in framesFlattened);
        
            return math.lerp(h0, h1, alphaT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 SampleHeightBurst(ref SpatialInterpolationData lerpData, int4 frameIndex, int textureResolution, in NativeArray<half> framesFlattened)
        {
            // lookup 4 values
            var indexBase = frameIndex * textureResolution * textureResolution;
            var h00 = ElementsAt(framesFlattened, indexBase + lerpData._V0 * textureResolution + lerpData._U0);
            var h10 = ElementsAt(framesFlattened, indexBase + lerpData._V0 * textureResolution + lerpData._U1);
            var h01 = ElementsAt(framesFlattened, indexBase + lerpData._V1 * textureResolution + lerpData._U0);
            var h11 = ElementsAt(framesFlattened, indexBase + lerpData._V1 * textureResolution + lerpData._U1);

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
