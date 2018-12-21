// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Ocean shape representation - power values for each octave of wave components.
    /// </summary>
    [CreateAssetMenu(fileName = "OceanWaves", menuName = "Crest/Ocean Wave Spectrum", order = 10000)]
    public class OceanWaveSpectrum : ScriptableObject
    {
        public const int NUM_OCTAVES = 12;
        public static readonly float SMALLEST_WL_POW_2 = -2f;

        public static readonly float MIN_POWER_LOG = -6f;
        public static readonly float MAX_POWER_LOG = 3f;

        [Tooltip("Variance of flow direction, in degrees"), Range(0f, 180f)]
        public float _waveDirectionVariance = 90f;

        [Tooltip("More gravity means faster waves."), Range(0f, 25f)]
        public float _gravityScale = 1f;

        [SerializeField, HideInInspector]
        float[] _powerLog = new float[NUM_OCTAVES]
            { -6f, -4.0088496f, -3.4452133f, -2.6996124f, -2.615044f, -1.2080691f, -0.53905386f, 0.27448857f, 0.53627354f, 1.0282621f, 1.4403292f, -6f };

        [SerializeField, HideInInspector]
        bool[] _powerDisabled = new bool[NUM_OCTAVES];

        [HideInInspector]
        public float[] _chopScales = new float[NUM_OCTAVES]
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        [HideInInspector]
        public float[] _gravityScales = new float[NUM_OCTAVES]
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        [Tooltip("Scales horizontal displacement"), Range(0f, 2f)]
        public float _chop = 1f;

        public bool _showAdvancedControls = false;

        public static float SmallWavelength(float octaveIndex) { return Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex); }

        public float GetAmplitude(float wavelength, float componentsPerOctave)
        {
            if (wavelength <= 0.001f)
            {
                Debug.LogError("Wavelength must be >= 0f");
                return 0f;
            }

            float wl_pow2 = Mathf.Log(wavelength) / Mathf.Log(2f);
            wl_pow2 = Mathf.Clamp(wl_pow2, SMALLEST_WL_POW_2, SMALLEST_WL_POW_2 + NUM_OCTAVES - 1f);

            int index = (int)(wl_pow2 - SMALLEST_WL_POW_2);

            if (index >= _powerLog.Length)
            {
                Debug.LogError("Out of bounds index");
                return 0f;
            }

            if (_powerDisabled[index])
            {
                return 0f;
            }

            // The amplitude calculation follows this nice paper from Frechot:
            // https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf
            float wl_lo = Mathf.Pow(2f, Mathf.Floor(wl_pow2));
            float k_lo = 2f * Mathf.PI / wl_lo;
            float omega_lo = k_lo * ComputeWaveSpeed(wl_lo);
            float wl_hi = 2f * wl_lo;
            float k_hi = 2f * Mathf.PI / wl_hi;
            float omega_hi = k_hi * ComputeWaveSpeed(wl_hi);

            float domega = (omega_lo - omega_hi) / componentsPerOctave;

            float a_2 = 2f * Mathf.Pow(10f, _powerLog[index]) * domega;
            var a = Mathf.Sqrt(a_2);
            return a;
        }

        float ComputeWaveSpeed(float wavelength)
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

        /// <summary>
        /// Samples spectrum to generate wave data. Wavelengths will be in ascending order.
        /// </summary>
        public void GenerateWaveData(int componentsPerOctave, ref float[] wavelengths, ref float[] anglesDeg)
        {
            var totalComponents = NUM_OCTAVES * componentsPerOctave;

            if (wavelengths == null || wavelengths.Length != totalComponents) wavelengths = new float[totalComponents];
            if (anglesDeg == null || anglesDeg.Length != totalComponents) anglesDeg = new float[totalComponents];

            var minWavelength = Mathf.Pow(2f, SMALLEST_WL_POW_2);
            var invComponentsPerOctave = 1f / componentsPerOctave;

            for (var octave = 0; octave < NUM_OCTAVES; octave++)
            {
                for (var i = 0; i < componentsPerOctave; i++)
                {
                    var index = octave * componentsPerOctave + i;

                    // stratified random sampling - should give a better range of wavelengths, and also means i can generated the
                    // wavelengths in sorted order!
                    var minWavelengthi = minWavelength + invComponentsPerOctave * minWavelength * i;
                    var  maxWavelengthi = Mathf.Min(minWavelengthi + invComponentsPerOctave * minWavelength, 2f * minWavelength);
                    wavelengths[index] = Mathf.Lerp(minWavelengthi, maxWavelengthi, Random.value);

                    var rnd = (i + Random.value) * invComponentsPerOctave;
                    anglesDeg[index] = (2f * rnd - 1f) * _waveDirectionVariance;
                }

                minWavelength *= 2f;
            }
        }

        public void ApplyPhillipsSpectrum(float windSpeed)
        {
            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                var pow = PhillipsSpectrum(windSpeed, OceanRenderer.Instance.WindDir, Mathf.Abs(Physics.gravity.y), Mathf.Pow(2f, SMALLEST_WL_POW_2), wl, 0f);
                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));
                _powerLog[octave] = Mathf.Log10(pow);
            }
        }

        public void ApplyPiersonMoskowitzSpectrum(float windSpeed)
        {
            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                var pow = PiersonMoskowitzSpectrum(Mathf.Abs(Physics.gravity.y), windSpeed, wl);
                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));
                _powerLog[octave] = Mathf.Log10(pow);
            }
        }

        public void ApplyJONSWAPSpectrum(float windSpeed, float fetch)
        {
            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                var pow = JONSWAPSpectrum(Mathf.Abs(Physics.gravity.y), windSpeed, wl, fetch);
                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));
                _powerLog[octave] = Mathf.Log10(pow);
            }
        }


        static float PhillipsSpectrum(float windSpeed, Vector2 windDir, float gravity, float smallestWavelength, float wavelength, float angle)
        {
            float wavenumber = 2f * Mathf.PI / wavelength;
            float angle_radians = Mathf.PI * angle / 180f;
            float kx = Mathf.Cos(angle_radians) * wavenumber;
            float kz = Mathf.Sin(angle_radians) * wavenumber;

            float k2 = kx * kx + kz * kz;

            float windSpeed2 = windSpeed * windSpeed;
            float wx = windDir.x;
            float wz = windDir.y;

            float kdotw = (wx * kx + wz * kz);

            float a = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            float L = windSpeed2 / gravity;

            // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.161.9102&rep=rep1&type=pdf
            return a * kdotw * kdotw * Mathf.Exp(-1f / (k2 * L * L)) / (k2 * k2);
        }

        // base of modern parametric wave spectrum
        static float PhilSpectrum(float gravity, float wavelength)
        {
            float alpha = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            return PhilSpectrum(gravity, alpha, wavelength);
        }
        // base of modern parametric wave spectrum
        static float PhilSpectrum(float gravity, float alpha, float wavelength)
        {
            //float alpha = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            float wavenumber = 2f * Mathf.PI / wavelength;
            float frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            return alpha * gravity * gravity / Mathf.Pow(frequency, 5f);
        }

        static float PiersonMoskowitzSpectrum(float gravity, float windspeed, float wavelength)
        {
            float wavenumber = 2f * Mathf.PI / wavelength;
            float frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            float frequency_peak = 0.855f * gravity / windspeed;
            return PhilSpectrum(gravity, wavelength) * Mathf.Exp(-Mathf.Pow(frequency_peak / frequency, 4f) * 5f / 4f);
        }
        static float PiersonMoskowitzSpectrum(float gravity, float windspeed, float frequency_peak, float alpha, float wavelength)
        {
            float wavenumber = 2f * Mathf.PI / wavelength;
            float frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            return PhilSpectrum(gravity, alpha, wavelength) * Mathf.Exp(-Mathf.Pow(frequency_peak / frequency, 4f) * 5f / 4f);
        }

        static float JONSWAPSpectrum(float gravity, float windspeed, float wavelength, float fetch)
        {
            // fetch distance
            float F = fetch;
            float alpha = 0.076f * Mathf.Pow(windspeed * windspeed / (F * gravity), 0.22f);

            float wavenumber = 2f * Mathf.PI / wavelength;
            float frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            float frequency_peak = 22f * Mathf.Pow(gravity * gravity / (windspeed * F), 1f / 3f);
            float sigma = frequency <= frequency_peak ? 0.07f : 0.09f;
            float r = Mathf.Exp(-Mathf.Pow(frequency - frequency_peak, 2f) / (2f * sigma * sigma * frequency_peak * frequency_peak));
            float gamma = 3.3f;

            return PiersonMoskowitzSpectrum(gravity, windspeed, frequency_peak, alpha, wavelength) * Mathf.Pow(gamma, r);
        }
    }
}
