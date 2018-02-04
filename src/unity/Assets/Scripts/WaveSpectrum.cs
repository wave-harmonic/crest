using UnityEngine;

namespace Crest
{
    public class WaveSpectrum : MonoBehaviour
    {
        const int NUM_OCTAVES = 12;
        const float SMALLEST_WL_POW_2 = -2f;

        [HideInInspector]
        public float[] _power = new float[NUM_OCTAVES];
        [HideInInspector]
        public bool[] _powerEnabled = new bool[NUM_OCTAVES];

        [Range(0f, 2f)]
        public float _amplitudeScale = 1f;

        [HideInInspector]
        public float _windSpeed = 5f;

        void Start()
        {
            for (int i = 0; i < _powerEnabled.Length; i++)
            {
                _powerEnabled[i] = true;
            }
        }

        private void Reset()
        {
            _power = new float[NUM_OCTAVES];

            for (int i = 0; i < _powerEnabled.Length; i++)
            {
                _powerEnabled[i] = true;
            }
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

        public void ApplyPhillipsSpectrum(float windSpeed)
        {
            UnityEditor.Undo.RecordObject(this, "Apply Phillips Spectrum");

            var waves = GetComponent<ShapeGerstner>();

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                _power[octave] = ShapeGerstner.PhillipsSpectrum(windSpeed, waves.WindDir, Mathf.Abs(Physics.gravity.y), waves._minWavelength, wl, 0f);
            }
        }
    }
}
