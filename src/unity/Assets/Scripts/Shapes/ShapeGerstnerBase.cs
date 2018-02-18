// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Base class for Gerstner wave shapes
    /// </summary>
    public class ShapeGerstnerBase : MonoBehaviour
    {
        [Tooltip("Geometry to rasterize into wave buffers to generate waves.")]
        public Mesh _rasterMesh;
        [Tooltip("Shader to be used to render out a single Gerstner octave.")]
        public Shader _waveShader;

        public int _randomSeed = 0;

        protected WaveSpectrum _spectrum;

        // data for all components
        protected float[] _wavelengths;
        protected float[] _amplitudes;
        protected float[] _angleDegs;
        protected float[] _phases;

        protected virtual void Start()
        {
            _spectrum = GetComponent<WaveSpectrum>();
        }

        protected virtual void Update()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWavelengths(ref _wavelengths, ref _angleDegs, ref _phases);

            Random.state = randomStateBkp;

            UpdateAmplitudes();
        }

        void UpdateAmplitudes()
        {
            if (_amplitudes == null || _amplitudes.Length != _wavelengths.Length)
            {
                _amplitudes = new float[_wavelengths.Length];
            }

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                _amplitudes[i] = _spectrum.GetAmplitude(_wavelengths[i]);
            }
        }

        float ComputeWaveSpeed(float wavelength/*, float depth*/)
        {
            // wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
            // https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
            float g = 9.81f;
            float k = 2f * Mathf.PI / wavelength;
            //float h = max(depth, 0.01);
            //float cp = sqrt(abs(tanh_clamped(h * k)) * g / k);
            float cp = Mathf.Sqrt(g / k);
            return cp;
        }

        public Vector3 GetDisplacement(Vector3 worldPos, float toff)
        {
            if (_amplitudes == null) return Vector3.zero;

            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            float chop = OceanRenderer.Instance._chop;
            float mytime = OceanRenderer.Instance.ElapsedTime + toff;

            Vector3 result = Vector3.zero;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                Vector2 D = new Vector2(Mathf.Cos(_angleDegs[j] * Mathf.Deg2Rad), Mathf.Sin(_angleDegs[j] * Mathf.Deg2Rad));
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float disp = -chop * Mathf.Sin(t);
                result += _amplitudes[j] * new Vector3(
                    D.x * disp,
                    Mathf.Cos(t),
                    D.y * disp
                    );
            }

            return result;
        }
    }
}
