// Crest Ocean System

// Copyright 2021 Wave Harmonic Ltd

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

//#define CREST_DEBUG_DUMP_EXRS

using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class FFTBaker
    {
        //public static FFTBakedData Bake(ShapeFFT fftWaves, int resolutionSpace, int resolutionTime, float wavePatchSize,
        //    float loopPeriod)
        //{
        //    Debug.Assert(Mathf.IsPowerOfTwo(resolutionSpace), "Crest: Spatial resolution must be power of 2");
        //    // Debug.Assert(Mathf.IsPowerOfTwo(resolutionTime), "Crest: Temporal resolution must be power of 2"); // seems unnecessary
        //    Debug.Assert(Mathf.IsPowerOfTwo((int) wavePatchSize), "Crest: Spatial path size must be power of 2");

        //    // create the staging texture wavePatchData that is written to then downloaded from the GPU
        //    var desc = new RenderTextureDescriptor
        //    {
        //        width = resolutionSpace,
        //        height = resolutionSpace,
        //        volumeDepth = 1,
        //        autoGenerateMips = false,
        //        colorFormat = RenderTextureFormat.ARGBFloat,
        //        enableRandomWrite = true,
        //        dimension = TextureDimension.Tex2D,
        //        depthBufferBits = 0,
        //        msaaSamples = 1,
        //        sRGB = false,
        //    };
        //    var wavePatchData = new RenderTexture(desc);
        //    wavePatchData.Create();

        //    var stagingTexture = new Texture2D(desc.width, desc.height, TextureFormat.RHalf, false, true);
        //    var buf = new CommandBuffer();

        //    var waveCombineShader = Resources.Load<ComputeShader>("FFT/FFTBake");
        //    var kernel = waveCombineShader.FindKernel("FFTBake");

        //    var frameCount = (int) (resolutionTime * loopPeriod);
        //    var frames = new half[frameCount][];

        //    for (int timeIndex = 0; timeIndex < frameCount; timeIndex++) // this means resolutionTime is actually FPS
        //    {
        //        float t = timeIndex / (float) resolutionTime;

        //        buf.Clear();

        //        // Generate multi-res FFT into a texture array
        //        var fftWaveDataTA = FFTCompute.GenerateDisplacements(buf, fftWaves._resolution, loopPeriod,
        //            fftWaves._windTurbulence, fftWaves.WindDirRadForFFT, fftWaves.WindSpeedForFFT, t,
        //            fftWaves._spectrum, true);

        //        // Compute shader generates the final waves
        //        buf.SetComputeFloatParam(waveCombineShader, "_BakeTime", t);
        //        buf.SetComputeFloatParam(waveCombineShader, "_WavePatchSize", wavePatchSize);
        //        buf.SetComputeTextureParam(waveCombineShader, kernel, "_InFFTWaves", fftWaveDataTA);
        //        buf.SetComputeTextureParam(waveCombineShader, kernel, "_OutHeights", wavePatchData);
        //        buf.DispatchCompute(waveCombineShader, kernel, resolutionSpace / 8, resolutionSpace / 8, 1);

        //        Graphics.ExecuteCommandBuffer(buf);

        //        // Readback data to CPU
        //        RenderTexture.active = wavePatchData;
        //        stagingTexture.ReadPixels(new Rect(0, 0, wavePatchData.width, wavePatchData.height), 0, 0);

        //        frames[timeIndex] = stagingTexture.GetRawTextureData<half>().ToArray();
        //    }

        //    var framesFlattened = frames.SelectMany(x => x).ToArray();
        //    var framesFileName = "frames";
        //    const string folderName = "BakedWave";
            
        //    SaveFramesToFile(framesFileName, framesFlattened);
            
        //    var bakedDataSO = ScriptableObject.CreateInstance<FFTBakedData>();
        //    var framesAsFloats = framesFlattened.Select(x => (float)x);
        //    bakedDataSO.Initialize(
        //        loopPeriod,
        //        resolutionSpace,
        //        wavePatchSize,
        //        frames.Length,
        //        new half(framesAsFloats.Min()),
        //        new half(framesAsFloats.Max()),
        //        framesFileName);

        //    SaveBakedDataAsset(bakedDataSO, folderName);

        //    return bakedDataSO;
        //}

        public static FFTBakedDataMultiRes BakeMultiRes(ShapeFFT fftWaves, int firstLod, int lodCount, int resolutionTime, float wavePatchSize,
            float loopPeriod)
        {
            // Need min scale, maybe max too - unlikely to need 16 orders of magnitude

            // Need to decide how many time samples to take. As first step can just divide
            // loopPeriod evenly like before. Probably always taking eg 16 samples per period
            // works well. So we can take 16 slices, and in the future we know that the period
            // of a bunch of the lods was much smaller, so we could take much denser samples.

            var buf = new CommandBuffer();

            var waveCombineShader = Resources.Load<ComputeShader>("FFT/FFTBake");
            var kernel = waveCombineShader.FindKernel("FFTBakeMultiRes");

            var bakedWaves = new RenderTexture(fftWaves._resolution, fftWaves._resolution * lodCount, 1, RenderTextureFormat.RFloat, 0);
            bakedWaves.enableRandomWrite = true;
            bakedWaves.Create();

            var stagingTexture = new Texture2D(fftWaves._resolution, fftWaves._resolution * lodCount, TextureFormat.RHalf, false, true);

            var frameCount = (int)(resolutionTime * loopPeriod);
            var frames = new half[frameCount][];

            const string folderName = "BakedWave";

#if CREST_DEBUG_DUMP_EXRS
            if (Directory.Exists(folderName))
            {
                Directory.Delete(folderName, true);
            }
            Directory.CreateDirectory(folderName);
#endif

            for (int timeIndex = 0; timeIndex < frameCount; timeIndex++) // this means resolutionTime is actually FPS
            {
                float t = timeIndex / (float)resolutionTime;

                buf.Clear();

                // Generate multi-res FFT into a texture array
                var fftWaveDataTA = FFTCompute.GenerateDisplacements(buf, fftWaves._resolution, loopPeriod,
                    fftWaves._windTurbulence, fftWaves.WindDirRadForFFT, fftWaves.WindSpeedForFFT, t,
                    fftWaves._spectrum, true);

                // Compute shader generates the final waves
                buf.SetComputeFloatParam(waveCombineShader, "_BakeTime", t);
                buf.SetComputeIntParam(waveCombineShader, "_MinSlice", firstLod);
                buf.SetComputeTextureParam(waveCombineShader, kernel, "_InFFTWaves", fftWaveDataTA);
                buf.SetComputeTextureParam(waveCombineShader, kernel, "_OutHeights", bakedWaves);
                buf.DispatchCompute(waveCombineShader, kernel, bakedWaves.width / 8, bakedWaves.height / 8, 1);

                Graphics.ExecuteCommandBuffer(buf);

                // Readback data to CPU
                RenderTexture.active = bakedWaves;
                stagingTexture.ReadPixels(new Rect(0, 0, bakedWaves.width, bakedWaves.height), 0, 0);

#if CREST_DEBUG_DUMP_EXRS
                var encodedTexture = stagingTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                File.WriteAllBytes($"{folderName}/test_{timeIndex}.exr", encodedTexture);
#endif

                frames[timeIndex] = stagingTexture.GetRawTextureData<half>().ToArray();
            }

            var framesFlattened = frames.SelectMany(x => x).ToArray();
            var framesFileName = "frames";

            SaveFramesToFile(framesFileName, framesFlattened);

            var bakedDataSO = ScriptableObject.CreateInstance<FFTBakedDataMultiRes>();
            var framesAsFloats = framesFlattened.Select(x => (float)x);
            bakedDataSO.Initialize(
                loopPeriod,
                fftWaves._resolution,
                firstLod,
                lodCount,
                wavePatchSize,
                frames.Length,
                new half(framesAsFloats.Min()),
                new half(framesAsFloats.Max()),
                framesFileName);

            SaveBakedDataAsset(bakedDataSO, folderName);

            return bakedDataSO;
        }

        private static void SaveFramesToFile(string framesFileName, half[] framesFlattened)
        {
            #if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            // TODO: decide whether the path should be Assets/Resources or Assets/Crest/Resources
            using (BinaryWriter writer = new BinaryWriter(File.Open($"Assets/Resources/{framesFileName}.bytes", FileMode.Create)))
            {
                foreach (var frame in framesFlattened)
                {
                    writer.Write(frame.value);
                }
            }
            #endif
        }

        private static void SaveBakedDataAsset(ScriptableObject bakedDataSO, string folderName)
        {
#if UNITY_EDITOR
            var bakedDataDirectory = $"Assets/{folderName}";
            if (!AssetDatabase.IsValidFolder(bakedDataDirectory))
            {
                AssetDatabase.CreateFolder("Assets", folderName);
            }
            AssetDatabase.CreateAsset(bakedDataSO, $"{bakedDataDirectory}/bakedTest.asset");
#endif
        }
    }
}
