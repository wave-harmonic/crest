// Crest Ocean System

// Copyright 2021 Wave Harmonic Ltd

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class FFTBaker
    {
        public static bool Bake(ShapeFFT fftWaves, int resolutionSpace, int resolutionTime, float period, float wavePatchWidth)
        {
            // TODO: assert wavePatchWidth is a power of 2
            // TODO: probably assert period and resolutions are powers of 2 as well. would not hurt..

            var desc = new RenderTextureDescriptor
            {
                width = resolutionSpace,
                height = resolutionSpace,
                volumeDepth = 1,
                autoGenerateMips = false,
                colorFormat = RenderTextureFormat.ARGBFloat,
                enableRandomWrite = true,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                depthBufferBits = 0,
                sRGB = false
            };

            // width of wave patch
            var wavePatchData = new RenderTexture(desc);
            var buf = new CommandBuffer();

            // TODO
            var waveCombineShader = Resources.Load("some compute shader (see below)");

            for (int timeIndex = 0; timeIndex < resolutionTime; timeIndex++)
            {
                float t = period * timeIndex / (float)resolutionTime;
                buf.Clear();
                var waveData = FFTCompute.GenerateDisplacements(buf, fftWaves._resolution, fftWaves._windTurbulence, fftWaves.WindDirRadForFFT, fftWaves.WindSpeedForFFT, t, fftWaves._spectrum, true);

                // Create a compute shader that generates the final waves:
                // - takes the waveData texture as input.
                // - outputs to the stage texture
                // - for each texel:
                //    - compute world position using UV and wavePatchWidth
                //    - initialise output to 0
                //    - loop over each slice in the wave data, compute slice UV. Similar to computation in AnimWavesGerstner.shader.
                //    - sample displacements from the slice and add them to the result. using Wrap sampling.
                //       - could convert to heightfield here, and only store a single value
                //       - some slices will be too high res. could omit, or maybe it doesnt matter.
                //buf.SetComputeTextureParam(waveCombineShader, "input", waveData);
                //buf.SetComputeTextureParam(waveCombineShader, "output", wavePatchData);
                // ...
                //buf.DispatchCompute();

                Graphics.ExecuteCommandBuffer(buf);

                // readback data to CPU
                // what was the trick to doing this again? copy the render texture to a normal texture then read it back? urgh
                //var data = wavePatchData.GetPixels();

                // File away the data somewhere. Perhaps this? FFTCollisionProvider would have the runtime code.
                //var fftCollProvider = OceanRenderer.Instance.CollisionProvider as FFTCollisionProvider;
                //fftCollProvider.SaveData(t, data);
            }

            return true;
        }
    }
}
