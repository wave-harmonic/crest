// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Ocean shape representation - power values for each octave of wave components.
    /// </summary>
    [CreateAssetMenu(fileName = "OceanWaves", menuName = "Crest/Ocean Wave Spectrum", order = 10000)]
    public class OceanWaveSpectrum : ScriptableObject
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        // These must match corresponding constants in FFTSpectrum.compute
        public const int NUM_OCTAVES = 14;
        public static readonly float SMALLEST_WL_POW_2 = -4f;

        [HideInInspector]
        public float _fetch = 500000f;

        public static readonly float MIN_POWER_LOG = -8f;
        public static readonly float MAX_POWER_LOG = 5f;

        [Tooltip("Variance of wave directions, in degrees. Gerstner-only - use the Turbulence param on the ShapeFFT component for FFT."), Range(0f, 180f)]
        public float _waveDirectionVariance = 90f;

        [Tooltip("More gravity means faster waves."), Range(0f, 25f)]
        public float _gravityScale = 1f;

        [Range(0f, 2f), HideInInspector]
        public float _smallWavelengthMultiplier = 1f;

        [Tooltip("Multiplier which scales waves"), Range(0f, 10f)]
        public float _multiplier = 1f;

        [HideInInspector, SerializeField]
        internal float[] _powerLog = new float[NUM_OCTAVES]
            { -5.71f, -5.03f, -4.54f, -3.88f, -3.28f, -2.32f, -1.78f, -1.21f, -0.54f, 0.28f, 0.54f, 1.03f, 1.44f, -8f };

        [HideInInspector, SerializeField]
        internal bool[] _powerDisabled = new bool[NUM_OCTAVES];

        [HideInInspector]
        public float[] _chopScales = new float[NUM_OCTAVES]
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        [HideInInspector]
        public float[] _gravityScales = new float[NUM_OCTAVES]
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        [Tooltip("Scales horizontal displacement"), Range(0f, 2f)]
        public float _chop = 1.6f;

        void Awake()
        {
            // For builds and when Shape* component is enabled in play mode.
            Upgrade();
        }

        void Reset()
        {
            // For when the reset button is used.
            Upgrade();
        }

        void Upgrade()
        {
            if (_version == 0)
            {
                // Auto-upgrade any new data objects directly to v1. This is in lieu of simply
                // giving _version a default value of 1 to distuingish new data, which we can't do
                // because _version is not present in the old data at all.
                // TODO: after a few releases, we can be sure _version will be present in the data.
                // At this point we can bump _version to a default value of 1 and from that point
                // onwards know that version is correct, and this auto upgrade path can go away.
                for (int i = 0; i < _powerLog.Length; i++)
                {
                    // This is equivalent to power /= 25, in log10 space
                    _powerLog[i] -= 1.39794f;
                }
                _version = 1;
            }
        }

#if UNITY_EDITOR
#pragma warning disable 414
        [SerializeField] bool _showAdvancedControls = false;
#pragma warning restore 414

        public enum SpectrumModel
        {
            None,
            PiersonMoskowitz,
        }

#pragma warning disable 414
        // We need to serialize if we want undo/redo.
        [HideInInspector, SerializeField] SpectrumModel _model;
