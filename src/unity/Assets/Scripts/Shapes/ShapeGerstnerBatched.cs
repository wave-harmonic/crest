// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Support script for gerstner wave ocean shapes.
    /// Generates a number of batches of gerstner waves.
    /// </summary>
    public class ShapeGerstnerBatched : MonoBehaviour
    {
        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        [Tooltip("Wind speed in m/s"), Range(0, 20), HideInInspector]
        public float _windSpeed = 5f;

        [Tooltip("Geometry to rasterize into wave buffers to generate waves.")]
        public Mesh _rasterMesh;
        [Tooltip("Shader to be used to render out a single Gerstner octave.")]
        public Shader _waveShader;

        public int _randomSeed = 0;

        // useful references
        Material[] _materials;
        Renderer[] _renderers;
        WaveSpectrum _spectrum;

        // data for all components
        float[] _wavelengths;
        float[] _angleDegs;
        float[] _phases;

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

        private void Update()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWavelengths(ref _wavelengths, ref _angleDegs, ref _phases);

            if (_materials == null || _materials.Length != OceanRenderer.Instance._lodCount + 1
                || _renderers == null || _renderers.Length != OceanRenderer.Instance._lodCount + 1)
            {
                InitMaterials();
            }

            Random.state = randomStateBkp;
        }

        private void LateUpdate()
        {
            LateUpdateMaterials();
        }

        void InitMaterials()
        {
            foreach (var child in transform)
            {
                Destroy((child as Transform).gameObject);
            }

            // num octaves plus one, because there is an additional last bucket for large wavelengths
            _materials = new Material[OceanRenderer.Instance._lodCount + 1];
            _renderers = new Renderer[OceanRenderer.Instance._lodCount + 1];

            for (int i = 0; i < _materials.Length; i++)
            {
                string postfix = i < _materials.Length - 1 ? i.ToString() : "BigWavelengths";

                GameObject GO = new GameObject(string.Format("Octave {0}", postfix));
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

        void UpdateBatch(int lodIdx, int firstComponent, int numComponents)
        {
            int numInBatch = 0;

            // register any nonzero components
            for( int i = 0; i < numComponents; i++)
            {
                float wl = _wavelengths[firstComponent + i];
                float pow = _spectrum.GetPower(wl);
                float period = wl / ComputeWaveSpeed(wl);
                float amp = Mathf.Sqrt(pow / period);

                if( amp >= 0.001f )
                {
                    _wavelengthsBatch[numInBatch] = wl;
                    _ampsBatch[numInBatch] = amp;
                    _anglesBatch[numInBatch] = Mathf.Deg2Rad * (_windDirectionAngle + _angleDegs[firstComponent + i]);
                    _phasesBatch[numInBatch] = _phases[firstComponent + i];
                    numInBatch++;
                }
            }

            if(numInBatch == 0)
            {
                // no waves to draw - abort
                _renderers[lodIdx].enabled = false;
                return;
            }

            // if we didnt fill the batch, put a terminator signal after the last position
            if( numInBatch < BATCH_SIZE)
            {
                _wavelengthsBatch[numInBatch] = 0f;
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

            // seek forward to first wavelength that is big enough to render into current lods
            float minWl = OceanRenderer.Instance.MaxWavelength(0) / 2f;
            while (_wavelengths[componentIdx] < minWl && componentIdx < _wavelengths.Length)
            {
                componentIdx++;
            }

            int lod;
            for (lod = 0; lod < OceanRenderer.Instance._lodCount; lod++, minWl *= 2f)
            {
                int startCompIdx = componentIdx;
                while(componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < 2f * minWl)
                {
                    componentIdx++;
                }

                UpdateBatch(lod, startCompIdx, componentIdx - startCompIdx);
            }

            // last batch for waves that did not fit neatly into the lods
            UpdateBatch(lod, componentIdx, _wavelengths.Length - componentIdx);
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

        static ShapeGerstner _instance;
        public static ShapeGerstner Instance { get { return _instance ?? (_instance = FindObjectOfType<ShapeGerstner>()); } }

        public Vector2 WindDir { get { return new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f)); } }
    }
}
