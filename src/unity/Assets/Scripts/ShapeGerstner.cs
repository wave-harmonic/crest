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
        [Tooltip( "Variance of flow direction, in degrees" )]
        public float _waveDirectionVariance = 45f;
        [Tooltip( "Wind speed in m/s" ), Range( 0, 20 )]
        public float _windSpeed = 5f;
        [Tooltip( "Choppiness of waves. Treat carefully: If set too high, can cause the geometry to overlap itself." ), Range( 0f, 1f )]
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

        IWaveSpectrum _spectrum;

        interface IWaveSpectrum
        {
            void Init( float winWL, float maxWL );
            float Cdf( float p );
            float Energy( float windSpeed, Vector2 windDir, float gravity, float smallestWavelength, float wavelength, float angle );
        }

        class StatisticalModelSpectrum : IWaveSpectrum
        {
            public float _distribution = 2f;

            float _minWavelength = 1f;
            float _maxWavelength = 64f;

            public void Init( float minWL, float maxWL )
            {
                _minWavelength = minWL;
                _maxWavelength = maxWL;
            }

            public float Cdf( float p )
            {
                float wavelength = Mathf.Pow( Random.value, _distribution );
                return Mathf.Lerp( _minWavelength, _maxWavelength, wavelength );
            }

            public float Energy( float windSpeed, Vector2 windDir, float gravity, float smallestWavelength, float wavelength, float angle )
            {
                return PhillipsSpectrum( windSpeed, windDir, Mathf.Abs( Physics.gravity.y ), _minWavelength, wavelength, angle );
                //return PhilSpectrum( Mathf.Abs( Physics.gravity.y ), _wavelengths[i] );
                //return PiersonMoskowitzSpectrum( Mathf.Abs( Physics.gravity.y ), _windSpeed, _wavelengths[i] );
                //return JONSWAPSpectrum( Mathf.Abs( Physics.gravity.y ), _windSpeed, _wavelengths[i] );
            }

            static float PhillipsSpectrum( float windSpeed, Vector2 windDir, float gravity, float smallestWavelength, float wavelength, float angle )
            {
                float wavenumber = 2f * Mathf.PI / wavelength;
                float angle_radians = Mathf.PI * angle / 180f;
                float kx = Mathf.Cos( angle_radians ) * wavenumber;
                float kz = Mathf.Sin( angle_radians ) * wavenumber;

                float k2 = kx * kx + kz * kz;

                float windSpeed2 = windSpeed * windSpeed;
                float wx = windDir.x;
                float wz = windDir.y;

                float kdotw = (wx * kx + wz * kz);

                float a = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
                float L = windSpeed2 / gravity;

                // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.161.9102&rep=rep1&type=pdf
                return a * kdotw * kdotw * Mathf.Exp( -1f / (k2 * L * L) ) / (k2 * k2);
            }


            // base of modern parametric wave spectrum
            static float PhilSpectrum( float gravity, float wavelength )
            {
                float alpha = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
                return PhilSpectrum( gravity, alpha, wavelength );
            }
            // base of modern parametric wave spectrum
            static float PhilSpectrum( float gravity, float alpha, float wavelength )
            {
                //float alpha = 0.0081f; // phillips constant ( https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf )
                float wavenumber = 2f * Mathf.PI / wavelength;
                float frequency = Mathf.Sqrt( gravity * wavenumber ); // deep water - depth > wavelength/2
                return alpha * gravity * gravity / Mathf.Pow( frequency, 5f );
            }

            static float PiersonMoskowitzSpectrum( float gravity, float windspeed, float wavelength )
            {
                float wavenumber = 2f * Mathf.PI / wavelength;
                float frequency = Mathf.Sqrt( gravity * wavenumber ); // deep water - depth > wavelength/2
                float frequency_peak = 0.855f * gravity / windspeed;
                return PhilSpectrum( gravity, wavelength ) * Mathf.Exp( -Mathf.Pow( frequency_peak / frequency, 4f ) * 5f / 4f );
            }
            static float PiersonMoskowitzSpectrum( float gravity, float windspeed, float frequency_peak, float alpha, float wavelength )
            {
                float wavenumber = 2f * Mathf.PI / wavelength;
                float frequency = Mathf.Sqrt( gravity * wavenumber ); // deep water - depth > wavelength/2
                return PhilSpectrum( gravity, alpha, wavelength ) * Mathf.Exp( -Mathf.Pow( frequency_peak / frequency, 4f ) * 5f / 4f );
            }

            static float JONSWAPSpectrum( float gravity, float windspeed, float wavelength )
            {
                // fetch distance
                float F = 1000f;
                float alpha = 0.076f * Mathf.Pow( windspeed * windspeed / (F * gravity), 0.22f );

                float wavenumber = 2f * Mathf.PI / wavelength;
                float frequency = Mathf.Sqrt( gravity * wavenumber ); // deep water - depth > wavelength/2
                float frequency_peak = 22f * Mathf.Pow( gravity * gravity / (windspeed * F), 1f / 3f );
                float sigma = frequency <= frequency_peak ? 0.07f : 0.09f;
                float r = Mathf.Exp( -Mathf.Pow( frequency - frequency_peak, 2f ) / (2f * sigma * sigma * frequency_peak * frequency_peak) );
                float gamma = 3.3f;

                return PiersonMoskowitzSpectrum( gravity, windspeed, frequency_peak, alpha, wavelength ) * Mathf.Pow( gamma, r );

            }
        }

        [ContextMenu("Reset Spectra")]
        void ResetSpectra()
        {
            _waveSpec._customSpectrum = new AnimationCurve( new Keyframe[] {
                new Keyframe(-2.0f, 0f),
                new Keyframe(-1.5f, 0f),
                new Keyframe(-1.0f, 0f),
                new Keyframe(-0.5f, 0f),
                new Keyframe( 0.0f, 0f),
                new Keyframe( 0.5f, 0f),
                new Keyframe( 1.0f, 0f),
                new Keyframe( 1.5f, 0f),
                new Keyframe( 2.0f, 0f),
                new Keyframe( 2.5f, 0f),
                new Keyframe( 3.0f, 0f),
                new Keyframe( 3.5f, 0f),
                new Keyframe( 4.0f, 0f),
                new Keyframe( 4.5f, 0f),
                new Keyframe( 5.0f, 0f),
                new Keyframe( 5.5f, 0f),
                new Keyframe( 6.0f, 0f),
                new Keyframe( 6.5f, 0f),
                new Keyframe( 7.0f, 0f),
                new Keyframe( 7.5f, 0f),
                new Keyframe( 8.0f, 0f),
                new Keyframe( 8.5f, 0f),
            } );
        }

        [System.Serializable]
        public class AuthoredWaveSpectrum : IWaveSpectrum
        {
            public float _energyScale = 5f;
            public AnimationCurve _customSpectrum = new AnimationCurve( new Keyframe[] {
                new Keyframe(-2.0f, 0f),
                new Keyframe(-1.5f, 0f),
                new Keyframe(-1.0f, 0f),
                new Keyframe(-0.5f, 0f),
                new Keyframe( 0.0f, 0f),
                new Keyframe( 0.5f, 0f),
                new Keyframe( 1.0f, 0f),
                new Keyframe( 1.5f, 0f),
                new Keyframe( 2.0f, 0f),
                new Keyframe( 2.5f, 0f),
                new Keyframe( 3.0f, 0f),
                new Keyframe( 3.5f, 0f),
                new Keyframe( 4.0f, 0f),
                new Keyframe( 4.5f, 0f),
                new Keyframe( 5.0f, 0f),
                new Keyframe( 5.5f, 0f),
                new Keyframe( 6.0f, 0f),
                new Keyframe( 6.5f, 0f),
                new Keyframe( 7.0f, 0f),
                new Keyframe( 7.5f, 0f),
                new Keyframe( 8.0f, 0f),
                new Keyframe( 8.5f, 0f),
            } );

            float _minWavelength = 1f;
            float _maxWavelength = 64f;

            float Pdf(float wl )
            {
                float l2 = Mathf.Log( wl ) / Mathf.Log( 2f );
                return _customSpectrum.Evaluate( l2 );
            }

            public float Cdf( float p )
            {
                if( p <= 0f || p >= 1f )
                    return Mathf.Clamp01( p );

                for( int i = 0; i < _cdfTable.Length - 1; i++ )
                {
                    if( p <= _cdfTable[i+1].CDF )
                    {
                        float s = Mathf.InverseLerp( _cdfTable[i].CDF, _cdfTable[i + 1].CDF, p );
                        return Mathf.Lerp( _cdfTable[i].wl, _cdfTable[i + 1].wl, s );
                    }
                }

                return 1f;
            }

            public float Energy( float windSpeed, Vector2 windDir, float gravity, float smallestWavelength, float wavelength, float angle )
            {
                return _energyScale * Pdf( wavelength );
            }

            public void Init( float minWL, float maxWL )
            {
                _minWavelength = minWL;
                _maxWavelength = maxWL;

                ComputeArea();
            }

            struct CdfTableEntry
            {
                public float wl;
                public float CDF;
            }
            CdfTableEntry[] _cdfTable = new CdfTableEntry[32];

            void ComputeArea()
            {
                // integrate in exponential space, because curve is defined on logarithmic axis
                float exp0 = Mathf.Log( _minWavelength );
                float exp1 = Mathf.Log( _maxWavelength );

                // compute table - also exponentially distributed
                int stepsi = _cdfTable.Length;

                _cdfTable[0].wl = _minWavelength;
                _cdfTable[0].CDF = 0f;
                for( int i = 1; i < stepsi; i++ )
                {
                    float a = i / (stepsi - 1f);
                    float wl1 = Mathf.Exp( Mathf.Lerp( exp0, exp1, a ) );
                    _cdfTable[i].wl = wl1;
                    _cdfTable[i].CDF = _cdfTable[i - 1].CDF + Pdf( _cdfTable[i].wl );
                }

                for( int i = 0; i < _cdfTable.Length; i++ )
                {
                    _cdfTable[i].CDF /= _cdfTable[_cdfTable.Length - 1].CDF;
                }
            }
        }

        public AuthoredWaveSpectrum _waveSpec;

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

            _spectrum = _waveSpec;
            //_spectrum = new StatisticalModelSpectrum( _minWavelength, maxWavelength );
            _spectrum.Init( _minWavelength, maxWavelength );

            for( int i = 0; i < _numOctaves; i++ )
            {
                _wavelengths[i] = _spectrum.Cdf( Random.value );
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
                float energy = _spectrum.Energy( _windSpeed, windDir, Mathf.Abs( Physics.gravity.y ), _minWavelength, _wavelengths[i], _angleDegs[i] );

                // energy to amplitude - eqn 19 - https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf
                float amp = Mathf.Sqrt( 2f * energy );

                _materials[i].SetFloat( "_Amplitude", amp );
            }
        }

        static ShapeGerstner _instance;
        public static ShapeGerstner Instance { get { return _instance ?? (_instance = FindObjectOfType<ShapeGerstner>()); } }

        Vector2 WindDir { get { return new Vector2( Mathf.Cos(Mathf.PI* _windDirectionAngle / 180f ), Mathf.Sin(Mathf.PI* _windDirectionAngle / 180f ) ); } }
    }
}
