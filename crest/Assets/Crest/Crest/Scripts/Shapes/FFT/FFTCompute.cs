// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Inspired by https://github.com/speps/GX-EncinoWaves

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Crest
{
    /// <summary>
    /// Runs FFT to generate water surface displacements
    /// </summary>
    public class FFTCompute
    {
        // Must match 'SIZE' param of first kernel in FFTCompute.compute
        const int FFT_KERNEL_0_RESOLUTION = 8;

        // Must match CASCADE_COUNT in FFTCompute.compute
        public const int CASCADE_COUNT = 16;

        bool _isInitialised = false;

        RenderTexture _spectrumInit;
        RenderTexture _spectrumHeight;
        RenderTexture _spectrumDisplaceX;
        RenderTexture _spectrumDisplaceZ;
        RenderTexture _tempFFT1;
        RenderTexture _tempFFT2;
        RenderTexture _tempFFT3;

        Texture2D _texButterfly;

        /// <summary>
        /// Generated 'raw', uncombined, wave data. Input for putting into AnimWaves data before combine pass.
        /// </summary>
        RenderTexture _waveBuffers;

        Texture2D _texSpectrumControls;
        bool _spectrumInitialised = false;
        Color[] _spectrumDataScratch = new Color[OceanWaveSpectrum.NUM_OCTAVES];

        ComputeShader _shaderSpectrum;
        ComputeShader _shaderFFT;

        int _kernelSpectrumInit;
        int _kernelSpectrumUpdate;

        // Generation data
        int _resolution;
        float _loopPeriod;
        float _windSpeed;
        float _windTurbulence;
        float _windDirRad;
        OceanWaveSpectrum _spectrum;

        float _generationTime = -1f;

        public static class ShaderIDs
        {
            public static readonly int s_Size = Shader.PropertyToID("_Size");
            public static readonly int s_WindSpeed = Shader.PropertyToID("_WindSpeed");
            public static readonly int s_Turbulence = Shader.PropertyToID("_Turbulence");
            public static readonly int s_Gravity = Shader.PropertyToID("_Gravity");
            public static readonly int s_Period = Shader.PropertyToID("_Period");
            public static readonly int s_WindDir = Shader.PropertyToID("_WindDir");
            public static readonly int s_SpectrumControls = Shader.PropertyToID("_SpectrumControls");
            public static readonly int s_ResultInit = Shader.PropertyToID("_ResultInit");
            public static readonly int s_Time = Shader.PropertyToID("_Time");
            public static readonly int s_Chop = Shader.PropertyToID("_Chop");
            public static readonly int s_Init0 = Shader.PropertyToID("_Init0");
            public static readonly int s_ResultHeight = Shader.PropertyToID("_ResultHeight");
            public static readonly int s_ResultDisplaceX = Shader.PropertyToID("_ResultDisplaceX");
            public static readonly int s_ResultDisplaceZ = Shader.PropertyToID("_ResultDisplaceZ");
            public static readonly int s_InputH = Shader.PropertyToID("_InputH");
            public static readonly int s_InputX = Shader.PropertyToID("_InputX");
            public static readonly int s_InputZ = Shader.PropertyToID("_InputZ");
            public static readonly int s_InputButterfly = Shader.PropertyToID("_InputButterfly");
            public static readonly int s_Output1 = Shader.PropertyToID("_Output1");
            public static readonly int s_Output2 = Shader.PropertyToID("_Output2");
            public static readonly int s_Output3 = Shader.PropertyToID("_Output3");
            public static readonly int s_Output = Shader.PropertyToID("_Output");
        }

        public FFTCompute(int resolution, float loopPeriod, float windSpeed, float windTurbulence, float windDirRad, OceanWaveSpectrum spectrum)
        {
            Debug.Assert(Mathf.NextPowerOfTwo(resolution) == resolution, "Crest: FFTCompute resolution must be power of 2");

            _resolution = resolution;
            _loopPeriod = loopPeriod;
            _windSpeed = windSpeed;
            _windTurbulence = windTurbulence;
            _windDirRad = windDirRad;
            _spectrum = spectrum;
        }

        public void Release()
        {
            if (_texButterfly != null) Helpers.Destroy(_texButterfly);
            if (_texSpectrumControls != null) Helpers.Destroy(_texSpectrumControls);
            if (_spectrumInit != null) _spectrumInit.Release();
            if (_spectrumHeight != null) _spectrumHeight.Release();
            if (_spectrumDisplaceX != null) _spectrumDisplaceX.Release();
            if (_spectrumDisplaceZ != null) _spectrumDisplaceZ.Release();
            if (_tempFFT1 != null) _tempFFT1.Release();
            if (_tempFFT2 != null) _tempFFT2.Release();
            if (_tempFFT3 != null) _tempFFT3.Release();

            if (_waveBuffers != null)
            {
                Helpers.Destroy(_waveBuffers);
                _waveBuffers = null;
            }

            _isInitialised = false;
        }

        void InitializeTextures()
        {
            Release();

            _shaderSpectrum = Resources.Load<ComputeShader>("FFT/FFTSpectrum");
            _kernelSpectrumInit = _shaderSpectrum.FindKernel("SpectrumInitalize");
            _kernelSpectrumUpdate = _shaderSpectrum.FindKernel("SpectrumUpdate");
            _shaderFFT = Resources.Load<ComputeShader>("FFT/FFTCompute");

            _texButterfly = new Texture2D(_resolution, Mathf.RoundToInt(Mathf.Log(_resolution, 2)), TextureFormat.RGBAFloat, false, true);

            _texSpectrumControls = new Texture2D(Crest.OceanWaveSpectrum.NUM_OCTAVES, 1, TextureFormat.RFloat, false, true);

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            rtd.width = rtd.height = _resolution;
            rtd.dimension = TextureDimension.Tex2DArray;
            rtd.enableRandomWrite = true;
            rtd.depthBufferBits = 0;
            rtd.volumeDepth = CASCADE_COUNT;
            rtd.colorFormat = RenderTextureFormat.ARGBFloat;
            rtd.msaaSamples = 1;

            Helpers.SafeCreateRenderTexture(ref _spectrumInit, rtd);
            _spectrumInit.name = "CrestFFTSpectrumInit";
            _spectrumInit.Create();

            rtd.colorFormat = RenderTextureFormat.RGFloat;


            Helpers.SafeCreateRenderTexture(ref _spectrumHeight, rtd);
            _spectrumHeight.name = "CrestFFTSpectrumHeight";
            _spectrumHeight.Create();
            Helpers.SafeCreateRenderTexture(ref _spectrumDisplaceX, rtd);
            _spectrumDisplaceX.name = "CrestFFTSpectrumDisplaceX";
            _spectrumDisplaceX.Create();
            Helpers.SafeCreateRenderTexture(ref _spectrumDisplaceZ, rtd);
            _spectrumDisplaceZ.name = "CrestFFTSpectrumDisplaceZ";
            _spectrumDisplaceZ.Create();

            Helpers.SafeCreateRenderTexture(ref _tempFFT1, rtd);
            _tempFFT1.name = "CrestFFTOutput1";
            _tempFFT1.Create();
            Helpers.SafeCreateRenderTexture(ref _tempFFT2, rtd);
            _tempFFT2.name = "CrestFFTOutput2";
            _tempFFT2.Create();
            Helpers.SafeCreateRenderTexture(ref _tempFFT3, rtd);
            _tempFFT3.name = "CrestFFTOutput3";
            _tempFFT3.Create();

            // Raw wave data buffer
            _waveBuffers = new RenderTexture(_resolution, _resolution, 0, GraphicsFormat.R16G16B16A16_SFloat);
            _waveBuffers.wrapMode = TextureWrapMode.Repeat;
            _waveBuffers.antiAliasing = 1;
            _waveBuffers.filterMode = FilterMode.Bilinear;
            _waveBuffers.anisoLevel = 0;
            _waveBuffers.useMipMap = false;
            _waveBuffers.name = "FFTCascades";
            _waveBuffers.dimension = TextureDimension.Tex2DArray;
            _waveBuffers.volumeDepth = CASCADE_COUNT;
            _waveBuffers.enableRandomWrite = true;
            _waveBuffers.Create();

            InitializeButterfly(_resolution);

            InitialiseSpectrumHandControls();

            _isInitialised = true;
        }

        void CleanUp()
        {
            // Destroy to clear references.
            Helpers.Destroy(_spectrumInit);
            Helpers.Destroy(_spectrumHeight);
            Helpers.Destroy(_spectrumDisplaceX);
            Helpers.Destroy(_spectrumDisplaceZ);
            Helpers.Destroy(_tempFFT1);
            Helpers.Destroy(_tempFFT2);
            Helpers.Destroy(_tempFFT3);
            _spectrumInitialised = false;
        }

        internal static void CleanUpAll()
        {
            foreach (var generator in _generators)
            {
                generator.Value.Release();
                generator.Value.CleanUp();
            }
        }

        static Dictionary<int, FFTCompute> _generators = new Dictionary<int, FFTCompute>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            _generators.Clear();
        }

        static int CalculateWaveConditionsHash(int resolution, float loopPeriod, float windTurbulence, float windDirRad, float windSpeed, OceanWaveSpectrum spectrum)
        {
            var conditionsHash = Hashy.CreateHash();
            Hashy.AddInt(resolution, ref conditionsHash);
            Hashy.AddFloat(loopPeriod, ref conditionsHash);
            Hashy.AddFloat(windSpeed, ref conditionsHash);
            Hashy.AddFloat(windTurbulence, ref conditionsHash);
            Hashy.AddFloat(windDirRad, ref conditionsHash);
            if (spectrum != null)
            {
                Hashy.AddObject(spectrum, ref conditionsHash);
            }
            return conditionsHash;
        }

        /// <summary>
        /// Computes water surface displacement, with wave components split across slices of the output texture array
        /// </summary>
        public static RenderTexture GenerateDisplacements(CommandBuffer buf, int resolution, float loopPeriod, float windTurbulence, float windDirRad, float windSpeed, float time, OceanWaveSpectrum spectrum, bool updateSpectrum)
        {
            // All static data arguments should be hashed here and passed to the generator constructor
            var conditionsHash = CalculateWaveConditionsHash(resolution, loopPeriod, windTurbulence, windDirRad, windSpeed, spectrum);
            if (!_generators.TryGetValue(conditionsHash, out var generator))
            {
                // No generator for these params - create one
                generator = new FFTCompute(resolution, loopPeriod, windSpeed, windTurbulence, windDirRad, spectrum);
                _generators.Add(conditionsHash, generator);
            }

            // The remaining dynamic data arguments should be passed in to the generation here
            return generator.GenerateDisplacementsInternal(buf, time, updateSpectrum);
        }

        RenderTexture GenerateDisplacementsInternal(CommandBuffer buf, float time, bool updateSpectrum)
        {
            // Check if already generated, and we're not being asked to re-update the spectrum
            if (_generationTime == time && !updateSpectrum)
            {
                return _waveBuffers;
            }

            if (!_isInitialised || _spectrumHeight == null)
            {
                InitializeTextures();
            }

            if (!_spectrumInitialised || updateSpectrum)
            {
                InitialiseSpectrumHandControls();
                InitializeSpectrum(buf);
                _spectrumInitialised = true;
            }

            UpdateSpectrum(buf, time);

            DispatchFFT(buf);

            _generationTime = time;

            return _waveBuffers;
        }

        /// <summary>
        /// Changing wave gen data can result in creating lots of new generators. This gives a way to notify
        /// that a parameter has changed. If there is no existing generator for the new param values, but there
        /// is one for the old param values, this old generator is repurposed.
        /// </summary>
        public static void OnGenerationDataUpdated(int resolution, float loopPeriod,
            float windTurbulenceOld, float windDirRadOld, float windSpeedOld, OceanWaveSpectrum spectrumOld,
            float windTurbulenceNew, float windDirRadNew, float windSpeedNew, OceanWaveSpectrum spectrumNew)
        {
            // If multiple wave components share one FFT, then one of them changes its settings, it will
            // actually steal the generator from the rest. Then the first from the rest which request the
            // old settings will trigger creation of a new generator, and the remaining ones will use this
            // new generator. In the end one new generator is created, but it's created for the old settings.
            // Generators are requested single threaded so there should not be a race condition. Odd pattern
            // but I don't think any other way works without ugly checks to see if old generators are still
            // used, or other complicated things.

            // Check if no generator exists for new values
            var newHash = CalculateWaveConditionsHash(resolution, loopPeriod, windTurbulenceNew, windDirRadNew, windSpeedNew, spectrumNew);
            if (!_generators.TryGetValue(newHash, out _))
            {
                // Try to adapt an existing generator rather than default to creating a new one
                var oldHash = CalculateWaveConditionsHash(resolution, loopPeriod, windTurbulenceOld, windDirRadOld, windSpeedOld, spectrumOld);
                if (_generators.TryGetValue(oldHash, out var generator))
                {
                    // Hash will change for this generator, so remove the current one
                    _generators.Remove(oldHash);

                    // Update params
                    generator._windTurbulence = windTurbulenceNew;
                    generator._windDirRad = windDirRadNew;
                    generator._windSpeed = windSpeedNew;
                    generator._spectrum = spectrumNew;

                    // Trigger generator to re-init the spectrum
                    generator._spectrumInitialised = false;

                    // Re-add with new hash
                    _generators.Add(newHash, generator);
                }
            }
            else
            {
                // There is already a new generator which will be used. Remove the previous one - if it really is needed
                // then it will be created later.
                var oldHash = CalculateWaveConditionsHash(resolution, loopPeriod, windTurbulenceOld, windDirRadOld, windSpeedOld, spectrumOld);
                _generators.Remove(oldHash);
            }
        }

        /// <summary>
        /// Number of FFT generators
        /// </summary>
        public static int GeneratorCount => _generators != null ? _generators.Count : 0;

        /// <summary>
        /// Computes the offsets used for the FFT calculation
        /// </summary>
        void InitializeButterfly(int resolution)
        {
            var log2Size = Mathf.RoundToInt(Mathf.Log(resolution, 2));
            var butterflyColors = new Color[resolution * log2Size];

            int offset = 1, numIterations = resolution >> 1;
            for (int rowIndex = 0; rowIndex < log2Size; rowIndex++)
            {
                int rowOffset = rowIndex * resolution;
                {
                    int start = 0, end = 2 * offset;
                    for (int iteration = 0; iteration < numIterations; iteration++)
                    {
                        var bigK = 0f;
                        for (int K = start; K < end; K += 2)
                        {
                            var phase = 2.0f * Mathf.PI * bigK * numIterations / resolution;
                            var cos = Mathf.Cos(phase);
                            var sin = Mathf.Sin(phase);
                            butterflyColors[rowOffset + K / 2] = new Color(cos, -sin, 0, 1);
                            butterflyColors[rowOffset + K / 2 + offset] = new Color(-cos, sin, 0, 1);

                            bigK += 1f;
                        }
                        start += 4 * offset;
                        end = start + 2 * offset;
                    }
                }
                numIterations >>= 1;
                offset <<= 1;
            }

            _texButterfly.SetPixels(butterflyColors);
            _texButterfly.Apply();
        }

        void InitialiseSpectrumHandControls()
        {
            for (var i = 0; i < OceanWaveSpectrum.NUM_OCTAVES; i++)
            {
                float pow = _spectrum._powerDisabled[i] ? 0f : Mathf.Pow(10f, _spectrum._powerLog[i]);
                pow *= _spectrum._multiplier * _spectrum._multiplier;
                _spectrumDataScratch[i] = pow * Color.white;
            }

            _texSpectrumControls.SetPixels(_spectrumDataScratch);
            _texSpectrumControls.Apply();
        }

        /// <summary>
        /// Computes base spectrum values based on wind speed & turbulence & spectrum controls
        /// </summary>
        void InitializeSpectrum(CommandBuffer buf)
        {
            buf.SetComputeIntParam(_shaderSpectrum, ShaderIDs.s_Size, _resolution);
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_WindSpeed, _windSpeed);
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_Turbulence, _windTurbulence);
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_Gravity, Mathf.Abs(Physics.gravity.magnitude));
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_Period, _loopPeriod);
            buf.SetComputeVectorParam(_shaderSpectrum, ShaderIDs.s_WindDir, new Vector2(Mathf.Cos(_windDirRad), Mathf.Sin(_windDirRad)));
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumInit, ShaderIDs.s_SpectrumControls, _texSpectrumControls);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumInit, ShaderIDs.s_ResultInit, _spectrumInit);
            buf.DispatchCompute(_shaderSpectrum, _kernelSpectrumInit, _resolution / 8, _resolution / 8, CASCADE_COUNT);
        }

        /// <summary>
        /// Computes a spectrum for the current time which can be FFT'd into the final surface
        /// </summary>
        void UpdateSpectrum(CommandBuffer buf, float time)
        {
            // Always set _Size as the compute shader returned from Resource.Load is the same asset every time and more
            // than one ShapeFFT will overwrite this value.
            buf.SetComputeIntParam(_shaderSpectrum, ShaderIDs.s_Size, _resolution);
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_Time, time * _spectrum._gravityScale);
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_Chop, _spectrum._chop);
            buf.SetComputeFloatParam(_shaderSpectrum, ShaderIDs.s_Period, _loopPeriod);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, ShaderIDs.s_Init0, _spectrumInit);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, ShaderIDs.s_ResultHeight, _spectrumHeight);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, ShaderIDs.s_ResultDisplaceX, _spectrumDisplaceX);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, ShaderIDs.s_ResultDisplaceZ, _spectrumDisplaceZ);
            buf.DispatchCompute(_shaderSpectrum, _kernelSpectrumUpdate, _resolution / 8, _resolution / 8, CASCADE_COUNT);
        }

        /// <summary>
        /// FFT the spectrum into surface displacements
        /// </summary>
        void DispatchFFT(CommandBuffer buf)
        {
            var kernelOffset = 2 * Mathf.RoundToInt(Mathf.Log(_resolution / FFT_KERNEL_0_RESOLUTION, 2f));

            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_InputH, _spectrumHeight);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_InputX, _spectrumDisplaceX);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_InputZ, _spectrumDisplaceZ);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_InputButterfly, _texButterfly);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_Output1, _tempFFT1);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_Output2, _tempFFT2);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, ShaderIDs.s_Output3, _tempFFT3);
            buf.DispatchCompute(_shaderFFT, kernelOffset, 1, _resolution, CASCADE_COUNT);

            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, ShaderIDs.s_InputH, _tempFFT1);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, ShaderIDs.s_InputX, _tempFFT2);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, ShaderIDs.s_InputZ, _tempFFT3);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, ShaderIDs.s_InputButterfly, _texButterfly);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, ShaderIDs.s_Output, _waveBuffers);
            buf.DispatchCompute(_shaderFFT, kernelOffset + 1, _resolution, 1, CASCADE_COUNT);
        }

        public static void OnGUI(int resolution, float loopPeriod, float windTurbulence, float windDirRad, float windSpeed, OceanWaveSpectrum spectrum)
        {
            _generators.TryGetValue(CalculateWaveConditionsHash(resolution, loopPeriod, windTurbulence, windDirRad, windSpeed, spectrum), out var generator);
            generator?.OnGUIInternal();
        }

        void OnGUIInternal()
        {
            if (_waveBuffers != null && _waveBuffers.IsCreated())
            {
                OceanDebugGUI.DrawTextureArray(_waveBuffers, 8, 0.5f, 20f);
            }

            if (_texSpectrumControls != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, 100f, 10f), _texSpectrumControls);
            }
        }
    }
}
