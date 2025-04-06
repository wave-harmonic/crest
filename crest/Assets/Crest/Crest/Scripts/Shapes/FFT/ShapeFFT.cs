// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Crest
{
    /// <summary>
    /// FFT ocean wave shape
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shape FFT")]
    public partial class ShapeFFT : ShapeWaves
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 1;
#pragma warning restore 414

        [Header("Wave Conditions")]

        [Tooltip("When true, uses the wind turbulence on this component rather than the wind turbulence from the Ocean Renderer component.")]
        public bool _overrideGlobalWindTurbulence;
        public float WindTurbulence => _overrideGlobalWindTurbulence ? _windTurbulence : OceanRenderer.Instance.WindTurbulence;

        [Tooltip("Impacts how aligned waves are with wind.")]
        [Range(0, 1)]
        public float _windTurbulence = 0.145f;
        public float WindDirRadForFFT => _meshForDrawingWaves != null ? 0f : WaveDirectionHeadingAngle * Mathf.Deg2Rad;

        [Header("Culling")]
        [Tooltip("Maximum amount surface will be displaced vertically from sea level. Increase this if gaps appear at bottom of screen."), SerializeField]
        float _maxVerticalDisplacement = 10f;
        [Tooltip("Maximum amount a point on the surface will be displaced horizontally by waves from its rest position. Increase this if gaps appear at sides of screen."), SerializeField]
        float _maxHorizontalDisplacement = 15f;

        [Header("Collision Data Baking")]
        [Tooltip("Enable running this FFT with baked data. This makes the FFT periodic (repeating in time).")]
        public bool _enableBakedCollision = false;
        [Tooltip("Frames per second of baked data. Larger values may help the collision track the surface closely at the cost of more frames and increase baked data size."), DecoratedField, Predicated("_enableBakedCollision")]
        public int _timeResolution = 4;
        [Tooltip("Smallest wavelength required in collision. To preview the effect of this, disable power sliders in spectrum for smaller values than this number. Smaller values require more resolution and increase baked data size."), DecoratedField, Predicated("_enableBakedCollision")]
        public float _smallestWavelengthRequired = 2f;
        [Tooltip("FFT waves will loop with a period of this many seconds. Smaller values decrease data size but can make waves visibly repetitive."), Predicated("_enableBakedCollision"), Range(4f, 128f)]
        public float _timeLoopLength = 32f;


        // Debug
        [Space(10)]

        [SerializeField]
        DebugFields _debug = new DebugFields();
        protected override DebugFields DebugSettings => _debug;


        internal float LoopPeriod => _enableBakedCollision ? _timeLoopLength : -1f;

        protected override int MinimumResolution => 16;
        protected override int MaximumResolution => int.MaxValue;

        float _windTurbulenceOld;
        float _windSpeedOld;
        float _windDirRadOld;
        OceanWaveSpectrum _spectrumOld;

        public override float MinWavelength(int cascadeIdx)
        {
            var diameter = 0.5f * (1 << cascadeIdx);
            // Matches constant with same name in FFTSpectrum.compute
            var WAVE_SAMPLE_FACTOR = 8f;
            return diameter / WAVE_SAMPLE_FACTOR;

            // This used to be:
            //var texelSize = diameter / _resolution;
            //float samplesPerWave = _resolution / 8;
            //return texelSize * samplesPerWave;
        }

        public override void CrestUpdate(CommandBuffer buf)
        {
            // We do not filter FFTs.
            _firstCascade = 0;
            _lastCascade = CASCADE_COUNT - 1;

            base.CrestUpdate(buf);

            // If using geo, the primary wave dir is used by the input shader to rotate the waves relative
            // to the geo rotation. If not, the wind direction is already used in the FFT gen.
            var waveDir = _meshForDrawingWaves != null ? PrimaryWaveDirection : Vector2.right;
            _matGenerateWaves.SetVector(sp_AxisX, waveDir);

            // If geometry is being used, the ocean input shader will rotate the waves to align to geo
            var windDirRad = WindDirRadForFFT;
            var windSpeedMPS = WindSpeed;
            float loopPeriod = LoopPeriod;

            // Don't create tons of generators when values are varying. Notify so that existing generators may be adapted.
            if (_windTurbulenceOld != WindTurbulence || _windDirRadOld != windDirRad || _windSpeedOld != windSpeedMPS || _spectrumOld != _activeSpectrum)
            {
                FFTCompute.OnGenerationDataUpdated(_resolution, loopPeriod, _windTurbulenceOld, _windDirRadOld, _windSpeedOld, _spectrumOld, WindTurbulence, windDirRad, windSpeedMPS, _activeSpectrum);
            }

            var waveData = FFTCompute.GenerateDisplacements(buf, _resolution, loopPeriod, WindTurbulence, windDirRad, windSpeedMPS, OceanRenderer.Instance.CurrentTime, _activeSpectrum, UpdateDataEachFrame);

            _windTurbulenceOld = WindTurbulence;
            _windDirRadOld = windDirRad;
            _windSpeedOld = windSpeedMPS;
            _spectrumOld = _activeSpectrum;
            _matGenerateWaves.SetTexture(sp_WaveBuffer, waveData);
        }

        protected override void ReportMaxDisplacement()
        {
            // Apply weight or will cause popping due to scale change.
            _maxHorizDisp = _maxHorizontalDisplacement * _weight;
            _maxVertDisp = _maxWavesDisp = _maxVerticalDisplacement * _weight;

            if (IsGlobalWaves)
            {
                OceanRenderer.Instance.ReportMaxDisplacementFromShape(_maxHorizDisp, _maxVertDisp, _maxVertDisp);
            }
        }

        protected override void DestroySharedResources()
        {
            FFTCompute.CleanUpAll();
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (_debug._drawSlicesInEditor)
            {
                FFTCompute.OnGUI(_resolution, LoopPeriod, WindTurbulence, WindDirRadForFFT, WindSpeed, _activeSpectrum);
            }
        }
#endif
    }

    public partial class ShapeFFT : ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            if (_version == 0)
            {
                _overrideGlobalWindDirection = true;
                _version = 1;
            }
        }
    }

