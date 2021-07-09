// Crest Ocean System

// Copyright 2021 Wave Harmonic Ltd

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class FFTBaker
    {
        public static FFTBakedData Bake(ShapeFFT fftWaves, int resolutionSpace, int resolutionTime, float wavePatchSize,
            float loopPeriod)
        {
            Debug.Assert(Mathf.IsPowerOfTwo(resolutionSpace), "Crest: Spatial resolution must be power of 2");
            // Debug.Assert(Mathf.IsPowerOfTwo(resolutionTime), "Crest: Temporal resolution must be power of 2"); // seems unnecessary
            Debug.Assert(Mathf.IsPowerOfTwo((int) wavePatchSize), "Crest: Spatial path size must be power of 2");

            // create the staging texture wavePatchData that is written to then downloaded from the GPU
            var desc = new RenderTextureDescriptor
            {
                width = resolutionSpace,
                height = resolutionSpace,
                volumeDepth = 1,
                autoGenerateMips = false,
                colorFormat = RenderTextureFormat.ARGBFloat,
                enableRandomWrite = true,
                dimension = TextureDimension.Tex2D,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
            };
            var wavePatchData = new RenderTexture(desc);
            wavePatchData.Create();

            var stagingTexture = new Texture2D(desc.width, desc.height, TextureFormat.RFloat, false, true);
            var buf = new CommandBuffer();

            var waveCombineShader = Resources.Load<ComputeShader>("FFT/FFTBake");
            var kernel = waveCombineShader.FindKernel("FFTBake");

            var frameCount = (int) (resolutionTime * loopPeriod);
            var frames = new float[frameCount][];

            for (int timeIndex = 0; timeIndex < frameCount; timeIndex++) // this means resolutionTime is actually FPS
            {
                float t = timeIndex / (float) resolutionTime;

                buf.Clear();

                // Generate multi-res FFT into a texture array
                var fftWaveDataTA = FFTCompute.GenerateDisplacements(buf, fftWaves._resolution, loopPeriod,
                    fftWaves._windTurbulence, fftWaves.WindDirRadForFFT, fftWaves.WindSpeedForFFT, t,
                    fftWaves._spectrum, true);

                // Compute shader generates the final waves
                buf.SetComputeFloatParam(waveCombineShader, "_BakeTime", t);
                buf.SetComputeFloatParam(waveCombineShader, "_WavePatchSize", wavePatchSize);
                buf.SetComputeTextureParam(waveCombineShader, kernel, "_InFFTWaves", fftWaveDataTA);
                buf.SetComputeTextureParam(waveCombineShader, kernel, "_OutHeights", wavePatchData);
                buf.DispatchCompute(waveCombineShader, kernel, resolutionSpace / 8, resolutionSpace / 8, 1);
                Graphics.ExecuteCommandBuffer(buf);

                // Readback data to CPU
                // what was the trick to doing this again? copy the render texture to a normal texture then read it back? urgh
                //var data = wavePatchData.GetPixels();
                RenderTexture.active = wavePatchData;
                stagingTexture.ReadPixels(new Rect(0, 0, wavePatchData.width, wavePatchData.height), 0,
                    0); // is this correct??

                frames[timeIndex] = stagingTexture.GetRawTextureData<float>().ToArray();
            }

            var framesFlattened = frames.SelectMany(x => x).ToArray();
            var framesFileName = "frames";
            const string folderName = "BakedWave";
            
            SaveFramesToFile(framesFileName, framesFlattened);
            
            var bakedDataSO = ScriptableObject.CreateInstance<FFTBakedData>();
            bakedDataSO.Initialize(
                loopPeriod,
                resolutionSpace,
                wavePatchSize,
                frames.Length,
                framesFlattened.Min(),
                framesFlattened.Max(),
                framesFileName);

            SaveBakedDataAsset(bakedDataSO, folderName);

            return bakedDataSO;
        }

        private static void SaveFramesToFile(string framesFileName, float[] framesFlattened)
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
                    writer.Write(frame);
                }
            }
            #endif
        }

        private static void SaveBakedDataAsset(FFTBakedData bakedDataSO, string folderName)
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