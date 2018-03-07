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

        public Vector3 GetPositionDisplacedToPositionExpensive(ref Vector3 displacedWorldPos, float toff)
        {
            // fpi - guess should converge to location that displaces to the target position
            Vector3 guess = displacedWorldPos;
            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            for( int i = 0; i < 4; i++)
            {
                Vector3 error = guess + GetDisplacement(ref guess, toff) - displacedWorldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }

            guess.y = OceanRenderer.Instance.SeaLevel;

            return guess;
        }

        public Vector3 GetDisplacement(ref Vector3 worldPos, float toff)
        {
            if (_amplitudes == null) return Vector3.zero;

            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            float chop = OceanRenderer.Instance._chop;
            float mytime = OceanRenderer.Instance.ElapsedTime + toff;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;

            Vector3 result = Vector3.zero;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                Vector2 D = new Vector2(Mathf.Cos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad), Mathf.Sin((windAngle + _angleDegs[j]) * Mathf.Deg2Rad));
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

        // compute normal to a surface with a parameterization - equation 14 here: http://mathworld.wolfram.com/NormalVector.html
        public Vector3 GetNormal(ref Vector3 worldPos, float toff)
        {
            if (_amplitudes == null) return Vector3.zero;

            var pos = new Vector2(worldPos.x, worldPos.z);
            float chop = OceanRenderer.Instance._chop;
            float mytime = OceanRenderer.Instance.ElapsedTime + toff;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;

            // base rate of change of our displacement function in x and z is unit
            var delfdelx = Vector3.right;
            var delfdelz = Vector3.forward;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                var D = new Vector2(Mathf.Cos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad), Mathf.Sin((windAngle + _angleDegs[j]) * Mathf.Deg2Rad));
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float disp = k * -chop * Mathf.Cos(t);
                float dispx = D.x * disp;
                float dispz = D.y * disp;
                float dispy = -k * Mathf.Sin(t);

                delfdelx += _amplitudes[j] * new Vector3(D.x * dispx, D.x * dispy, D.y * dispx);
                delfdelz += _amplitudes[j] * new Vector3(D.x * dispz, D.y * dispy, D.y * dispz);
            }

            return Vector3.Cross(delfdelz, delfdelx).normalized;
        }

        public float GetHeightExpensive(ref Vector3 worldPos, float toff)
        {
            Vector3 posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            Vector3 undisplacedPos = GetPositionDisplacedToPositionExpensive(ref posFlatland, toff);

            return posFlatland.y + GetDisplacement(ref undisplacedPos, toff).y;
        }

        public Vector3 GetSurfaceVelocity(ref Vector3 worldPos, float toff)
        {
            if (_amplitudes == null) return Vector3.zero;

            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            float chop = OceanRenderer.Instance._chop;
            float mytime = OceanRenderer.Instance.ElapsedTime + toff;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;

            Vector3 result = Vector3.zero;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                Vector2 D = new Vector2(Mathf.Cos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad), Mathf.Sin((windAngle + _angleDegs[j]) * Mathf.Deg2Rad));
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float disp = -chop * k * C * Mathf.Cos(t);
                result += _amplitudes[j] * new Vector3(
                    D.x * disp,
                    -k * C * Mathf.Sin(t),
                    D.y * disp
                    );
            }

            return result;
        }
    }
}
