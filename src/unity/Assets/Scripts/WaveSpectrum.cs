using UnityEngine;

namespace Crest
{
    public class WaveSpectrum : MonoBehaviour
    {

        const int NUM_OCTAVES = 12;
        const float SMALLEST_WL_POW_2 = -2f;

        [HideInInspector]
        public float[] _amp = new float[NUM_OCTAVES];
        [HideInInspector]
        public bool[] _ampEn = new bool[NUM_OCTAVES];

        [Range(0f, 2f)]
        public float _amplitudeScale = 1f;

        void Start()
        {
            for (int i = 0; i < _ampEn.Length; i++)
            {
                _ampEn[i] = true;
            }
        }

        private void Reset()
        {
            _amp = new float[NUM_OCTAVES];

            for (int i = 0; i < _ampEn.Length; i++)
            {
                _ampEn[i] = true;
            }
        }

        public float SmallestWavelength { get { return Mathf.Pow(2f, SMALLEST_WL_POW_2); } }
        public float SmallWavelength(float octaveIndex) { return Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex); }
        public float LargeWavelength(float octaveIndex) { return Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex + 1f); }

        public float GetAmplitude(float wavelength)
        {
            if (wavelength <= 0.001f)
            {
                Debug.LogError("Wavelength must be >= 0f");
                return 0f;
            }

            float wl_pow2 = Mathf.Log(wavelength) / Mathf.Log(2f);
            wl_pow2 = Mathf.Clamp(wl_pow2, SMALLEST_WL_POW_2, SMALLEST_WL_POW_2 + NUM_OCTAVES - 1f);

            int index = (int)(wl_pow2 - SMALLEST_WL_POW_2);

            if (index >= _amp.Length)
            {
                Debug.LogError("Out of bounds index");
                return 0f;
            }

            if (!_ampEn[index])
            {
                return 0f;
            }

            return _amplitudeScale * _amp[index];
        }

        [ContextMenu("Phillips")]
        void ApplyPhillipsSpectrum()
        {
            UnityEditor.Undo.RecordObject(this, "Apply Phillips Spectrum");

            var waves = GetComponent<ShapeGerstner>();

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                float wl = SmallWavelength(octave) * 1.5f;
                float energy = ShapeGerstner.PhillipsSpectrum(waves._windSpeed, waves.WindDir, Mathf.Abs(Physics.gravity.y), waves._minWavelength, wl, 0f);
                float amp = Mathf.Sqrt(2f * energy);
                _amp[octave] = amp;
            }
        }
    }
}
