// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Ocean shape representation - power values for each octave of wave components.
    /// </summary>
    [CreateAssetMenu(fileName = "OceanWaves", menuName = "Crest/Ocean Wave Spectrum", order = 10000)]
    public class OceanWaveSpectrum : ScriptableObject
    {
        public const int NUM_OCTAVES = 14;
        public static readonly float SMALLEST_WL_POW_2 = -4f;

        [HideInInspector]
        public float _windSpeed = 10f;

        [HideInInspector]
        public float _fetch = 500000f;

        public static readonly float MIN_POWER_LOG = -6f;
        public static readonly float MAX_POWER_LOG = 5f;

        [Tooltip("Variance of wave directions, in degrees"), Range(0f, 180f)]
        public float _waveDirectionVariance = 90f;

        [Tooltip("More gravity means faster waves."), Range(0f, 25f)]
        public float _gravityScale = 1f;

        [HideInInspector]
        public float _smallWavelengthMultiplier = 1f;

        [Tooltip("Multiplier"), Range(0f, 10f), SerializeField]
        float _multiplier = 1f;

        [SerializeField]
        float[] _powerLog = new float[NUM_OCTAVES]
            { -6f, -6f, -6f, -4.0088496f, -3.4452133f, -2.6996124f, -2.615044f, -1.2080691f, -0.53905386f, 0.27448857f, 0.53627354f, 1.0282621f, 1.4403292f, -6f };

        [SerializeField]
        bool[] _powerDisabled = new bool[NUM_OCTAVES];

        [HideInInspector]
        public float[] _chopScales = new float[NUM_OCTAVES]
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        [HideInInspector]
        public float[] _gravityScales = new float[NUM_OCTAVES]
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        [Tooltip("Scales horizontal displacement"), Range(0f, 2f)]
        public float _chop = 1.6f;

        public bool _showAdvancedControls = false;

        public static float SmallWavelength(float octaveIndex) { return Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex); }

        public float GetAmplitude(float wavelength, float componentsPerOctave)
        {
            // Always take random value so that sequence remains deterministic even if this function early outs
            var rand0 = Random.value;

            Debug.Assert(wavelength > 0f, "OceanWaveSpectrum: Wavelength must be >= 0f", this);

            var wl_pow2 = Mathf.Log(wavelength) / Mathf.Log(2f);
            wl_pow2 = Mathf.Clamp(wl_pow2, SMALLEST_WL_POW_2, SMALLEST_WL_POW_2 + NUM_OCTAVES - 1f);

            var lower = Mathf.Pow(2f, Mathf.Floor(wl_pow2));

            var index = (int)(wl_pow2 - SMALLEST_WL_POW_2);

            if(_powerLog.Length < NUM_OCTAVES)
            {
                Debug.LogWarning($"Wave spectrum {name} is out of date, please open this asset and resave in editor.", this);
            }

            if (index >= _powerLog.Length)
            {
                Debug.Assert(index < _powerLog.Length, $"OceanWaveSpectrum: index {index} is out of range.", this);
                return 0f;
            }

            // Get the first power for interpolation if available
            var thisPower = !_powerDisabled[index] ? _powerLog[index] : MIN_POWER_LOG;

            // Get the next power for interpolation if available
            var nextIndex = index + 1;
            var hasNextIndex = nextIndex < _powerLog.Length;
            var nextPower = hasNextIndex && !_powerDisabled[nextIndex] ? _powerLog[nextIndex] : MIN_POWER_LOG;

            // The amplitude calculation follows this nice paper from Frechot:
            // https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf
            var wl_lo = Mathf.Pow(2f, Mathf.Floor(wl_pow2));
            var k_lo = 2f * Mathf.PI / wl_lo;
            var omega_lo = k_lo * ComputeWaveSpeed(wl_lo);
            var wl_hi = 2f * wl_lo;
            var k_hi = 2f * Mathf.PI / wl_hi;
            var omega_hi = k_hi * ComputeWaveSpeed(wl_hi);

            var domega = (omega_lo - omega_hi) / componentsPerOctave;

            // Alpha used to interpolate between power values
            var alpha = (wavelength - lower) / lower;

            // Power
            var pow = hasNextIndex ? Mathf.Lerp(thisPower, nextPower, alpha) : thisPower;

            var a_2 = 2f * Mathf.Pow(10f, pow) * domega;

            // Amplitude
            var a = Mathf.Sqrt(a_2);

            return a * rand0 * _multiplier;
        }

        public static float ComputeWaveSpeed(float wavelength, float gravityMultiplier = 1f)
        {
            // wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
            // https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
            var g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            var k = 2f * Mathf.PI / wavelength;
            //float h = max(depth, 0.01);
            //float cp = sqrt(abs(tanh_clamped(h * k)) * g / k);
            var cp = Mathf.Sqrt(g / k);
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
                    var maxWavelengthi = Mathf.Min(minWavelengthi + invComponentsPerOctave * minWavelength, 2f * minWavelength);
                    wavelengths[index] = Mathf.Lerp(minWavelengthi, maxWavelengthi, Random.value);

                    var rnd = (i + Random.value) * invComponentsPerOctave;
                    anglesDeg[index] = (2f * rnd - 1f) * _waveDirectionVariance;
                }

                minWavelength *= 2f;
            }
        }

        public void ApplyPhillipsSpectrum(float windSpeed, float smallWavelengthMultiplier)
        {
            // Angles should usually be relative to wind direction, so setting wind direction to angle=0 should be ok.
            var windDir = Vector2.right;

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                // Shift wavelengths based on a magic number of this spectrum which seems to give small waves.
                var wl = SmallWavelength(octave) * smallWavelengthMultiplier * 1.5f;

                var pow = PhillipsSpectrum(windSpeed, windDir, Mathf.Abs(Physics.gravity.y), wl, 0f);
                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));
                _powerLog[octave] = Mathf.Log10(pow);
            }
        }

        public void ApplyPiersonMoskowitzSpectrum(float windSpeed, float smallWavelengthMultiplier)
        {
            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                // Shift wavelengths based on a magic number of this spectrum which seems to give small waves.
                var wl = SmallWavelength(octave) * smallWavelengthMultiplier * 9f;

                var pow = PiersonMoskowitzSpectrum(Mathf.Abs(Physics.gravity.y), windSpeed, wl);
                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));
                _powerLog[octave] = Mathf.Log10(pow);
            }
        }

        public void ApplyJONSWAPSpectrum(float windSpeed, float fetch, float smallWavelengthMultiplier)
        {
            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                // Shift wavelengths based on a magic number of this spectrum which seems to give small waves.
                var wl = SmallWavelength(octave) * smallWavelengthMultiplier * 9f;

                var pow = JONSWAPSpectrum(Mathf.Abs(Physics.gravity.y), windSpeed, wl, fetch);
                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));
                _powerLog[octave] = Mathf.Log10(pow);
            }
        }

        static float PhillipsSpectrum(float windSpeed, Vector2 windDir, float gravity, float wavelength, float angle)
        {
            var wavenumber = 2f * Mathf.PI / wavelength;
            var angle_radians = Mathf.PI * angle / 180f;
            var kx = Mathf.Cos(angle_radians) * wavenumber;
            var kz = Mathf.Sin(angle_radians) * wavenumber;
            
            var k2 = kx * kx + kz * kz;
            
            var windSpeed2 = windSpeed * windSpeed;
            var wx = windDir.x;
            var wz = windDir.y;
            
            var kdotw = (wx * kx + wz * kz);
            
            var a = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            var L = windSpeed2 / gravity;

            // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.161.9102&rep=rep1&type=pdf
            return a * kdotw * kdotw * Mathf.Exp(-1f / (k2 * L * L)) / (k2 * k2);
        }

        // base of modern parametric wave spectrum
        static float PhilSpectrum(float gravity, float wavelength)
        {
            var alpha = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            return PhilSpectrum(gravity, alpha, wavelength);
        }
        // base of modern parametric wave spectrum
        static float PhilSpectrum(float gravity, float alpha, float wavelength)
        {
            //float alpha = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            var wavenumber = 2f * Mathf.PI / wavelength;
            var frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            return alpha * gravity * gravity / Mathf.Pow(frequency, 5f);
        }

        static float PiersonMoskowitzSpectrum(float gravity, float windspeed, float wavelength)
        {
            var wavenumber = 2f * Mathf.PI / wavelength;
            var frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            var frequency_peak = 0.855f * gravity / windspeed;
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
            var F = fetch;
            var alpha = 0.076f * Mathf.Pow(windspeed * windspeed / (F * gravity), 0.22f);

            var wavenumber = 2f * Mathf.PI / wavelength;
            var frequency = Mathf.Sqrt(gravity * wavenumber); // deep water - depth > wavelength/2
            var frequency_peak = 22f * Mathf.Pow(gravity * gravity / (windspeed * F), 1f / 3f);
            var sigma = frequency <= frequency_peak ? 0.07f : 0.09f;
            var r = Mathf.Exp(-Mathf.Pow(frequency - frequency_peak, 2f) / (2f * sigma * sigma * frequency_peak * frequency_peak));
            var gamma = 3.3f;

            return PiersonMoskowitzSpectrum(gravity, windspeed, frequency_peak, alpha, wavelength) * Mathf.Pow(gamma, r);
        }

