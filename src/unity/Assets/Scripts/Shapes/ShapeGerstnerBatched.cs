// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Support script for Gerstner wave ocean shapes.
    /// Generates a number of batches of Gerstner waves.
    /// </summary>
    public class ShapeGerstnerBatched : ShapeGerstnerBase
    {
        // useful references
        Material[] _materials;
        CommandBuffer[] _renderWaveShapeCmdBufs;
        CommandBuffer _renderBigWavelengthsShapeCmdBuf;

        // IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatch.shader, both must be updated together!
        const int BATCH_SIZE = 32;

        // scratch data used by batching code
        static float[] _wavelengthsBatch = new float[BATCH_SIZE];
        static float[] _ampsBatch = new float[BATCH_SIZE];
        static float[] _anglesBatch = new float[BATCH_SIZE];
        static float[] _phasesBatch = new float[BATCH_SIZE];

        public bool useCmdBufs = false;

        protected override void Update()
        {
            base.Update();

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
        }

        private void LateUpdate()
        {
            LateUpdateMaterials();
        }

        /// <summary>
        /// Returns number of wave components rendered in this batch.
        /// </summary>
        int UpdateBatch(int lodIdx, int firstComponent, int lastComponentNonInc)
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
            _materials[lodIdx].SetFloatArray("_Wavelengths", _wavelengthsBatch);
            _materials[lodIdx].SetFloatArray("_Amplitudes", _ampsBatch);
            _materials[lodIdx].SetFloatArray("_Angles", _anglesBatch);
            _materials[lodIdx].SetFloatArray("_Phases", _phasesBatch);
            _materials[lodIdx].SetFloat("_NumInBatch", numInBatch);

            return numInBatch;
        }

        void LateUpdateMaterials()
        {
            int componentIdx = 0;

            // seek forward to first wavelength that is big enough to render into current LODs
            float minWl = OceanRenderer.Instance.MaxWavelength(0) / 2f;
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

                if (UpdateBatch(lod, startCompIdx, componentIdx) > 0)
                {
                    // draw shape into this lod
                    AddDrawShapeCommandBuffer(lod);
                }
            }

            // the last batch handles waves for the last lod, and waves that did not fit in the last lod
            if (UpdateBatch(OceanRenderer.Instance.Builder.CurrentLodCount - 1, componentIdx, _wavelengths.Length) > 0)
            {
                // special command buffer that gets added to last 2 lods, to handle smooth transitions for camera height changes
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
            OceanRenderer.Instance.Builder._shapeCameras[lastLod - 1].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderBigWavelengthsShapeCmdBuf);
        }

        void RemoveDrawShapeCommandBuffers()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.Builder == null || _renderBigWavelengthsShapeCmdBuf == null)
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
    }
}
