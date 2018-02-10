using UnityEngine;

namespace Crest
{
    public class WaveSpectrum : MonoBehaviour
    {
        const int NUM_OCTAVES = 12;
        const float SMALLEST_WL_POW_2 = -2f;

        [Delayed]
        public int _componentsPerOctave = 10;

        [HideInInspector]
        public float[] _power = new float[NUM_OCTAVES];
        [HideInInspector]
        public bool[] _powerEnabled = new bool[NUM_OCTAVES];

        [Range(0f, 2f)]
        public float _amplitudeScale = 1f;

        [HideInInspector]
        public float _windSpeed = 5f;

        [HideInInspector]
        public float _fetch = 1000f;

        private void Reset()
        {
            _power = new float[NUM_OCTAVES];
        }

        public float SmallestWavelength { get { return Mathf.Pow(2f, SMALLEST_WL_POW_2); } }
        public float SmallWavelength(float octaveIndex) { return Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex); }
        public float LargeWavelength(float octaveIndex) { return Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex + 1f); }

        public float GetPower(float wavelength)
        {
            if (wavelength <= 0.001f)
            {
                Debug.LogError("Wavelength must be >= 0f");
                return 0f;
            }

            float wl_pow2 = Mathf.Log(wavelength) / Mathf.Log(2f);
            wl_pow2 = Mathf.Clamp(wl_pow2, SMALLEST_WL_POW_2, SMALLEST_WL_POW_2 + NUM_OCTAVES - 1f);

            int index = (int)(wl_pow2 - SMALLEST_WL_POW_2);

            if (index >= _power.Length)
            {
                Debug.LogError("Out of bounds index");
                return 0f;
            }

            if (!_powerEnabled[index])
            {
                return 0f;
            }

            return _amplitudeScale * _power[index];
        }

        public void GenerateWavelengths(ref float[] wavelengths, ref float[] anglesDeg, ref float[] phases)
        {
            int totalComponents = NUM_OCTAVES * _componentsPerOctave;

            if (wavelengths == null || wavelengths.Length != totalComponents) wavelengths = new float[totalComponents];
            if (anglesDeg == null || anglesDeg.Length != totalComponents) anglesDeg = new float[totalComponents];
            if (phases == null || phases.Length != totalComponents) phases = new float[totalComponents];

            float minWavelength = Mathf.Pow(2f, SMALLEST_WL_POW_2);

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                for (int i = 0; i < _componentsPerOctave; i++)
                {
                    int index = octave * _componentsPerOctave + i;
                    wavelengths[index] = minWavelength * (1f + Random.value);
                    phases[index] = 2f * Mathf.PI * Random.value;
                }

                System.Array.Sort(wavelengths, octave * _componentsPerOctave, _componentsPerOctave);

                minWavelength *= 2f;
            }
        }

        public void ApplyPhillipsSpectrum(float windSpeed)
        {
            UnityEditor.Undo.RecordObject(this, "Apply Phillips Spectrum");

            var waves = GetComponent<ShapeGerstner>();

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                _power[octave] = PhillipsSpectrum(windSpeed, waves.WindDir, Mathf.Abs(Physics.gravity.y), waves._minWavelength, wl, 0f);
            }
        }

        public void ApplyPiersonMoskowitzSpectrum(float windSpeed)
        {
            UnityEditor.Undo.RecordObject(this, "Apply Pierson-Moskowitz Spectrum");

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                _power[octave] = PiersonMoskowitzSpectrum(Mathf.Abs(Physics.gravity.y), windSpeed, wl);
            }
        }

        public void ApplyJONSWAPSpectrum(float windSpeed)
        {
            UnityEditor.Undo.RecordObject(this, "Apply JONSWAP Spectrum");

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                _power[octave] = JONSWAPSpectrum(Mathf.Abs(Physics.gravity.y), windSpeed, wl, _fetch);
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