#if UNITY_EDITOR
        public void Upgrade()
        {
            OceanWaveSpectrumEditor.UpgradeSpectrum(ref _chopScales, 1f);
            OceanWaveSpectrumEditor.UpgradeSpectrum(ref _gravityScales, 1f);
            OceanWaveSpectrumEditor.UpgradeSpectrum(ref _powerDisabled, false);
            OceanWaveSpectrumEditor.UpgradeSpectrum(ref _powerLog, MIN_POWER_LOG);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OceanWaveSpectrum))]
    public class OceanWaveSpectrumEditor : Editor
    {
        private static GUIStyle ToggleButtonStyleNormal = null;
        private static GUIStyle ToggleButtonStyleToggled = null;

        static bool _applyPhillipsSpectrum = false;
        static bool _applyPiersonMoskowitzSpectrum = false;
        static bool _applyJONSWAPSpectrum = false;

        static GUIContent s_labelPhillips = new GUIContent("Phillips", "Base of modern parametric wave spectra");
        static GUIContent s_labelPiersonMoskowitz = new GUIContent("Pierson-Moskowitz", "Fully developed sea with infinite fetch");
        static GUIContent s_labelJONSWAP = new GUIContent("JONSWAP", "Fetch limited sea where waves continue to grow");
        static GUIContent s_labelSWM = new GUIContent("Small wavelength multiplier", "Modifies parameters for the empirical spectra, tends to boost smaller wavelengths");
        static GUIContent s_labelFetch = new GUIContent("Fetch", "Length of area that wind excites waves. Applies only to JONSWAP");

        public static void UpgradeSpectrum(SerializedProperty prop, float defaultValue)
        {
            while (prop.arraySize < OceanWaveSpectrum.NUM_OCTAVES)
            {
                prop.InsertArrayElementAtIndex(0);
                prop.GetArrayElementAtIndex(0).floatValue = defaultValue;
            }
        }
        public static void UpgradeSpectrum(SerializedProperty prop, bool defaultValue)
        {
            while (prop.arraySize < OceanWaveSpectrum.NUM_OCTAVES)
            {
                prop.InsertArrayElementAtIndex(0);
                prop.GetArrayElementAtIndex(0).boolValue = defaultValue;
            }
        }
        public static void UpgradeSpectrum(ref float[] values, float defaultValue)
        {
            while (values.Length < OceanWaveSpectrum.NUM_OCTAVES)
            {
                ArrayUtility.Insert(ref values, 0, defaultValue);
            }
        }
        public static void UpgradeSpectrum(ref bool[] values, bool defaultValue)
        {
            while (values.Length < OceanWaveSpectrum.NUM_OCTAVES)
            {
                ArrayUtility.Insert(ref values, 0, defaultValue);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var showAdvancedControls = serializedObject.FindProperty("_showAdvancedControls").boolValue;

            // preamble - styles for toggle buttons. this code and the below was based off the useful info provided by user Lasse here:
            // https://gamedev.stackexchange.com/questions/98920/how-do-i-create-a-toggle-button-in-unity-inspector
            if (ToggleButtonStyleNormal == null)
            {
                ToggleButtonStyleNormal = "Button";
                ToggleButtonStyleToggled = new GUIStyle(ToggleButtonStyleNormal);
                ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;
            }

            EditorGUILayout.Space();

            var spDisabled = serializedObject.FindProperty("_powerDisabled");
            UpgradeSpectrum(spDisabled, false);
            EditorGUILayout.BeginHorizontal();
            bool allEnabled = true;
            for (int i = 0; i < spDisabled.arraySize; i++)
            {
                if (spDisabled.GetArrayElementAtIndex(i).boolValue) allEnabled = false;
            }
            bool toggle = allEnabled;
            if (toggle != EditorGUILayout.Toggle(toggle, GUILayout.Width(13f)))
            {
                for (int i = 0; i < spDisabled.arraySize; i++)
                {
                    spDisabled.GetArrayElementAtIndex(i).boolValue = toggle;
                }
            }
            EditorGUILayout.LabelField("Spectrum", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            var spec = target as OceanWaveSpectrum;

            var spPower = serializedObject.FindProperty("_powerLog");
            UpgradeSpectrum(spPower, OceanWaveSpectrum.MIN_POWER_LOG);
            var spChopScales = serializedObject.FindProperty("_chopScales");
            UpgradeSpectrum(spChopScales, 1f);
            var spGravScales = serializedObject.FindProperty("_gravityScales");
            UpgradeSpectrum(spGravScales, 1f);

            for (int i = 0; i < spPower.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var spDisabled_i = spDisabled.GetArrayElementAtIndex(i);
                spDisabled_i.boolValue = !EditorGUILayout.Toggle(!spDisabled_i.boolValue, GUILayout.Width(15f));

                float smallWL = OceanWaveSpectrum.SmallWavelength(i);
                var spPower_i = spPower.GetArrayElementAtIndex(i);

                if (showAdvancedControls)
                {
                    EditorGUILayout.LabelField(string.Format("{0}", smallWL), EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Slider(spPower_i, OceanWaveSpectrum.MIN_POWER_LOG, OceanWaveSpectrum.MAX_POWER_LOG, "    Power");
                }
                else
                {
                    EditorGUILayout.LabelField(string.Format("{0}", smallWL), GUILayout.Width(30f));
                    spPower_i.floatValue = GUILayout.HorizontalSlider(spPower_i.floatValue, OceanWaveSpectrum.MIN_POWER_LOG, OceanWaveSpectrum.MAX_POWER_LOG);
                    EditorGUILayout.EndHorizontal();
                }

                if (showAdvancedControls)
                {
                    EditorGUILayout.Slider(spChopScales.GetArrayElementAtIndex(i), 0f, 4f, "    Chop Scale");
                    EditorGUILayout.Slider(spGravScales.GetArrayElementAtIndex(i), 0f, 4f, "    Grav Scale");
                }
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Empirical Spectra", EditorStyles.boldLabel);

            var labelWidth = 170f;
            EditorGUILayout.BeginHorizontal();
            float spd_kmh = spec._windSpeed * 3.6f;
            EditorGUILayout.LabelField("Wind speed (km/h)", GUILayout.Width(labelWidth));
            spd_kmh = EditorGUILayout.Slider(spd_kmh, 0f, 120f);
            spec._windSpeed = spd_kmh / 3.6f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(s_labelSWM, GUILayout.Width(labelWidth));
            spec._smallWavelengthMultiplier = EditorGUILayout.Slider(spec._smallWavelengthMultiplier, 0f, 10f);
            EditorGUILayout.EndHorizontal();

            // descriptions from this very useful paper: https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf

            if (GUILayout.Button(s_labelPhillips, _applyPhillipsSpectrum ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                _applyPhillipsSpectrum = !_applyPhillipsSpectrum;
            }
            if (_applyPhillipsSpectrum)
            {
                _applyJONSWAPSpectrum = _applyPiersonMoskowitzSpectrum = false;

                Undo.RecordObject(this, "Apply Phillips Spectrum");

                spec.ApplyPhillipsSpectrum(spec._windSpeed, spec._smallWavelengthMultiplier);
            }

            if (GUILayout.Button(s_labelPiersonMoskowitz, _applyPiersonMoskowitzSpectrum ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                _applyPiersonMoskowitzSpectrum = !_applyPiersonMoskowitzSpectrum;
            }
            if (_applyPiersonMoskowitzSpectrum)
            {
                _applyPhillipsSpectrum = _applyJONSWAPSpectrum = false;

                Undo.RecordObject(this, "Apply Pierson-Moskowitz Spectrum");

                spec.ApplyPiersonMoskowitzSpectrum(spec._windSpeed, spec._smallWavelengthMultiplier);
            }

            spec._fetch = EditorGUILayout.Slider(s_labelFetch, spec._fetch, 0f, 1000000f);

            if (GUILayout.Button(s_labelJONSWAP, _applyJONSWAPSpectrum ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                _applyJONSWAPSpectrum = !_applyJONSWAPSpectrum;
            }
            if (_applyJONSWAPSpectrum)
            {
                _applyPhillipsSpectrum = _applyPiersonMoskowitzSpectrum = false;

                Undo.RecordObject(this, "Apply JONSWAP Spectrum");

                spec.ApplyJONSWAPSpectrum(spec._windSpeed, spec._fetch, spec._smallWavelengthMultiplier);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
