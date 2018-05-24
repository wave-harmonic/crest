// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Support script for Gerstner wave ocean shapes.
    /// Generates a number of batches of Gerstner waves.
    /// </summary>
    [RequireComponent(typeof(WaveSpectrum))]
    public class ShapeGerstnerBatched : MonoBehaviour
    {
        [Tooltip("Geometry to rasterize into wave buffers to generate waves.")]
        public Mesh _rasterMesh;
        [Tooltip("Shader to be used to render out a single Gerstner octave.")]
        public Shader _waveShader;

        public int _randomSeed = 0;

        // data for all components
        float[] _wavelengths;
        float[] _waveSpeed;
        float[] _amplitudes;
        float[] _angleDegs;
        float[] _phases;
        List<int> _mostSignificant=new List<int>();

        public float m_SignificantThresholdForCPU = 0.01f; //amplitude needed to use it for CPU water height calc

        // useful references
        WaveSpectrum _spectrum;
        Material[] _materials;
        Material _materialBigWaveTransition;
        CommandBuffer[] _renderWaveShapeCmdBufs;
        // the command buffers to transition big waves between the last 2 lods
        CommandBuffer _renderBigWavelengthsShapeCmdBuf, _renderBigWavelengthsShapeCmdBufTransition;

        // IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatch.shader, both must be updated together!
        const int BATCH_SIZE = 32;

        // scratch data used by batching code
        static float[] _wavelengthsBatch = new float[BATCH_SIZE];
        static float[] _ampsBatch = new float[BATCH_SIZE];
        static float[] _anglesBatch = new float[BATCH_SIZE];
        static float[] _phasesBatch = new float[BATCH_SIZE];

        void Start()
        {
            _spectrum = GetComponent<WaveSpectrum>();
        }

        void Update()
        {
            m_Recent.Clear();
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWavelengths(ref _wavelengths, ref _angleDegs, ref _phases);

            Random.state = randomStateBkp;

            UpdateAmplitudes();

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
            _mostSignificant.Clear();
            if (_amplitudes == null || _amplitudes.Length != _wavelengths.Length)
            {
                _amplitudes = new float[_wavelengths.Length];
            }
            if (_waveSpeed == null || _waveSpeed.Length != _wavelengths.Length)
            {
                _waveSpeed = new float[_wavelengths.Length];
            }
            for (int i = 0; i < _wavelengths.Length; i++)
            {
                _waveSpeed[i] = ComputeWaveSpeed(_wavelengths[i]);
                _amplitudes[i] = _spectrum.GetAmplitude(_wavelengths[i]);
                if (_amplitudes[i] > m_SignificantThresholdForCPU)
                {
                    _mostSignificant.Add(i);
                }
            }
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
                        _wavelengthsBatch[numInBatch] = wl;
                        _ampsBatch[numInBatch] = amp;
                        _anglesBatch[numInBatch] = Mathf.Deg2Rad * (OceanRenderer.Instance._windDirectionAngle + _angleDegs[firstComponent + i]);
                        _phasesBatch[numInBatch] = _phases[firstComponent + i];
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
                _wavelengthsBatch[numInBatch] = 0f;
            }

            // apply the data to the shape material
            material.SetFloatArray("_Wavelengths", _wavelengthsBatch);
            material.SetFloatArray("_Amplitudes", _ampsBatch);
            material.SetFloatArray("_Angles", _anglesBatch);
            material.SetFloatArray("_Phases", _phasesBatch);
            material.SetFloat("_NumInBatch", numInBatch);

            OceanRenderer.Instance.Builder._shapeWDCs[lodIdx].ApplyMaterialParams(0, new PropertyWrapperMaterial(material), false, false);

            return numInBatch;
        }

        void LateUpdateMaterials()
        {
            int componentIdx = 0;

            // seek forward to first wavelength that is big enough to render into current LODs
            float minWl = OceanRenderer.Instance.Builder._shapeWDCs[0].MaxWavelength() / 2f;
            while (_wavelengths[componentIdx] < minWl && componentIdx < _wavelengths.Length)
            {
                componentIdx++;
            }

            // slightly clunky but remove any draw-shape command buffers - these will be re-added below
            RemoveDrawShapeCommandBuffers();

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
            }

            // the last batch handles waves for the last lod, and waves that did not fit in the last lod
            int lastBatchCount = UpdateBatch(OceanRenderer.Instance.Builder.CurrentLodCount - 1, componentIdx, _wavelengths.Length, _materials[OceanRenderer.Instance.Builder.CurrentLodCount - 1]);
            UpdateBatch(OceanRenderer.Instance.Builder.CurrentLodCount - 2, componentIdx, _wavelengths.Length, _materialBigWaveTransition);

            if (lastBatchCount > 0)
            {
                // special command buffers that get added to last 2 lods, to handle smooth transitions for camera height changes
                AddDrawShapeBigWavelengthsCommandBuffer();
            }
        }

        // helper code below to manage command buffers. lods from 0 to N-2 render the gerstner waves from their lod. additionally, any waves
        // in the biggest lod, or too big for the biggest lod, are rendered into both of the last two lods N-1 and N-2, as this allows us to
        // move these waves between lods without pops when the camera changes heights and the lods need to change scale.
        void AddDrawShapeCommandBuffer(int lodIndex)
        {
            OceanRenderer.Instance.Builder._shapeCameras[lodIndex].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderWaveShapeCmdBufs[lodIndex]);
        }

        void AddDrawShapeBigWavelengthsCommandBuffer()
        {
            int lastLod = OceanRenderer.Instance.Builder.CurrentLodCount - 1;
            OceanRenderer.Instance.Builder._shapeCameras[lastLod].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBuf);
            // the second-to-last lod will transition content into it from the last lod
            OceanRenderer.Instance.Builder._shapeCameras[lastLod - 1].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBufTransition);
        }

        void RemoveDrawShapeCommandBuffers()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.Builder == null || _renderBigWavelengthsShapeCmdBuf == null || _renderBigWavelengthsShapeCmdBufTransition == null)
                return;

            for (int lod = 0; lod < OceanRenderer.Instance.Builder.CurrentLodCount; lod++)
            {
                if (lod < OceanRenderer.Instance.Builder.CurrentLodCount - 1)
                {
                    if (_renderWaveShapeCmdBufs == null || _renderWaveShapeCmdBufs[lod] == null)
                        continue;

                    OceanRenderer.Instance.Builder._shapeCameras[lod].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderWaveShapeCmdBufs[lod]);
                }

                OceanRenderer.Instance.Builder._shapeCameras[lod].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBuf);
                OceanRenderer.Instance.Builder._shapeCameras[lod].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBufTransition);
            }
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

        public Vector3 GetPositionDisplacedToPositionExpensive(ref Vector3 displacedWorldPos, float toff)
        {
            // fpi - guess should converge to location that displaces to the target position
            Vector3 guess = displacedWorldPos;
            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            for (int i = 0; i < 4; i++)
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

        public bool m_Approx = true;

        public float SinCos(float x,out float cos)
        {
//            if (!m_Approx)
            {
                cos = Mathf.Cos(x);
                return Mathf.Sin(x);
            }
//            cos = Mathf.Cos(x);
//            return Mathf.Sqrt(1 - cos * cos);


            /*
                        float xf = Mathf.Floor(x / (Mathf.PI * 2));
                        x -= xf * Mathf.PI * 2;
                        const float B = 4 / Mathf.PI;
                        const float C = -4 / (Mathf.PI * Mathf.PI);

                        float Sin= -(B * x + C * x * ((x < 0) ? -x : x));

                        cos = Mathf.Sqrt(1 - Sin * Sin);

                        return Sin;
            */
        }

        Dictionary<uint,Vector3> m_Recent=new Dictionary<uint, Vector3>();

        uint CalcHash(Vector3 _wp)
        {
            _wp *= 10;
            return (uint)((_wp.x+32000)+(_wp.z+32000)*65000);
        }
        public Vector3 GetDisplacementFast(ref Vector3 worldPos, float toff)
        {
            uint hash = CalcHash(worldPos);
            Vector3 h;
            if (m_Recent.TryGetValue(hash, out h))
            {
                return h;
            }





            if (_amplitudes == null) return Vector3.zero;

            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            float chop = OceanRenderer.Instance._chop;
            float mytime = OceanRenderer.Instance.ElapsedTime + toff;
            float windAngle = OceanRenderer.Instance._windDirectionAngle;

            Vector3 result = Vector3.zero;
            

            for (int i = 0; i < _mostSignificant.Count; i++)
            {
                int j = _mostSignificant[i];
                float C = _waveSpeed[j];//ComputeWaveSpeed(_wavelengths[j]);

                // direction

                float cosA;
                float sinA = SinCos((windAngle + _angleDegs[j]) * Mathf.Deg2Rad, out cosA);


                //Vector2 D = new Vector2(cosA, sinA);
                // wave number
                float k = 2f * Mathf.PI / _wavelengths[j];

                float x = cosA * pos.x + sinA * pos.y;//Vector2.Dot(D, pos);
                float t = k * (x + C * mytime) + _phases[j];
                float cosT=Mathf.Cos(t);
//                float sinT = SinCos(t, out cosT);

                
//                float disp = -chop * sinT;

                //result += _amplitudes[j] * new Vector3(D.x * disp,cosT,D.y * disp);
                result.y += _amplitudes[j] * cosT;
            }
            m_Recent.Add(hash,result);
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
            Profiler.BeginSample("GetHeightExpensive");
            Vector3 posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            Vector3 undisplacedPos = GetPositionDisplacedToPositionExpensive(ref posFlatland, toff);

            var ret= posFlatland.y + GetDisplacement(ref undisplacedPos, toff).y;
            Profiler.EndSample();
            return ret;
        }
        public float GetHeightFast(ref Vector3 worldPos, float toff)
        {
            Profiler.BeginSample("GetHeightFast");
            Vector3 posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;
            var ret= posFlatland.y + GetDisplacementFast(ref worldPos, toff).y;
            Profiler.EndSample();
            return ret;
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
