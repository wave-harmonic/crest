// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Support script for gerstner wave ocean shapes.
    /// Generates a number of batches of gerstner waves.
    /// </summary>
    public class ShapeGerstnerBatched : ShapeGerstnerBase
    {
        // useful references
        Material[] _materials;
        Renderer[] _renderers;

        // IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatch.shader, both must be updated together!
        const int BATCH_SIZE = 32;

        // scratch data used by batching code
        static float[] _wavelengthsBatch = new float[BATCH_SIZE];
        static float[] _ampsBatch = new float[BATCH_SIZE];
        static float[] _anglesBatch = new float[BATCH_SIZE];
        static float[] _phasesBatch = new float[BATCH_SIZE];

        protected override void Update()
        {
            base.Update();

            if (_materials == null || _materials.Length != OceanRenderer.Instance.Builder.CurrentLodCount
                || _renderers == null || _renderers.Length != OceanRenderer.Instance.Builder.CurrentLodCount)
            {
                InitMaterials();
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
            _renderers = new Renderer[OceanRenderer.Instance.Builder.CurrentLodCount];

            for (int i = 0; i < _materials.Length; i++)
            {
                string postfix = i < _materials.Length - 1 ? i.ToString() : "BigWavelengths";

                GameObject GO = new GameObject(string.Format("Batch {0}", postfix));
                GO.layer = i < _materials.Length - 1 ? LayerMask.NameToLayer("WaveData" + i.ToString()) : LayerMask.NameToLayer("WaveDataBigWavelengths");

                MeshFilter meshFilter = GO.AddComponent<MeshFilter>();
                meshFilter.mesh = _rasterMesh;

                GO.transform.parent = transform;
                GO.transform.localPosition = Vector3.zero;
                GO.transform.localRotation = Quaternion.identity;
                GO.transform.localScale = Vector3.one;

                _materials[i] = new Material(_waveShader);

                _renderers[i] = GO.AddComponent<MeshRenderer>();
                _renderers[i].material = _materials[i];
                _renderers[i].allowOcclusionWhenDynamic = false;
            }
        }

        private void LateUpdate()
        {
            LateUpdateMaterials();
        }

        void UpdateBatch(int lodIdx, int firstComponent, int lastComponentNonInc)
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
                _renderers[lodIdx].enabled = false;
                return;
            }


            // if we did not fill the batch, clear the rest of the data
            for (int i = numInBatch; i < BATCH_SIZE; i++)
            {
                // termination signal
                _ampsBatch[i] = 0f;

                // fill wavelengths with dummy values (to avoid div by 0s in the shader)
                _wavelengthsBatch[i] = 1f;
            }

            // apply the data to the shape material
            _renderers[lodIdx].enabled = true;
            _materials[lodIdx].SetFloatArray("_Wavelengths", _wavelengthsBatch);
            _materials[lodIdx].SetFloatArray("_Amplitudes", _ampsBatch);
            _materials[lodIdx].SetFloatArray("_Angles", _anglesBatch);
            _materials[lodIdx].SetFloatArray("_Phases", _phasesBatch);
            _materials[lodIdx].SetFloat("_NumInBatch", numInBatch);
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

            // batch together appropriate wavelengths for each lod, except the last lod, which are handled separately below
            for (int lod = 0; lod < OceanRenderer.Instance.Builder.CurrentLodCount - 1; lod++, minWl *= 2f)
            {
                int startCompIdx = componentIdx;
                while(componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < 2f * minWl)
                {
                    componentIdx++;
                }

                UpdateBatch(lod, startCompIdx, componentIdx);
            }

            // the last batch handles waves for the last lod, and waves that did not fit in the last lod
            UpdateBatch(OceanRenderer.Instance.Builder.CurrentLodCount - 1, componentIdx, _wavelengths.Length);
        }
    }
}