#if UNITY_EDITOR
    public partial class ShapeFFT : IValidated
    {
        public override bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = base.Validate(ocean, showMessage);

#if !CREST_UNITY_MATHEMATICS
            if (_enableBakedCollision)
            {
                showMessage
                (
                    "The <i>Unity Mathematics (com.unity.mathematics)</i> package is required for baking.",
                    "Add the <i>Unity Mathematics</i> package.",
                    ValidatedHelper.MessageType.Warning, this,
                    ValidatedHelper.FixAddMissingMathPackage
                );
            }
#endif

            return isValid;
        }
    }

    [CustomEditor(typeof(ShapeFFT))]
    public class ShapeFFTEditor : CustomBaseEditor
    {
#if CREST_UNITY_MATHEMATICS
        /// <summary>
        /// Display some validation and statistics about the bake.
        /// </summary>
        void BakeHelpBox(ShapeFFT target)
        {
            var message = "";

            FFTBaker.ComputeRequiredOctaves(target._spectrum, target._smallestWavelengthRequired, out var smallestOctaveRequired, out var largestOctaveRequired);
            if (largestOctaveRequired == -1 || smallestOctaveRequired == -1 || smallestOctaveRequired > largestOctaveRequired)
            {
                EditorGUILayout.HelpBox("No waves in spectrum. Increase one or more of the spectrum sliders.", MessageType.Error);
                return;
            }

            message += $"FFT resolution is {target._resolution}.";
            message += $" Spectrum power sliders give {largestOctaveRequired - smallestOctaveRequired + 1} active octaves greater than smallest wavelength {target._smallestWavelengthRequired}m.";
            var scales = largestOctaveRequired - smallestOctaveRequired + 2;
            message += $" Bake data resolution will be {target._resolution} x {target._resolution} x {scales}.";

            message += "\n\n";
            message += $"Period is {target._timeLoopLength}s.";
            message += $" Frames per second setting is {target._timeResolution}.";
            var frameCount = target._timeLoopLength * target._timeResolution;
            message += $" Frame count is {target._timeLoopLength} x {target._timeResolution} = {frameCount}.";

            message += "\n\n";
            var pointsPerFrame = target._resolution * target._resolution * scales;
            var channelCount = 4;
            var bytesPerChannel = 4;
            message += $"Total data size will be {pointsPerFrame * frameCount * channelCount * bytesPerChannel / 1048576f} MB.";

            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        public override void OnInspectorGUI()
        {
            var target = this.target as ShapeFFT;

            base.OnInspectorGUI();

            bool bakingEnabled = target._enableBakedCollision;

            if (bakingEnabled)
            {
                if (target._spectrum == null)
                {
                    EditorGUILayout.HelpBox("A spectrum must be assigned to enable collision baking.", MessageType.Error);
                    return;
                }

                BakeHelpBox(target);
            }

            GUI.enabled = bakingEnabled;
            OnInspectorGUIBaking();
            GUI.enabled = true;
        }

        /// <summary>
        /// Controls & GUI for baking.
        /// </summary>
        void OnInspectorGUIBaking()
        {
            if (OceanRenderer.Instance == null) return;

            var bakeLabel = "Bake to asset";
            var bakeAndAssignLabel = "Bake to asset and assign to current settings";
            var selectCurrentSettingsLabel = "Select current settings";
            if (OceanRenderer.Instance._simSettingsAnimatedWaves != null)
            {
                GUILayout.Space(10);

                if (GUILayout.Button(bakeLabel))
                {
                    FFTBaker.BakeShapeFFT(target as ShapeFFT);
                }

                GUI.enabled = GUI.enabled && OceanRenderer.Instance._simSettingsAnimatedWaves != null;
                if (GUILayout.Button(bakeAndAssignLabel))
                {
                    var result = FFTBaker.BakeShapeFFT(target as ShapeFFT);
                    if (result != null)
                    {
                        OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource = SimSettingsAnimatedWaves.CollisionSources.BakedFFT;
                        OceanRenderer.Instance._simSettingsAnimatedWaves._bakedFFTData = result;
                        Selection.activeObject = OceanRenderer.Instance._simSettingsAnimatedWaves;

                        // Rebuild ocean
                        OceanRenderer.Instance.Rebuild();
                    }
                }
                GUI.enabled = true;

                if (GUILayout.Button(selectCurrentSettingsLabel))
                {
                    Selection.activeObject = OceanRenderer.Instance._simSettingsAnimatedWaves;
                }
            }
            else
            {
                // No settings available, disable and show tooltip
                GUI.enabled = false;
                GUILayout.Button(new GUIContent(bakeAndAssignLabel, "No settings available to apply to. Assign an Animated Waves Sim Settings to the OceanRenderer component."));
                GUILayout.Button(new GUIContent(selectCurrentSettingsLabel, "No settings available. Assign an Animated Waves Sim Settings to the OceanRenderer component."));
                GUI.enabled = true;
            }
        }
#endif // CREST_UNITY_MATHEMATICS
    }
#endif
}
