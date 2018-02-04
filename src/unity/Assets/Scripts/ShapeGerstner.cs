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
        [Tooltip( "The number of wave octaves" )]
        public int _numOctaves = 32;
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
        float[] _wavelengths;

        Material[] _materials;
        float[] _angleDegs;

        void Start()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState( _randomSeed );

            Vector2 windDir = WindDir;

            _angleDegs = new float[_numOctaves];
            _materials = new Material[_numOctaves];
            _wavelengths = new float[_numOctaves];

            // derive the range of wavelengths from the LOD settings, the base ocean density, the min and max scales. the wavelength
            // range always fills the dynamic range of the multiscale sim.
            float minDiameter = 4f * OceanRenderer.Instance._minScale;
            float minTexelSize = minDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            _minWavelength = minTexelSize * OceanRenderer.Instance._minTexelsPerWave;
            float maxDiameter = 4f * OceanRenderer.Instance._maxScale * Mathf.Pow( 2f, OceanRenderer.Instance._lodCount - 1 );
            float maxTexelSize = maxDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            float maxWavelength = 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;

            for( int i = 0; i < _numOctaves; i++ )
            {
                float wavelengthSel = Mathf.Pow( Random.value, _wavelengthDistribution );
                _wavelengths[i] = Mathf.Lerp( _minWavelength, maxWavelength, wavelengthSel );
            }
            System.Array.Sort( _wavelengths );

            // Generate the given number of octaves, each generating a GameObject rendering a quad.
            for (int i = 0; i < _numOctaves; i++)
            {
                GameObject GO = new GameObject( string.Format( "Wavelength {0}", _wavelengths[i].ToString("0.000") ) );
                GO.layer = gameObject.layer;

                MeshFilter meshFilter = GO.AddComponent<MeshFilter>();
                meshFilter.mesh = _rasterMesh;

                GO.transform.parent = transform;
                GO.transform.localPosition = Vector3.zero;
                GO.transform.localRotation = Quaternion.identity;
                GO.transform.localScale = Vector3.one;

                _materials[i] = new Material( _waveShader );

                MeshRenderer renderer = GO.AddComponent<MeshRenderer>();
                renderer.material = _materials[i];

                // Wavelength
                _materials[i].SetFloat( "_Wavelength", _wavelengths[i] );
            }

            Random.state = randomStateBkp;
        }

        private void Update()
        {
            UpdateAmplitudes(GetComponent<WaveSpectrum>());
        }

        void UpdateAmplitudes(WaveSpectrum spec)
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            for (int i = 0; i < _numOctaves; i++)
            {
                float wlCount = 1f;
                float lowerWavelength = Mathf.Pow(2f, Mathf.Floor(Mathf.Log(_wavelengths[i]) / Mathf.Log(2f)));
                for (int j = i - 1; j >= 0; j--)
                {
                    if (_wavelengths[j] < lowerWavelength)
                        break;

                    wlCount += 1f;
                }
                float upperWavelength = 2f * lowerWavelength;
                for (int j = i + 1; j < _numOctaves; j++)
                {
                    if (_wavelengths[j] >= upperWavelength)
                        break;

                    wlCount += 1f;
                }

                float pow = spec.GetPower(_wavelengths[i]) / wlCount;
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
            }

            Random.state = randomStateBkp;
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
