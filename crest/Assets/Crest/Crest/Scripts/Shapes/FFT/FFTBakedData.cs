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
            public float _alphaU;
            public float _alphaV;
            public int _U0;
            public int _V0;
            public int _U1;
            public int _V1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateSamplingData(float x, float z, ref SpatialInterpolationData lerpData, in FFTBakedDataParameters parameters)
        {
            // 0-1 uv
            var u01 = x / parameters._worldSize;
            if (u01 >= 0f)
            {
                u01 = u01 % 1f;
            }
            else
            {
                u01 = 1f - (Mathf.Abs(u01) % 1f);
            }

            var v01 = z / parameters._worldSize;
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
            var uTexels = u01 * parameters._textureResolution;
            var vTexels = v01 * parameters._textureResolution;

            // offset for texel center
            uTexels -= 0.5f;
            vTexels -= 0.5f;
            if (uTexels < 0f) uTexels += parameters._textureResolution;
            if (vTexels < 0f) vTexels += parameters._textureResolution;

            lerpData._alphaU = uTexels % 1f;
            lerpData._alphaV = vTexels % 1f;
            lerpData._U0 = (int)uTexels;
            lerpData._V0 = (int)vTexels;
            lerpData._U1 = (lerpData._U0 + 1) % parameters._textureResolution;
            lerpData._V1 = (lerpData._V0 + 1) % parameters._textureResolution;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float SampleHeight(ref SpatialInterpolationData lerpData, int frameIndex)
        {
            // lookup 4 values
            var indexBase = frameIndex * _parameters._textureResolution * _parameters._textureResolution;
            var h00 = _framesFlattenedNative[indexBase + lerpData._V0 * _parameters._textureResolution + lerpData._U0];
            var h10 = _framesFlattenedNative[indexBase + lerpData._V0 * _parameters._textureResolution + lerpData._U1];
            var h01 = _framesFlattenedNative[indexBase + lerpData._V1 * _parameters._textureResolution + lerpData._U0];
            var h11 = _framesFlattenedNative[indexBase + lerpData._V1 * _parameters._textureResolution + lerpData._U1];

            // lerp u direction first
            var h_0 = Mathf.Lerp(h00, h10, lerpData._alphaU);
            var h_1 = Mathf.Lerp(h01, h11, lerpData._alphaU);

            // lerp v direction
            return Mathf.Lerp(h_0, h_1, lerpData._alphaV);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SampleHeight(float x, float z, float t)
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
                t01 = 1f - (Mathf.Abs(t01) % 1f);
            }
            var f0 = (int)(t01 * _parameters._frameCount);
            var f1 = (f0 + 1) % _parameters._frameCount;
            var alphaT = t01 * _parameters._frameCount - f0;

            // Spatial lerp data
            SpatialInterpolationData lerpData = new SpatialInterpolationData();
            CalculateSamplingData(x, z, ref lerpData, in _parameters);

            var h0 = SampleHeight(ref lerpData, f0);
            var h1 = SampleHeight(ref lerpData, f1);

            return Mathf.Lerp(h0, h1, alphaT);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SampleHeightBurst(float x, float z, float t, FFTBakedDataParameters parameters, in NativeArray<half> framesFlattened)
        {
            // Temporal lerp
            var t01 = t / parameters._period;
            if (t01 >= 0f)
            {
                t01 = t01 % 1f;
            }
            else
            {
                t01 = 1f - (Mathf.Abs(t01) % 1f);
            }
            var f0 = (int)(t01 * parameters._frameCount);
            var f1 = (f0 + 1) % parameters._frameCount;
            var alphaT = t01 * parameters._frameCount - f0;
        
            // Spatial lerp data
            SpatialInterpolationData lerpData = new SpatialInterpolationData();
            CalculateSamplingData(x, z, ref lerpData, in parameters);
        
            var h0 = SampleHeightBurst(ref lerpData, f0, parameters._textureResolution, in framesFlattened);
            var h1 = SampleHeightBurst(ref lerpData, f1, parameters._textureResolution, in framesFlattened);
        
            return Mathf.Lerp(h0, h1, alphaT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float SampleHeightBurst(ref SpatialInterpolationData lerpData, int frameIndex, int textureResolution, in NativeArray<half> framesFlattened)
        {
            // lookup 4 values
            var indexBase = frameIndex * textureResolution * textureResolution;
            var h00 = framesFlattened[indexBase + lerpData._V0 * textureResolution + lerpData._U0];
            var h10 = framesFlattened[indexBase + lerpData._V0 * textureResolution + lerpData._U1];
            var h01 = framesFlattened[indexBase + lerpData._V1 * textureResolution + lerpData._U0];
            var h11 = framesFlattened[indexBase + lerpData._V1 * textureResolution + lerpData._U1];

            // lerp u direction first
            var h_0 = Mathf.Lerp(h00, h10, lerpData._alphaU);
            var h_1 = Mathf.Lerp(h01, h11, lerpData._alphaU);

            // lerp v direction
            return Mathf.Lerp(h_0, h_1, lerpData._alphaV);
        }
    }
}
