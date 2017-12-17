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
        [Tooltip("The number of wave octaves")]
        public int _numOctaves = 32;
        [Tooltip("Distribution of wavelengths, > 1 means concentrated at low wavelengths")]
        public float _wavelengthDistribution = 4f;
        [Tooltip( "Wind direction (angle from x axis in degrees)" ), Range( -180, 180 )]
        public float _windDirectionAngle = 0f;
        [Tooltip( "Variance of flow direction, in degrees" )]
        public float _waveDirectionVariance = 45f;
        [Tooltip( "Wind speed in m/s" ), Range( 0, 20 )]
        public float _windSpeed = 5f;
        [Tooltip( "Choppiness of waves. Treat carefully: If set too high, can cause the geometry to overlap itself." ), Range( 0, 5 )]
        public float _choppiness = 0f;

        [Tooltip( "Geometry to rasterise into wave buffers to generate waves." )]
        public Mesh _rasterMesh;
        [Tooltip( "Shader to be used to render out a single Gerstner octave." )]
        public Shader _waveShader;

        public int _randomSeed = 0;

        float _minWavelength;
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
                // Direction
                _angleDegs[i] = _windDirectionAngle + Random.Range( -_waveDirectionVariance, _waveDirectionVariance );
                if( _angleDegs[i] > 180f )
                {
                    _angleDegs[i] -= 360f;
                }
                if( _angleDegs[i] < -180f )
                {
                    _angleDegs[i] += 360f;
                }

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

                _materials[i].SetFloat("_Angle", _angleDegs[i] );

                // Wavelength
                _materials[i].SetFloat( "_Wavelength", _wavelengths[i] );
            }

            Random.state = randomStateBkp;
        }

        private void Update()
        {
            Shader.SetGlobalFloat( "_Choppiness", _choppiness );

            UpdateAmplitudes();
        }

        void UpdateAmplitudes()
        {
            Vector2 windDir = WindDir;

            for( int i = 0; i < _numOctaves; i++ )
            {
                float energy = PhillipsSpectrum( _windSpeed, windDir, Mathf.Abs( Physics.gravity.y ), _minWavelength, _wavelengths[i], _angleDegs[i] );

                // energy to amplitude ( http://www.physicsclassroom.com/class/waves/Lesson-2/Energy-Transport-and-the-Amplitude-of-a-Wave )
                float amp = Mathf.Sqrt( energy );

                _materials[i].SetFloat( "_Amplitude", amp );
            }
        }

        static ShapeGerstner _instance;
        public static ShapeGerstner Instance { get { return _instance ?? (_instance = FindObjectOfType<ShapeGerstner>()); } }

        Vector2 WindDir { get { return new Vector2( Mathf.Cos(Mathf.PI* _windDirectionAngle / 180f ), Mathf.Sin(Mathf.PI* _windDirectionAngle / 180f ) ); } }

        static float PhillipsSpectrum( float windSpeed, Vector2 windDir, float gravity, float smallestWavelength, float wavelength, float angle )
        {
            float wavenumber = 2f * Mathf.PI / wavelength;
            float angle_radians = Mathf.PI * angle / 180f;
            float kx = Mathf.Cos( angle_radians ) * wavenumber;
            float kz = Mathf.Sin( angle_radians ) * wavenumber;

            float k2 = kx * kx + kz * kz;

            if( k2 < 0.0001f ) return 0f;

            float windSpeed2 = windSpeed * windSpeed;
            float wx = windDir.x;
            float wz = windDir.y;

            float kdotw = (wx * kx + wz * kz);

            float a = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
            float L = windSpeed2 / gravity;

            // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.161.9102&rep=rep1&type=pdf
            return a * kdotw * kdotw * Mathf.Exp( -1f / (k2 * L * L) ) / (k2 * k2);
        }
    }
}
