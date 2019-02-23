// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Support script for Gerstner wave ocean shapes.
    /// Generates a number of batches of Gerstner waves.
    /// </summary>
    public class ShapeGerstnerBatched : MonoBehaviour, ICollProvider
    {
        [Tooltip("Geometry to rasterize into wave buffers to generate waves.")]
        public Mesh _rasterMesh;
        [Tooltip("Shader to be used to render out a single Gerstner octave.")]
        public Shader _waveShader;
        [Tooltip("The spectrum that defines the ocean surface shape. Create asset of type Crest/Ocean Waves Spectrum.")]
        public OceanWaveSpectrum _spectrum;

        [Delayed, Tooltip("How many wave components to generate in each octave.")]
        public int _componentsPerOctave = 5;

        [Range(0f, 1f)]
        public float _weight = 1f;

        public int _randomSeed = 0;

        // data for all components
        float[] _wavelengths;
        float[] _amplitudes;
        float[] _angleDegs;
        float[] _phases;

        // useful references
        Material[] _materials;
        bool[] _drawLOD;
        Material _materialBigWaveTransition;
        bool _drawLODTransitionWaves;

        // IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatch.shader, both must be updated together!
        const int BATCH_SIZE = 32;

        enum CmdBufStatus
        {
            NoStatus,
            NotAttached,
            Attached
        }

        // scratch data used by batching code
        struct UpdateBatchScratchData
        {
            public static Vector4[] _twoPiOverWavelengthsBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _ampsBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _waveDirXBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _waveDirZBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _phasesBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _chopAmpsBatch = new Vector4[BATCH_SIZE / 4];
        }

        void Start()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            if (_spectrum == null)
            {
                _spectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _spectrum.name = "Default Waves (auto)";
            }
        }

        void InitPhases()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            var totalComps = _componentsPerOctave * OceanWaveSpectrum.NUM_OCTAVES;
            _phases = new float[totalComps];
            for (var octave = 0; octave < OceanWaveSpectrum.NUM_OCTAVES; octave++)
            {
                for (var i = 0; i < _componentsPerOctave; i++)
                {
                    var index = octave * _componentsPerOctave + i;
                    var rnd = (i + Random.value) / _componentsPerOctave;
                    _phases[index] = 2f * Mathf.PI * rnd;
                }
            }

            Random.state = randomStateBkp;
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            if (_phases == null) return;

            var windAngle = OceanRenderer.Instance._windDirectionAngle;
            for (int i = 0; i < _phases.Length; i++)
            {
                var direction = new Vector3(Mathf.Cos((windAngle + _angleDegs[i]) * Mathf.Deg2Rad), 0f, Mathf.Sin((windAngle + _angleDegs[i]) * Mathf.Deg2Rad));
                var phaseOffsetMeters = Vector3.Dot(newOrigin, direction);

                // wave number
                var k = 2f * Mathf.PI / _wavelengths[i];

                _phases[i] = Mathf.Repeat(_phases[i] + phaseOffsetMeters * k, Mathf.PI * 2f);
            }
        }

        void Update()
        {
            if (_phases == null || _phases.Length != _componentsPerOctave * OceanWaveSpectrum.NUM_OCTAVES)
            {
                InitPhases();
            }

            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWaveData(_componentsPerOctave, ref _wavelengths, ref _angleDegs);

            Random.state = randomStateBkp;

            UpdateAmplitudes();

            ReportMaxDisplacement();

            // this is done every frame for flexibility/convenience, in case the lod count changes
            if (_materials == null || _materials.Length != OceanRenderer.Instance.CurrentLodCount)
            {
                InitMaterials();
            }
        }

        void UpdateAmplitudes()
        {
            if (_amplitudes == null || _amplitudes.Length != _wavelengths.Length)
            {
                _amplitudes = new float[_wavelengths.Length];
            }

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                _amplitudes[i] = _weight * _spectrum.GetAmplitude(_wavelengths[i], _componentsPerOctave);
            }
        }

        private void ReportMaxDisplacement()
        {
            float ampSum = 0f;
            for (int i = 0; i < _wavelengths.Length; i++)
            {
                ampSum += _amplitudes[i] * _spectrum._chopScales[i / _componentsPerOctave];
            }
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(ampSum * _spectrum._chop, ampSum);
        }

        void InitMaterials()
        {
            foreach (var child in transform)
            {
                Destroy((child as Transform).gameObject);
            }

            // num octaves plus one, because there is an additional last bucket for large wavelengths
            _materials = new Material[OceanRenderer.Instance.CurrentLodCount];
            _drawLOD = new bool[_materials.Length];

            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i] = new Material(_waveShader);
                _drawLOD[i] = false;
            }

            _materialBigWaveTransition = new Material(_waveShader);
            _drawLODTransitionWaves = false;
        }

        /// <summary>
        /// Computes Gerstner params for a set of waves, for the given lod idx. Writes shader data to the given material.
        /// Returns number of wave components rendered in this batch.
        /// </summary>
        int UpdateBatch(int lodIdx, int firstComponent, int lastComponentNonInc, Material material)
        {
            int numComponents = lastComponentNonInc - firstComponent;
            int numInBatch = 0;
            int dropped = 0;

            float twopi = 2f * Mathf.PI;
            float one_over_2pi = 1f / twopi;
            float minWavelengthThisBatch = OceanRenderer.Instance._lods[lodIdx].MaxWavelength() / 2f;
            float maxWavelengthCurrentlyRendering = OceanRenderer.Instance._lods[OceanRenderer.Instance.CurrentLodCount - 1].MaxWavelength();
            float viewerAltitudeLevelAlpha = OceanRenderer.Instance.ViewerAltitudeLevelAlpha;

            // register any nonzero components
            for (int i = 0; i < numComponents; i++)
            {
                float wl = _wavelengths[firstComponent + i];

                // compute amp - contains logic for shifting wave components between last two lods..
                float amp = _amplitudes[firstComponent + i];
                bool renderingIntoLastTwoLods = minWavelengthThisBatch * 4.01f > maxWavelengthCurrentlyRendering;
                // no special weighting needed for any lods except the last 2
                if (renderingIntoLastTwoLods)
                {
                    bool renderingIntoLastLod = minWavelengthThisBatch * 2.01f > maxWavelengthCurrentlyRendering;
                    if (renderingIntoLastLod)
                    {
                        // example: fade out the last lod as viewer drops in altitude, so there is no pop when the lod chain shifts in scale
                        amp *= viewerAltitudeLevelAlpha;
                    }
                    else
                    {
                        // rendering to second-to-last lod. nothing required unless we are dealing with large wavelengths, which we want to transition into
                        // this second-to-last lod when the viewer drops in altitude, ready for a seamless transition when the lod chain shifts in scale
                        amp *= (wl < 2f * minWavelengthThisBatch) ? 1f : 1f - viewerAltitudeLevelAlpha;
                    }
                }

                if (amp >= 0.001f)
                {
                    if (numInBatch < BATCH_SIZE)
                    {
                        int vi = numInBatch / 4;
                        int ei = numInBatch - vi * 4;

                        UpdateBatchScratchData._twoPiOverWavelengthsBatch[vi][ei] = 2f * Mathf.PI / wl;
                        UpdateBatchScratchData._ampsBatch[vi][ei] = amp;

                        float chopScale = _spectrum._chopScales[(firstComponent + i) / _componentsPerOctave];
                        UpdateBatchScratchData._chopAmpsBatch[vi][ei] = -chopScale * _spectrum._chop * amp;

                        float angle = Mathf.Deg2Rad * (OceanRenderer.Instance._windDirectionAngle + _angleDegs[firstComponent + i]);
                        UpdateBatchScratchData._waveDirXBatch[vi][ei] = Mathf.Cos(angle);
                        UpdateBatchScratchData._waveDirZBatch[vi][ei] = Mathf.Sin(angle);

                        // It used to be this, but I'm pushing all the stuff that doesn't depend on position into the phase.
                        //half4 angle = k * (C * _CrestTime + x) + _Phases[vi];
                        float gravityScale = _spectrum._gravityScales[(firstComponent + i) / _componentsPerOctave];
                        float gravity = OceanRenderer.Instance.Gravity * _spectrum._gravityScale;
                        float C = Mathf.Sqrt(wl * gravity * gravityScale * one_over_2pi);
                        float k = twopi / wl;
                        UpdateBatchScratchData._phasesBatch[vi][ei] = _phases[firstComponent + i] + k * C * OceanRenderer.Instance.CurrentTime;

                        numInBatch++;
                    }
                    else
                    {
                        dropped++;
                    }
                }
            }

            if (dropped > 0)
            {
                Debug.LogWarning(string.Format("Gerstner LOD{0}: Batch limit reached, dropped {1} wavelengths. To support bigger batch sizes, see the comment around the BATCH_SIZE declaration.", lodIdx, dropped), this);
                numComponents = BATCH_SIZE;
            }

            if (numInBatch == 0)
            {
                // no waves to draw - abort
                return numInBatch;
            }

            // if we did not fill the batch, put a terminator signal after the last position
            if (numInBatch < BATCH_SIZE)
            {
                int vi_last = numInBatch / 4;
                int ei_last = numInBatch - vi_last * 4;

                for (int vi = vi_last; vi < BATCH_SIZE / 4; vi++)
                {
                    for (int ei = ei_last; ei < 4; ei++)
                    {
                        UpdateBatchScratchData._twoPiOverWavelengthsBatch[vi][ei] = 1f; // wary of NaNs
                        UpdateBatchScratchData._ampsBatch[vi][ei] = 0f;
                        UpdateBatchScratchData._waveDirXBatch[vi][ei] = 0f;
                        UpdateBatchScratchData._waveDirZBatch[vi][ei] = 0f;
                        UpdateBatchScratchData._phasesBatch[vi][ei] = 0f;
                        UpdateBatchScratchData._chopAmpsBatch[vi][ei] = 0f;
                    }

                    ei_last = 0;
                }
            }

            // apply the data to the shape material
            material.SetVectorArray("_TwoPiOverWavelengths", UpdateBatchScratchData._twoPiOverWavelengthsBatch);
            material.SetVectorArray("_Amplitudes", UpdateBatchScratchData._ampsBatch);
            material.SetVectorArray("_WaveDirX", UpdateBatchScratchData._waveDirXBatch);
            material.SetVectorArray("_WaveDirZ", UpdateBatchScratchData._waveDirZBatch);
            material.SetVectorArray("_Phases", UpdateBatchScratchData._phasesBatch);
            material.SetVectorArray("_ChopAmps", UpdateBatchScratchData._chopAmpsBatch);
            material.SetFloat("_NumInBatch", numInBatch);
            material.SetFloat("_AttenuationInShallows", OceanRenderer.Instance._lodDataAnimWaves.Settings.AttenuationInShallows);

            int numVecs = (numInBatch + 3) / 4;
            material.SetInt("_NumWaveVecs", numVecs);
            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(lodIdx, 0, material);

            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(lodIdx, 0, material, false);
            }

            return numInBatch;
        }

        /// <summary>
        /// More complicated than one would hope - loops over each component and assigns to a Gerstner batch which will render to a LOD.
        /// the camera WL range does not always match the octave WL range (because the vertices per wave is not constrained to powers of
        /// 2, unfortunately), so i cant easily just loop over octaves. also any WLs that either go to the last WDC, or don't fit in the last
        /// WDC, are rendered into both the last and second-to-last WDCs, in order to transition them smoothly without pops in all scenarios.
        /// </summary>
        void LateUpdate()
        {
            int componentIdx = 0;

            // seek forward to first wavelength that is big enough to render into current LODs
            float minWl = OceanRenderer.Instance._lods[0].MaxWavelength() / 2f;
            while (_wavelengths[componentIdx] < minWl && componentIdx < _wavelengths.Length)
            {
                componentIdx++;
            }

            // batch together appropriate wavelengths for each lod, except the last lod, which are handled separately below
            for (int lod = 0; lod < OceanRenderer.Instance.CurrentLodCount - 1; lod++, minWl *= 2f)
            {
                int startCompIdx = componentIdx;
                while (componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < 2f * minWl)
                {
                    componentIdx++;
                }

                _drawLOD[lod] = UpdateBatch(lod, startCompIdx, componentIdx, _materials[lod]) > 0;
            }

            // the last batch handles waves for the last lod, and waves that did not fit in the last lod
            _drawLOD[OceanRenderer.Instance.CurrentLodCount - 1] =
                UpdateBatch(OceanRenderer.Instance.CurrentLodCount - 1, componentIdx, _wavelengths.Length, _materials[OceanRenderer.Instance.CurrentLodCount - 1]) > 0;
            _drawLODTransitionWaves =
                UpdateBatch(OceanRenderer.Instance.CurrentLodCount - 2, componentIdx, _wavelengths.Length, _materialBigWaveTransition) > 0;
        }

        /// <summary>
        /// Submit draws to create the Gerstner waves. LODs from 0 to N-2 render the Gerstner waves from their lod. Additionally, any waves
        /// in the biggest lod, or too big for the biggest lod, are rendered into both of the last two LODs N-1 and N-2, as this allows us to
        /// move these waves between LODs without pops when the camera changes heights and the LODs need to change scale.
        /// </summary>
        public void BuildCommandBuffer(int lodIdx, OceanRenderer ocean, CommandBuffer buf)
        {
            var lodCount = ocean.CurrentLodCount;

            // LODs up to but not including the last lod get the normal sets of waves
            if (lodIdx < lodCount - 1 && _drawLOD[lodIdx])
            {
                buf.DrawMesh(_rasterMesh, Matrix4x4.identity, _materials[lodIdx]);
            }

            // The second-to-last lod will transition content into it from the last lod
            if (lodIdx == lodCount - 2 && _drawLODTransitionWaves)
            {
                buf.DrawMesh(_rasterMesh, Matrix4x4.identity, _materialBigWaveTransition);
            }

            // Last lod gets the big wavelengths
            if (lodIdx == lodCount - 1 && _drawLOD[lodIdx])
            {
                buf.DrawMesh(_rasterMesh, Matrix4x4.identity, _materials[OceanRenderer.Instance.CurrentLodCount - 1]);
            }
        }

        void OnEnable()
        {
            if (OceanRenderer.Instance != null && OceanRenderer.Instance._lodDataAnimWaves != null)
            {
                OceanRenderer.Instance._lodDataAnimWaves.AddGerstnerComponent(this);
            }
        }

        void OnDisable()
        {
            if (OceanRenderer.Instance != null && OceanRenderer.Instance._lodDataAnimWaves != null)
            {
                OceanRenderer.Instance._lodDataAnimWaves.RemoveGerstnerComponent(this);
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

        public bool GetSurfaceVelocity(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_surfaceVel)
        {
            o_surfaceVel = Vector3.zero;

            if (_amplitudes == null) return false;

            Vector2 pos = new Vector2(i_worldPos.x, i_worldPos.z);
            float mytime = OceanRenderer.Instance.CurrentTime;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;
            float minWaveLength = i_samplingData._minSpatialLength / 2f;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;
                if (_wavelengths[j] < minWaveLength) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                Vector2 D = new Vector2(Mathf.Cos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad), Mathf.Sin((windAngle + _angleDegs[j]) * Mathf.Deg2Rad));
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float disp = -_spectrum._chop * k * C * Mathf.Cos(t);
                o_surfaceVel += _amplitudes[j] * new Vector3(
                    D.x * disp,
                    -k * C * Mathf.Sin(t),
                    D.y * disp
                    );
            }

            return true;
        }

        public bool SampleHeight(ref Vector3 i_worldPos, SamplingData i_samplingData, out float o_height)
        {
            o_height = 0f;

            Vector3 posFlatland = i_worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            Vector3 undisplacedPos;
            if (!ComputeUndisplacedPosition(ref posFlatland, i_samplingData, out undisplacedPos))
                return false;

            Vector3 disp;
            if (!SampleDisplacement(ref undisplacedPos, i_samplingData, out disp))
                return false;

            o_height = posFlatland.y + disp.y;

            return true;
        }

        public bool GetSamplingData(ref Rect i_displacedSamplingArea, float i_minSpatialLength, SamplingData o_samplingData)
        {
            // We're not bothered with areas as the waves are infinite, so just store the min wavelength.
            o_samplingData._minSpatialLength = i_minSpatialLength;
            return true;
        }

        public void ReturnSamplingData(SamplingData i_data)
        {
            i_data._minSpatialLength = -1f;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_undisplacedWorldPos)
        {
            // FPI - guess should converge to location that displaces to the target position
            Vector3 guess = i_worldPos;
            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            Vector3 disp;
            for (int i = 0; i < 4 && SampleDisplacement(ref guess, i_samplingData, out disp); i++)
            {
                Vector3 error = guess + disp - i_worldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }

            o_undisplacedWorldPos = guess;
            o_undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;

            return true;
        }

        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData)
        {
            return _amplitudes == null ? AvailabilityResult.NotInitialisedYet : AvailabilityResult.DataAvailable;
        }

        // Compute normal to a surface with a parameterization - equation 14 here: http://mathworld.wolfram.com/NormalVector.html
        public bool SampleNormal(ref Vector3 i_undisplacedWorldPos, SamplingData i_samplingData, out Vector3 o_normal)
        {
            o_normal = Vector3.zero;

            if (_amplitudes == null) return false;

            var pos = new Vector2(i_undisplacedWorldPos.x, i_undisplacedWorldPos.z);
            float mytime = OceanRenderer.Instance.CurrentTime;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;
            float minWaveLength = i_samplingData._minSpatialLength / 2f;

            // base rate of change of our displacement function in x and z is unit
            var delfdelx = Vector3.right;
            var delfdelz = Vector3.forward;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;
                if (_wavelengths[j] < minWaveLength) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                var D = new Vector2(Mathf.Cos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad), Mathf.Sin((windAngle + _angleDegs[j]) * Mathf.Deg2Rad));
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float disp = k * -_spectrum._chop * Mathf.Cos(t);
                float dispx = D.x * disp;
                float dispz = D.y * disp;
                float dispy = -k * Mathf.Sin(t);

                delfdelx += _amplitudes[j] * new Vector3(D.x * dispx, D.x * dispy, D.y * dispx);
                delfdelz += _amplitudes[j] * new Vector3(D.x * dispz, D.y * dispy, D.y * dispz);
            }

            o_normal = Vector3.Cross(delfdelz, delfdelx).normalized;

            return true;
        }

        public bool SampleDisplacement(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement)
        {
            o_displacement = Vector3.zero;

            if (_amplitudes == null)
            {
                return false;
            }

            Vector2 pos = new Vector2(i_worldPos.x, i_worldPos.z);
            float mytime = OceanRenderer.Instance.CurrentTime;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;
            float minWavelength = i_samplingData._minSpatialLength / 2f;

            for (int j = 0; j < _amplitudes.Length; j++)
            {
                if (_amplitudes[j] <= 0.001f) continue;
                if (_wavelengths[j] < minWavelength) continue;

                float C = ComputeWaveSpeed(_wavelengths[j]);

                // direction
                Vector2 D = new Vector2(Mathf.Cos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad), Mathf.Sin((windAngle + _angleDegs[j]) * Mathf.Deg2Rad));
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float disp = -_spectrum._chop * Mathf.Sin(t);
                o_displacement += _amplitudes[j] * new Vector3(
                    D.x * disp,
                    Mathf.Cos(t),
                    D.y * disp
                    );
            }

            return true;
        }

        public void SampleDisplacementVel(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid)
        {
            o_displacementValid = SampleDisplacement(ref i_worldPos, i_samplingData, out o_displacement);
            o_velValid = GetSurfaceVelocity(ref i_worldPos, i_samplingData, out o_displacementVel);
        }
    }
}
