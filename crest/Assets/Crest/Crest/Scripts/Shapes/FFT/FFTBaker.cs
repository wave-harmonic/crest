// Crest Ocean System

// Copyright 2021 Wave Harmonic Ltd

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

//#define CREST_DEBUG_DUMP_EXRS

using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public static class FFTBaker
    {
        /// <summary>
        /// Runs FFT for a bunch of time steps and saves all the resulting data to a scriptable object
        /// </summary>
        public static FFTBakedData Bake(ShapeFFT fftWaves, int firstLod, int lodCount, int resolutionTime, float loopPeriod)
        {
            // Need min scale, maybe max too - unlikely to need 16 orders of magnitude

            // Need to decide how many time samples to take. As first step can just divide
            // loopPeriod evenly like before. Probably always taking eg 16 samples per period
            // works well. So we can take 16 slices, and in the future we know that the period
            // of a bunch of the lods was much smaller, so we could take much denser samples.

            var buf = new CommandBuffer();

            var waveCombineShader = Resources.Load<ComputeShader>("FFT/FFTBake");
            var kernel = waveCombineShader.FindKernel("FFTBakeMultiRes");

            var bakedWaves = new RenderTexture(fftWaves._resolution, fftWaves._resolution * lodCount, 1, RenderTextureFormat.ARGBFloat, 0);
            bakedWaves.enableRandomWrite = true;
            bakedWaves.Create();

            var stagingTexture = new Texture2D(fftWaves._resolution, fftWaves._resolution * lodCount, TextureFormat.RGBAHalf, false, true);

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
                buf.SetComputeTextureParam(waveCombineShader, kernel, "_OutDisplacements", bakedWaves);
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
            //Debug.Log($"Width: {fftWaves._resolution}, frame count: {frameCount}, slices: {lodCount}, floats per frame: {frames[0].Length}, total floats: {framesFlattened.Length}");

            var bakedDataSO = ScriptableObject.CreateInstance<FFTBakedData>();
            var framesAsFloats = framesFlattened.Select(x => (float)x);
            bakedDataSO.Initialize(
                loopPeriod,
                fftWaves._resolution,
                firstLod,
                lodCount,
                frames.Length,
                new half(framesAsFloats.Min()),
                new half(framesAsFloats.Max()),
                framesFlattened);

            SaveBakedDataAsset(bakedDataSO, folderName);

            return bakedDataSO;
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
