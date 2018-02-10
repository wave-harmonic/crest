// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Support script for gerstner wave ocean shapes.
    /// Generates a number of gerstner octaves in child gameobjects.
    /// </summary>
    public class ShapeGerstner : MonoBehaviour
    {
        [Tooltip( "Distribution of wavelengths, > 1 means concentrated at low wavelengths" )]
        public float _wavelengthDistribution = 4f;
        [Tooltip( "Wind direction (angle from x axis in degrees)" ), Range( -180, 180 )]
        public float _windDirectionAngle = 0f;
        [Tooltip("Variance of flow direction, in degrees"), Range(0f, 180f)]
        public float _waveDirectionVariance = 45f; 
        [Tooltip( "Wind speed in m/s" ), Range( 0, 20 ), HideInInspector]
        public float _windSpeed = 5f;
        [Tooltip( "Choppiness of waves. Treat carefully: If set too high, can cause the geometry to overlap itself." ), Range( 0f, 1f )]
        public float _choppiness = 0f;

        [Tooltip( "Geometry to rasterise into wave buffers to generate waves." )]
        public Mesh _rasterMesh;
        [Tooltip( "Shader to be used to render out a single Gerstner octave." )]
        public Shader _waveShader;

        public int _randomSeed = 0;

        public float _minWavelength;

        Material[] _materials;

        float[] _wavelengths;
        float[] _angleDegs;
        float[] _phases;

        WaveSpectrum _spectrum;

        void Start()
        {
            _spectrum = GetComponent<WaveSpectrum>();
        }

        void InitMaterials()
        {
            foreach( var child in transform )
            {
                Destroy((child as Transform).gameObject);
            }

            _materials = new Material[_wavelengths.Length];

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                GameObject GO = new GameObject(string.Format("Wavelength {0}", _wavelengths[i].ToString("0.000")));
                GO.layer = gameObject.layer;

                MeshFilter meshFilter = GO.AddComponent<MeshFilter>();
                meshFilter.mesh = _rasterMesh;

                GO.transform.parent = transform;
                GO.transform.localPosition = Vector3.zero;
                GO.transform.localRotation = Quaternion.identity;
                GO.transform.localScale = Vector3.one;

                _materials[i] = new Material(_waveShader);

                MeshRenderer renderer = GO.AddComponent<MeshRenderer>();
                renderer.material = _materials[i];
            }
        }

        private void Update()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWavelengths(ref _wavelengths, ref _angleDegs, ref _phases);

            if (_materials == null || _materials.Length != _wavelengths.Length)
            {
                InitMaterials();
            }

            UpdateMaterials();

            Random.state = randomStateBkp;
        }

        void UpdateMaterials()
        {
            for (int i = 0; i < _wavelengths.Length; i++)
            {
                // Wavelength
                _materials[i].SetFloat("_Wavelength", _wavelengths[i]);

                float wlCount = 1f;
                float lowerWavelength = Mathf.Pow(2f, Mathf.Floor(Mathf.Log(_wavelengths[i]) / Mathf.Log(2f)));
                for (int j = i - 1; j >= 0; j--)
                {
                    if (_wavelengths[j] < lowerWavelength)
                        break;

                    wlCount += 1f;
                }
                float upperWavelength = 2f * lowerWavelength;
                for (int j = i + 1; j < _wavelengths.Length; j++)
                {
                    if (_wavelengths[j] >= upperWavelength)
                        break;

                    wlCount += 1f;
                }

                float pow = _spectrum.GetPower(_wavelengths[i]) / wlCount;
                float period = _wavelengths[i] / ComputeWaveSpeed(_wavelengths[i]);
                float amp = Mathf.Sqrt(pow / period);
                _materials[i].SetFloat("_Amplitude", amp);

                // Direction
                _angleDegs[i] = _windDirectionAngle + Random.Range(-_waveDirectionVariance, _waveDirectionVariance);
                if (_angleDegs[i] > 180f)
                {
                    _angleDegs[i] -= 360f;
                }
                if (_angleDegs[i] < -180f)
                {
                    _angleDegs[i] += 360f;
                }
                _materials[i].SetFloat("_Angle", _angleDegs[i]);

                _materials[i].SetFloat("_Phase", _phases[i]);
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

        static ShapeGerstner _instance;
        public static ShapeGerstner Instance { get { return _instance ?? (_instance = FindObjectOfType<ShapeGerstner>()); } }

        public Vector2 WindDir { get { return new Vector2( Mathf.Cos(Mathf.PI* _windDirectionAngle / 180f ), Mathf.Sin(Mathf.PI* _windDirectionAngle / 180f ) ); } }
    }
}