#pragma warning restore 414
#endif

        public static float SmallWavelength(float octaveIndex) => Mathf.Pow(2f, SMALLEST_WL_POW_2 + octaveIndex);

        public static int GetOctaveIndex(float wavelength)
        {
            Debug.Assert(wavelength > 0f, "Crest: OceanWaveSpectrum: Wavelength must be > 0.");
            var wl_pow2 = Mathf.Log(wavelength) / Mathf.Log(2f);
            return (int)(wl_pow2 - SMALLEST_WL_POW_2);
        }

        /// <summary>
        /// Returns the amplitude of a wave described by wavelength.
        /// </summary>
        /// <param name="wavelength">Wavelength in m</param>
        /// <param name="componentsPerOctave">How many waves we're sampling, used to conserve energy for different sampling rates</param>
        /// <param name="windSpeed">Wind speed in m/s</param>
        /// <param name="power">The energy of the wave in J</param>
        /// <returns>The amplitude of the wave in m</returns>
        public float GetAmplitude(float wavelength, float componentsPerOctave, float windSpeed, out float power)
        {
            Debug.Assert(wavelength > 0f, "Crest: OceanWaveSpectrum: Wavelength must be > 0.", this);

            var wl_pow2 = Mathf.Log(wavelength) / Mathf.Log(2f);
            wl_pow2 = Mathf.Clamp(wl_pow2, SMALLEST_WL_POW_2, SMALLEST_WL_POW_2 + NUM_OCTAVES - 1f);

            var lower = Mathf.Pow(2f, Mathf.Floor(wl_pow2));

            var index = (int)(wl_pow2 - SMALLEST_WL_POW_2);

            if (_powerLog.Length < NUM_OCTAVES || _powerDisabled.Length < NUM_OCTAVES)
            {
                Debug.LogWarning($"Crest: Wave spectrum {name} is out of date, please open this asset and resave in editor.", this);
            }

            if (index >= _powerLog.Length || index >= _powerDisabled.Length)
            {
                Debug.Assert(index < _powerLog.Length && index < _powerDisabled.Length, $"Crest: OceanWaveSpectrum: index {index} is out of range.", this);
                power = 0f;
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
            var c_lo = ComputeWaveSpeed(wl_lo);
            var omega_lo = k_lo * c_lo;
            var wl_hi = 2f * wl_lo;
            var k_hi = 2f * Mathf.PI / wl_hi;
            var c_hi = ComputeWaveSpeed(wl_hi);
            var omega_hi = k_hi * c_hi;

            var domega = (omega_lo - omega_hi) / componentsPerOctave;

            // Alpha used to interpolate between power values
            var alpha = (wavelength - lower) / lower;

            // Power
            power = hasNextIndex ? Mathf.Lerp(thisPower, nextPower, alpha) : thisPower;
            power = Mathf.Pow(10f, power);

            // Empirical wind influence based on alpha-beta spectrum that underlies empirical spectra
            var gravity = _gravityScale * Mathf.Abs(Physics.gravity.y);
            var B = 1.291f;
            var wm = 0.87f * gravity / windSpeed;
            DeepDispersion(2f * Mathf.PI / wavelength, gravity, out var w);
            power *= Mathf.Exp(-B * Mathf.Pow(wm / w, 4.0f));

            var a_2 = 2f * power * domega;

            // Amplitude
            var a = Mathf.Sqrt(a_2);

            // Gerstner fudge -one hack to get Gerstners looking on par with FFT
            if (_version > 0)
            {
                a *= 5f;
            }

            return a * _multiplier;
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

                    // Stratified random sampling - should give a better distribution of wavelengths, and also means i can generate
                    // the wavelengths in ascending order!
                    var minWavelengthi = minWavelength + invComponentsPerOctave * minWavelength * i;
                    var maxWavelengthi = Mathf.Min(minWavelengthi + invComponentsPerOctave * minWavelength, 2f * minWavelength);
                    wavelengths[index] = Mathf.Lerp(minWavelengthi, maxWavelengthi, Random.value);

                    var rnd = (i + Random.value) * invComponentsPerOctave;
                    anglesDeg[index] = (2f * rnd - 1f) * _waveDirectionVariance;
                }

                minWavelength *= 2f;
            }
        }

        // This applies the correct PM spectrum powers, validated against a separate implementation
        public void ApplyPiersonMoskowitzSpectrum()
        {
            var gravity = Physics.gravity.magnitude;

            for (int octave = 0; octave < NUM_OCTAVES; octave++)
            {
                var wl = SmallWavelength(octave);

                var pow = PiersonMoskowitzSpectrum(gravity, wl);

                // we store power on logarithmic scale. this does not include 0, we represent 0 as min value
                pow = Mathf.Max(pow, Mathf.Pow(10f, MIN_POWER_LOG));

                _powerLog[octave] = Mathf.Log10(pow);
            }
        }

        // Alpha-beta spectrum without the beta. Beta represents wind influence and is evaluated at runtime
        // for 'current' wind conditions
        static float AlphaSpectrum(float A, float g, float w)
        {
            return A * g * g / Mathf.Pow(w, 5.0f);
        }

        static void DeepDispersion(float k, float gravity, out float w)
        {
            w = Mathf.Sqrt(gravity * k);
        }

        static float PiersonMoskowitzSpectrum(float gravity, float wavelength)
        {
            var k = 2f * Mathf.PI / wavelength;
            DeepDispersion(k, gravity, out var w);
            var phillipsConstant = 8.1e-3f;
            return AlphaSpectrum(phillipsConstant, gravity, w);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OceanWaveSpectrum))]
    public class OceanWaveSpectrumEditor : Editor
    {
        readonly static string[] modelDescriptions = new string[]
        {
            "Select an option to author waves using a spectrum model.",
            "Fully developed sea with infinite fetch.",
        };

        static void Upgrade(SerializedObject soSpectrum)
        {
            var spVer = soSpectrum.FindProperty("_version");

            // Upgrade to version 1: Calibrate spectrum power values to make gerstner waves match FFT.
            if (spVer.intValue == 0)
            {
                var powValues = soSpectrum.FindProperty("_powerLog");
                for (int i = 0; i < powValues.arraySize; i++)
                {
                    float pow = powValues.GetArrayElementAtIndex(i).floatValue;
                    pow = Mathf.Pow(10f, pow);
                    pow /= 25f;
                    pow = Mathf.Log10(pow);
                    powValues.GetArrayElementAtIndex(i).floatValue = pow;
                }
                // Spectrum model enum has changed so use "None" to be safe.
                soSpectrum.FindProperty("_model").enumValueIndex = 0;
                spVer.intValue = spVer.intValue + 1;
            }

            // Future: Upgrade to version 2: ...

            soSpectrum.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            Upgrade(serializedObject);

            base.OnInspectorGUI();

            var showAdvancedControls = serializedObject.FindProperty("_showAdvancedControls").boolValue;

            var spSpectrumModel = serializedObject.FindProperty("_model");
            var spectraIndex = serializedObject.FindProperty("_model").enumValueIndex;
            var spectrumModel = (OceanWaveSpectrum.SpectrumModel)Mathf.Clamp(spectraIndex, 0, 1);

            EditorGUILayout.Space();

            var spDisabled = serializedObject.FindProperty("_powerDisabled");
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
            var spChopScales = serializedObject.FindProperty("_chopScales");
            var spGravScales = serializedObject.FindProperty("_gravityScales");

            // Disable sliders if authoring with model.
            var canEditSpectrum = spectrumModel != OceanWaveSpectrum.SpectrumModel.None;

            for (int i = 0; i < spPower.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var spDisabled_i = spDisabled.GetArrayElementAtIndex(i);
                spDisabled_i.boolValue = !EditorGUILayout.Toggle(!spDisabled_i.boolValue, GUILayout.Width(15f));

                float smallWL = OceanWaveSpectrum.SmallWavelength(i);
                var spPower_i = spPower.GetArrayElementAtIndex(i);

                var isPowerDisabled = spDisabled_i.boolValue;
                var powerValue = isPowerDisabled ? OceanWaveSpectrum.MIN_POWER_LOG : spPower_i.floatValue;

                if (showAdvancedControls)
                {
                    EditorGUILayout.LabelField(string.Format("{0}", smallWL), EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    // Disable slider if authoring with model.
                    GUI.enabled = !canEditSpectrum && !spDisabled_i.boolValue;
                    powerValue = EditorGUILayout.Slider("    Power", powerValue, OceanWaveSpectrum.MIN_POWER_LOG, OceanWaveSpectrum.MAX_POWER_LOG);
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUILayout.LabelField(string.Format("{0}", smallWL), GUILayout.Width(30f));
                    // Disable slider if authoring with model.
                    GUI.enabled = !canEditSpectrum && !spDisabled_i.boolValue;
                    powerValue = GUILayout.HorizontalSlider(powerValue, OceanWaveSpectrum.MIN_POWER_LOG, OceanWaveSpectrum.MAX_POWER_LOG);
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    // This will create a tooltip for slider.
                    GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent("", powerValue.ToString()));
                }

                // If the power is disabled, we are using the MIN_POWER_LOG value so we don't want to store it.
                if (!isPowerDisabled)
                {
                    spPower_i.floatValue = powerValue;
                }

                if (showAdvancedControls)
                {
                    EditorGUILayout.Slider(spChopScales.GetArrayElementAtIndex(i), 0f, 4f, "    Chop Scale");
                    EditorGUILayout.Slider(spGravScales.GetArrayElementAtIndex(i), 0f, 4f, "    Grav Scale");
                }
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Empirical Spectra", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            spectrumModel = (OceanWaveSpectrum.SpectrumModel)EditorGUILayout.EnumPopup(spectrumModel);
            spSpectrumModel.enumValueIndex = (int)spectrumModel;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(modelDescriptions[(int)spectrumModel], MessageType.Info);
            EditorGUILayout.Space();

            if (spectrumModel == OceanWaveSpectrum.SpectrumModel.None)
            {
                Undo.RecordObject(spec, "Change Spectrum");
            }
            else
            {
                // It doesn't seem to matter where this is called.
                Undo.RecordObject(spec, $"Apply {ObjectNames.NicifyVariableName(spectrumModel.ToString())} Spectrum");


                // Descriptions from this very useful paper:
                // https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf

                switch (spectrumModel)
                {
                    case OceanWaveSpectrum.SpectrumModel.PiersonMoskowitz:
                        spec.ApplyPiersonMoskowitzSpectrum();
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                // We need to call this otherwise any property which has HideInInspector won't save.
                EditorUtility.SetDirty(spec);
            }
        }
    }
#endif
}
