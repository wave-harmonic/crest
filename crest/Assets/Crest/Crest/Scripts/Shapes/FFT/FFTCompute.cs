// Crest Ocean System

// Copyright 2021 Wave Harmonic Ltd

// Inspired by https://github.com/speps/GX-EncinoWaves

using UnityEngine;
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
        const int CASCADE_COUNT = 16;

        bool _isInitialised = false;

        RenderTexture _spectrumInit;
        RenderTexture _spectrumHeight;
        RenderTexture _spectrumDisplaceX;
        RenderTexture _spectrumDisplaceZ;
        RenderTexture _tempFFT1;
        RenderTexture _tempFFT2;
        RenderTexture _tempFFT3;

        Texture2D _texButterfly;

        Texture2D _texSpectrumControls;
        bool _spectrumInitialised = false;
        Color[] _spectrumDataScratch = new Color[OceanWaveSpectrum.NUM_OCTAVES];

        ComputeShader _shaderSpectrum;
        ComputeShader _shaderFFT;

        float _prevWindTurbulence;
        float _prevWindSpeed;
        int _prevResolution;

        int _kernelSpectrumInit;
        int _kernelSpectrumUpdate;

        public void Release()
        {
            if (_texButterfly != null) Object.DestroyImmediate(_texButterfly);
            if (_texSpectrumControls != null) Object.DestroyImmediate(_texSpectrumControls);
            if (_spectrumInit != null) _spectrumInit.Release();
            if (_spectrumHeight != null) _spectrumHeight.Release();
            if (_spectrumDisplaceX != null) _spectrumDisplaceX.Release();
            if (_spectrumDisplaceZ != null) _spectrumDisplaceZ.Release();
            if (_tempFFT1 != null) _tempFFT1.Release();
            if (_tempFFT2 != null) _tempFFT2.Release();
            if (_tempFFT3 != null) _tempFFT3.Release();

            _prevWindTurbulence = -1;
            _prevWindSpeed = -1;
            _prevResolution = -1;
            _isInitialised = false;
        }

        void InitializeTextures(int resolution, OceanWaveSpectrum spectrum)
        {
            Release();

            _shaderSpectrum = Resources.Load<ComputeShader>("FFT/FFTSpectrum");
            _kernelSpectrumInit = _shaderSpectrum.FindKernel("SpectrumInitalize");
            _kernelSpectrumUpdate = _shaderSpectrum.FindKernel("SpectrumUpdate");
            _shaderFFT = Resources.Load<ComputeShader>("FFT/FFTCompute");

            _texButterfly = new Texture2D(resolution, Mathf.RoundToInt(Mathf.Log(resolution, 2)), TextureFormat.RGBAFloat, false, true);

            _texSpectrumControls = new Texture2D(Crest.OceanWaveSpectrum.NUM_OCTAVES, 1, TextureFormat.RFloat, false, true);

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            rtd.width = rtd.height = resolution;
            rtd.dimension = TextureDimension.Tex2DArray;
            rtd.enableRandomWrite = true;
            rtd.depthBufferBits = 0;
            rtd.volumeDepth = CASCADE_COUNT;
            rtd.colorFormat = RenderTextureFormat.ARGBFloat;
            rtd.msaaSamples = 1;

            _spectrumInit = new RenderTexture(rtd);
            _spectrumInit.Create();

            rtd.colorFormat = RenderTextureFormat.RGFloat;

            _spectrumHeight = new RenderTexture(rtd);
            _spectrumHeight.Create();
            _spectrumDisplaceX = new RenderTexture(rtd);
            _spectrumDisplaceX.Create();
            _spectrumDisplaceZ = new RenderTexture(rtd);
            _spectrumDisplaceZ.Create();

            _tempFFT1 = new RenderTexture(rtd);
            _tempFFT1.Create();
            _tempFFT2 = new RenderTexture(rtd);
            _tempFFT2.Create();
            _tempFFT3 = new RenderTexture(rtd);
            _tempFFT3.Create();

            InitializeButterfly(resolution);

            InitialiseSpectrumHandControls(spectrum);

            _isInitialised = true;
        }

        /// <summary>
        /// Computes water surface displacement, with wave components split across slices of the output texture array
        /// </summary>
        public void GenerateDisplacements(CommandBuffer buf, float windTurbulence, float windSpeed, float time, OceanWaveSpectrum spectrum, bool updateSpectrum, RenderTexture _outputTextureArray)
        {
            Debug.Assert(_outputTextureArray != null, "FFT: No output texture provided.");

            var resolution = _outputTextureArray.width;
            if (!_isInitialised || _spectrumHeight == null || _spectrumHeight.width != resolution)
            {
                InitializeTextures(resolution, spectrum);
            }

            if (!_spectrumInitialised || updateSpectrum)
            {
                InitialiseSpectrumHandControls(spectrum);
            }

            if (!Mathf.Approximately(_prevWindTurbulence, windTurbulence) ||
                !Mathf.Approximately(_prevWindSpeed, windSpeed) ||
                resolution != _prevResolution ||
                updateSpectrum
                )
            {
                _prevWindTurbulence = windTurbulence;
                _prevWindSpeed = windSpeed;
                _prevResolution = resolution;

                InitializeSpectrum(buf, resolution, windSpeed, windTurbulence, spectrum._gravityScale * Mathf.Abs(Physics.gravity.magnitude));
            }

            UpdateSpectrum(buf, resolution, time, spectrum._chop);

            DispatchFFT(buf, resolution, _outputTextureArray);
        }

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

        void InitialiseSpectrumHandControls(OceanWaveSpectrum spectrum)
        {
            for (var i = 0; i < OceanWaveSpectrum.NUM_OCTAVES; i++)
            {
                float pow = spectrum._powerDisabled[i] ? 0f : Mathf.Pow(10f, spectrum._powerLog[i]);
                pow *= spectrum._multiplier * spectrum._multiplier;
                _spectrumDataScratch[i] = pow * Color.white;
            }

            _texSpectrumControls.SetPixels(_spectrumDataScratch);
            _texSpectrumControls.Apply();

            _spectrumInitialised = true;
        }

        /// <summary>
        /// Computes base spectrum values based on wind speed & turbulence & spectrum controls
        /// </summary>
        void InitializeSpectrum(CommandBuffer buf, int size, float windSpeed, float windTurbulence, float gravity)
        {
            buf.SetComputeIntParam(_shaderSpectrum, "_Size", size);
            buf.SetComputeFloatParam(_shaderSpectrum, "_WindSpeed", windSpeed);
            buf.SetComputeFloatParam(_shaderSpectrum, "_Turbulence", windTurbulence);
            buf.SetComputeFloatParam(_shaderSpectrum, "_Gravity", gravity);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumInit, "_SpectrumControls", _texSpectrumControls);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumInit, "_ResultInit", _spectrumInit);
            buf.DispatchCompute(_shaderSpectrum, _kernelSpectrumInit, size / 8, size / 8, CASCADE_COUNT);
        }

        /// <summary>
        /// Computes a spectrum for the current time which can be FFT'd into the final surface
        /// </summary>
        void UpdateSpectrum(CommandBuffer buf, int size, float time, float chop)
        {
            buf.SetComputeFloatParam(_shaderSpectrum, "_Time", time);
            buf.SetComputeFloatParam(_shaderSpectrum, "_Chop", chop);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, "_Init0", _spectrumInit);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, "_ResultHeight", _spectrumHeight);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, "_ResultDisplaceX", _spectrumDisplaceX);
            buf.SetComputeTextureParam(_shaderSpectrum, _kernelSpectrumUpdate, "_ResultDisplaceZ", _spectrumDisplaceZ);
            buf.DispatchCompute(_shaderSpectrum, _kernelSpectrumUpdate, size / 8, size / 8, CASCADE_COUNT);
        }

        /// <summary>
        /// FFT the spectrum into surface displacements
        /// </summary>
        void DispatchFFT(CommandBuffer buf, int resolution, RenderTexture output)
        {
            var kernelOffset = 2 * Mathf.RoundToInt(Mathf.Log(resolution / FFT_KERNEL_0_RESOLUTION, 2f));

            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_InputH", _spectrumHeight);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_InputX", _spectrumDisplaceX);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_InputZ", _spectrumDisplaceZ);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_InputButterfly", _texButterfly);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_Output1", _tempFFT1);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_Output2", _tempFFT2);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset, "_Output3", _tempFFT3);
            buf.DispatchCompute(_shaderFFT, kernelOffset, 1, resolution, CASCADE_COUNT);

            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, "_InputH", _tempFFT1);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, "_InputX", _tempFFT2);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, "_InputZ", _tempFFT3);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, "_InputButterfly", _texButterfly);
            buf.SetComputeTextureParam(_shaderFFT, kernelOffset + 1, "_Output", output);
            buf.DispatchCompute(_shaderFFT, kernelOffset + 1, resolution, 1, CASCADE_COUNT);
        }

        internal void OnGUI()
        {
            GUI.DrawTexture(new Rect(0f, 0f, 100f, 10f), _texSpectrumControls);
        }
    }
}
