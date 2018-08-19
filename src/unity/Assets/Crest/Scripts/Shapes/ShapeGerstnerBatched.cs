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
        Material _materialBigWaveTransition;
        CommandBuffer[] _renderWaveShapeCmdBufs;
        // the command buffers to transition big waves between the last 2 lods
        CommandBuffer _renderBigWavelengthsShapeCmdBuf, _renderBigWavelengthsShapeCmdBufTransition;

        // IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatch.shader, both must be updated together!
        const int BATCH_SIZE = 32;

        enum CmdBufStatus
        {
            NoStatus,
            NotAttached,
            Attached
        }

        CmdBufStatus[] _cmdBufWaveAdded = new CmdBufStatus[16];
        CmdBufStatus _cmdBufBigWavesAdded = CmdBufStatus.NoStatus;

        // scratch data used by batching code
        struct UpdateBatchScratchData
        {
            public static Vector4[] _wavelengthsBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _ampsBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _anglesBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _phasesBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _chopScalesBatch = new Vector4[BATCH_SIZE / 4];
            public static Vector4[] _gravityScalesBatch = new Vector4[BATCH_SIZE / 4];
        }

        void Start()
        {
            if( _spectrum == null )
            {
                _spectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _spectrum.name = "Default Waves (auto)";
            }
        }

        void Update()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWaveData(_componentsPerOctave, ref _wavelengths, ref _angleDegs, ref _phases);

            Random.state = randomStateBkp;

            UpdateAmplitudes();

            ReportMaxDisplacement();

            // this is done every frame for flexibility/convenience, in case the lod count changes
            if (_materials == null || _materials.Length != OceanRenderer.Instance.Builder.CurrentLodCount)
            {
                InitMaterials();
            }

            // this is done every frame for flexibility/convenience, in case the lod count changes
            if (_renderWaveShapeCmdBufs == null || _renderWaveShapeCmdBufs.Length != OceanRenderer.Instance.Builder.CurrentLodCount - 1)
            {
                InitCommandBuffers();
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
            _materials = new Material[OceanRenderer.Instance.Builder.CurrentLodCount];

            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i] = new Material(_waveShader);
            }

            _materialBigWaveTransition = new Material(_waveShader);
        }

        private void LateUpdate()
        {
            LateUpdateMaterials();
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

            // register any nonzero components
            for( int i = 0; i < numComponents; i++)
            {
                float wl = _wavelengths[firstComponent + i];
                float amp = _amplitudes[firstComponent + i];

                if( amp >= 0.001f )
                {
                    if( numInBatch < BATCH_SIZE)
                    {
                        int vi = numInBatch / 4;
                        int ei = numInBatch - vi * 4;
                        UpdateBatchScratchData._wavelengthsBatch[vi][ei] = wl;
                        UpdateBatchScratchData._ampsBatch[vi][ei] = amp;
                        UpdateBatchScratchData._anglesBatch[vi][ei] = 
                            Mathf.Deg2Rad * (OceanRenderer.Instance._windDirectionAngle + _angleDegs[firstComponent + i]);
                        UpdateBatchScratchData._phasesBatch[vi][ei] = _phases[firstComponent + i];
                        UpdateBatchScratchData._chopScalesBatch[vi][ei] = _spectrum._chopScales[(firstComponent + i) / _componentsPerOctave];
                        UpdateBatchScratchData._gravityScalesBatch[vi][ei] = _spectrum._gravityScales[(firstComponent + i) / _componentsPerOctave];
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
            if( numInBatch < BATCH_SIZE)
            {
                int vi = numInBatch / 4;
                int ei = numInBatch - vi * 4;
                UpdateBatchScratchData._wavelengthsBatch[vi][ei] = 0f;
            }

            // apply the data to the shape material
            material.SetVectorArray("_Wavelengths", UpdateBatchScratchData._wavelengthsBatch);
            material.SetVectorArray("_Amplitudes", UpdateBatchScratchData._ampsBatch);
            material.SetVectorArray("_Angles", UpdateBatchScratchData._anglesBatch);
            material.SetVectorArray("_Phases", UpdateBatchScratchData._phasesBatch);
            material.SetVectorArray("_ChopScales", UpdateBatchScratchData._chopScalesBatch);
            material.SetVectorArray("_GravityScales", UpdateBatchScratchData._gravityScalesBatch);
            material.SetFloat("_NumInBatch", numInBatch);
            material.SetFloat("_Chop", _spectrum._chop);
            material.SetFloat("_Gravity", OceanRenderer.Instance.Gravity * _spectrum._gravityScale);

            OceanRenderer.Instance.Builder._lodDataAnimWaves[lodIdx].LDSeaDepth.BindResultData(0, material, false);

            return numInBatch;
        }

        // more complicated than i would like - loops over each component and assigns to a gerstner batch which will render to a LOD.
        // the camera WL range does not always match the octave WL range (because the vertices per wave is not constrained to powers of
        // 2, unfortunately), so i cant easily just loop over octaves. also any WLs that either go to the last WDC, or dont fit in the last
        // WDC, are rendered into both the last and second-to-last WDCs, in order to transition them smoothly without pops in all scenarios.
        void LateUpdateMaterials()
        {
            int componentIdx = 0;

            // seek forward to first wavelength that is big enough to render into current LODs
            float minWl = OceanRenderer.Instance.Builder._lodDataAnimWaves[0].MaxWavelength() / 2f;
            while (_wavelengths[componentIdx] < minWl && componentIdx < _wavelengths.Length)
            {
                componentIdx++;
            }

            // batch together appropriate wavelengths for each lod, except the last lod, which are handled separately below
            for (int lod = 0; lod < OceanRenderer.Instance.Builder.CurrentLodCount - 1; lod++, minWl *= 2f)
            {
                int startCompIdx = componentIdx;
                while(componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < 2f * minWl)
                {
                    componentIdx++;
                }

                if (UpdateBatch(lod, startCompIdx, componentIdx, _materials[lod]) > 0)
                {
                    // draw shape into this lod
                    AddDrawShapeCommandBuffer(lod);
                }
                else
                {
                    RemoveDrawShapeCommandBuffer(lod);
                }
            }

            // the last batch handles waves for the last lod, and waves that did not fit in the last lod
            int lastBatchCount = UpdateBatch(OceanRenderer.Instance.Builder.CurrentLodCount - 1, componentIdx, _wavelengths.Length, _materials[OceanRenderer.Instance.Builder.CurrentLodCount - 1]);
            UpdateBatch(OceanRenderer.Instance.Builder.CurrentLodCount - 2, componentIdx, _wavelengths.Length, _materialBigWaveTransition);

            if (lastBatchCount > 0)
            {
                // special command buffers that get added to last 2 lods, to handle smooth transitions for camera height changes
                AddDrawShapeBigWavelengthsCommandBuffer();
            }
            else
            {
                RemoveDrawShapeBigWavelengthsCommandBuffer();
            }
        }

        // helper code below to manage command buffers. lods from 0 to N-2 render the gerstner waves from their lod. additionally, any waves
        // in the biggest lod, or too big for the biggest lod, are rendered into both of the last two lods N-1 and N-2, as this allows us to
        // move these waves between lods without pops when the camera changes heights and the lods need to change scale.
        void AddDrawShapeCommandBuffer(int lodIndex)
        {
            if(_cmdBufWaveAdded[lodIndex] != CmdBufStatus.Attached)
            {
                OceanRenderer.Instance.Builder._camsAnimWaves[lodIndex].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderWaveShapeCmdBufs[lodIndex]);
                _cmdBufWaveAdded[lodIndex] = CmdBufStatus.Attached;
            }
        }

        void RemoveDrawShapeCommandBuffer(int lodIndex)
        {
            if (_cmdBufWaveAdded[lodIndex] != CmdBufStatus.NotAttached)
            {
                OceanRenderer.Instance.Builder._camsAnimWaves[lodIndex].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderWaveShapeCmdBufs[lodIndex]);
                _cmdBufWaveAdded[lodIndex] = CmdBufStatus.NotAttached;
            }
        }

        void AddDrawShapeBigWavelengthsCommandBuffer()
        {
            if(_cmdBufBigWavesAdded != CmdBufStatus.Attached)
            {
                int lastLod = OceanRenderer.Instance.Builder.CurrentLodCount - 1;
                OceanRenderer.Instance.Builder._camsAnimWaves[lastLod].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBuf);
                // the second-to-last lod will transition content into it from the last lod
                OceanRenderer.Instance.Builder._camsAnimWaves[lastLod - 1].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBufTransition);

                _cmdBufBigWavesAdded = CmdBufStatus.Attached;
            }
        }

        void RemoveDrawShapeBigWavelengthsCommandBuffer()
        {
            if (_cmdBufBigWavesAdded != CmdBufStatus.NotAttached)
            {
                int lastLod = OceanRenderer.Instance.Builder.CurrentLodCount - 1;
                OceanRenderer.Instance.Builder._camsAnimWaves[lastLod].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBuf);
                // the second-to-last lod will transition content into it from the last lod
                OceanRenderer.Instance.Builder._camsAnimWaves[lastLod - 1].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBufTransition);

                _cmdBufBigWavesAdded = CmdBufStatus.NotAttached;
            }
        }

        void RemoveDrawShapeCommandBuffers()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.Builder == null || _renderBigWavelengthsShapeCmdBuf == null || _renderBigWavelengthsShapeCmdBufTransition == null)
                return;

            for (int lod = 0; lod < OceanRenderer.Instance.Builder.CurrentLodCount - 1; lod++)
            {
                RemoveDrawShapeCommandBuffer(lod);
            }

            RemoveDrawShapeBigWavelengthsCommandBuffer();
        }

        void InitCommandBuffers()
        {
            Matrix4x4 drawMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.right), Vector3.one * 100000f);

            // see the command buffer helpers below for comments about how the command buffers are arranged
            _renderWaveShapeCmdBufs = new CommandBuffer[OceanRenderer.Instance.Builder.CurrentLodCount - 1];
            for (int i = 0; i < _renderWaveShapeCmdBufs.Length; i++)
            {
                _renderWaveShapeCmdBufs[i] = new CommandBuffer();
                _renderWaveShapeCmdBufs[i].name = "ShapeGerstnerBatched" + i;
                _renderWaveShapeCmdBufs[i].DrawMesh(_rasterMesh, drawMatrix, _materials[i]);
            }

            _renderBigWavelengthsShapeCmdBuf = new CommandBuffer();
            _renderBigWavelengthsShapeCmdBuf.name = "ShapeGerstnerBatchedBigWavelengths";
            _renderBigWavelengthsShapeCmdBuf.DrawMesh(_rasterMesh, drawMatrix, _materials[OceanRenderer.Instance.Builder.CurrentLodCount - 1]);

            _renderBigWavelengthsShapeCmdBufTransition = new CommandBuffer();
            _renderBigWavelengthsShapeCmdBufTransition.name = "ShapeGerstnerBatchedBigWavelengthsTrans";
            _renderBigWavelengthsShapeCmdBufTransition.DrawMesh(_rasterMesh, drawMatrix, _materialBigWaveTransition);
        }

        // copied from unity's command buffer examples because it sounds important
        void OnEnable()
        {
            RemoveDrawShapeCommandBuffers();
        }

        void OnDisable()
        {
            RemoveDrawShapeCommandBuffers();
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

        public Vector3 GetPositionDisplacedToPosition(ref Vector3 displacedWorldPos, float toff)
        {
            // fpi - guess should converge to location that displaces to the target position
            Vector3 guess = displacedWorldPos;
            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            for (int i = 0; i < 4; i++)
            {
                Vector3 error = guess + SampleDisplacement(ref guess, toff) - displacedWorldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }

            guess.y = OceanRenderer.Instance.SeaLevel;

            return guess;
        }

        public Vector3 SampleDisplacement(ref Vector3 worldPos, float toff)
        {
            if (_amplitudes == null) return Vector3.zero;

            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            float mytime = Time.time + toff;
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
                float disp = -_spectrum._chop * Mathf.Sin(t);
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
            float mytime = Time.time + toff;
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
                float disp = k * -_spectrum._chop * Mathf.Cos(t);
                float dispx = D.x * disp;
                float dispz = D.y * disp;
                float dispy = -k * Mathf.Sin(t);

                delfdelx += _amplitudes[j] * new Vector3(D.x * dispx, D.x * dispy, D.y * dispx);
                delfdelz += _amplitudes[j] * new Vector3(D.x * dispz, D.y * dispy, D.y * dispz);
            }

            return Vector3.Cross(delfdelz, delfdelx).normalized;
        }

        public float SampleHeight(ref Vector3 worldPos, float toff)
        {
            Vector3 posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            Vector3 undisplacedPos = GetPositionDisplacedToPosition(ref posFlatland, toff);

            return posFlatland.y + SampleDisplacement(ref undisplacedPos, toff).y;
        }

        public Vector3 GetSurfaceVelocity(ref Vector3 worldPos, float toff)
        {
            if (_amplitudes == null) return Vector3.zero;

            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            float mytime = Time.time + toff;
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
                float disp = -_spectrum._chop * k * C * Mathf.Cos(t);
                result += _amplitudes[j] * new Vector3(
                    D.x * disp,
                    -k * C * Mathf.Sin(t),
                    D.y * disp
                    );
            }

            return result;
        }

        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            displacement = SampleDisplacement(ref worldPos, 0f);
            return true;
        }

        public bool SampleHeight(ref Vector3 worldPos, ref float height)
        {
            height = SampleHeight(ref worldPos, 0f);
            return true;
        }

        public void PrewarmForSamplingArea(Rect areaXZ)
        {
            // nothing to do here
        }
        public void PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            // nothing to do here
            OnDisable();

        }
        public bool SampleDisplacementInArea(ref Vector3 worldPos, ref Vector3 displacement)
        {
            // revert to usual function
            return SampleDisplacement(ref worldPos, ref displacement);
        }
        public bool SampleHeightInArea(ref Vector3 worldPos, ref float height)
        {
            // revert to usual function
            return SampleHeight(ref worldPos, ref height);
        }

        public bool SampleNormal(ref Vector3 undisplacedWorldPos, ref Vector3 normal)
        {
            normal = GetNormal(ref undisplacedWorldPos, 0f);
            return true;
        }
        public bool SampleNormal(ref Vector3 undisplacedWorldPos, ref Vector3 normal, float minSpatialLength)
        {
            Debug.LogWarning("ShapeGerstnerBatched: minSpatialLength not implemented for GetNormal(), this parameter will be ignored.", this);
            normal = GetNormal(ref undisplacedWorldPos, 0f);
            return true;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 worldPos, ref Vector3 undisplacedWorldPos)
        {
            // fpi - guess should converge to location that displaces to the target position
            Vector3 guess = worldPos;
            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            Vector3 disp = Vector3.zero;
            for (int i = 0; i < 4 && SampleDisplacement(ref guess, ref disp); i++)
            {
                Vector3 error = guess + disp - worldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }

            undisplacedWorldPos = guess;
            undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;

            return true;
        }
    }
}
