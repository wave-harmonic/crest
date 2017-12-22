// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    // First experiment with object interacting with water. Assumes capsule is always touching water, does
    // not take current water height into account yet
    public class InteractSphere : MonoBehaviour
    {
        public float _dragLinSubmerged = 0.6f;
        public float _dragRotSubmerged = 1.8f;
        public float _dragLinEmerged = 0.2f;
        public float _dragRotEmerged = 0.2f;

        [Range( 0.01f, 1f )]
        public float _densityComparedToWater = 1f;

        [Tooltip( "The max number of wavelengths crossing the extent of the shape which will be used for physics." ), Range( 1f, 10f )]
        public float _maxWaves = 3f;

        Texture2D _copiedWaveData;

        float _texReadTime = 0f;
        float _waveDataSampleTime = 0f;

        public Shader _displaceShader;
        float _displacedVLast = -1f;

        public Renderer _dispProxy;

        Material _mat;
        Rigidbody _rigidbody;

        void Start()
        {
            _mat = new Material( _displaceShader );
            _dispProxy.material = _mat;

            _rigidbody = GetComponent<Rigidbody>();
        }

        void LateUpdate()
        {
            ComputeIntersectionVolume();
        }

        private void OnGUI()
        {
            if( _copiedWaveData != null )
            {
                GUI.color = Color.white;
                GUI.DrawTexture( new Rect( 165f, 5f, _copiedWaveData.width, _copiedWaveData.height ), _copiedWaveData, ScaleMode.ScaleAndCrop, false );
                GUI.Label( new Rect( 170f, 10f, _copiedWaveData.width, 25f ), "Download: " + (_texReadTime * 1000f).ToString( "0.00" ) + "ms" );
                GUI.Label( new Rect( 170f, 35f, _copiedWaveData.width, 25f ), "Sample: " + (_waveDataSampleTime * 1000f).ToString( "0.00" ) + "ms" );
            }
        }

        float ComputeIntersectionVolume()
        {
            // uniform scale for now
            float sphereRad = transform.lossyScale.x;
            float sphereVol = (4 / 3f) * Mathf.PI * sphereRad * sphereRad * sphereRad;
            _rigidbody.mass = _densityComparedToWater * sphereVol * OceanRenderer.WATER_DENSITY;

            Vector2 thisPos;
            thisPos.x = transform.position.x;
            thisPos.y = transform.position.z;

            WaveDataCam wdc = null;
            bool done = false;

            // no interested if there are less than x wavelengths crossing this object - filter them out.
            float minL = 2f * sphereRad / _maxWaves;

            Camera[] cams = OceanRenderer.Instance.Builder._shapeCameras;
            foreach( var cam in cams )
            {
                var thisWDC = cam.GetComponent<WaveDataCam>();

                if( thisWDC.ShapeBounds.Contains(thisPos) && thisWDC.MaxWavelength > minL )
                {
                    wdc = thisWDC;

                    var src = wdc.GetComponent<PingPongRts>()._sourceThisFrame;
                    if( !src ) continue;

                    if( _copiedWaveData == null || _copiedWaveData.width != src.width )
                    {
                        _copiedWaveData = new Texture2D( src.width, src.height, TextureFormat.RGBAFloat, false );
                    }

                    RenderTexture bkp = RenderTexture.active;
                    RenderTexture.active = src;

                    float startTime = Time.realtimeSinceStartup;

                    if( ShapeWaveSim._captureShape )
                    {
                        _copiedWaveData.ReadPixels( new Rect( 0, 0, src.width, src.height ), 0, 0 );
                        _copiedWaveData.Apply();
                    }

                    RenderTexture.active = bkp;

                    // i consistently see this readTime is on the order of the frame time, so it is stalling the GPU, even
                    // when reading a rendertexture that has not been touched for many frames!
                    _texReadTime = Time.realtimeSinceStartup - startTime;

                    done = true;
                    break;
                }
            }

            if( done )
            {
                float startTime = Time.realtimeSinceStartup;

                const int SAMPLE_COUNT = 10;

                float A = Mathf.PI * sphereRad * sphereRad;

                float displacedV = 0f;

                for( int i = 0; i < SAMPLE_COUNT; i++ )
                {
                    float r = Mathf.Sqrt( Random.value );
                    // r^2 + y^2 = 1^2
                    float y = Mathf.Sqrt( 1f - r * r );
                    r *= sphereRad;
                    y *= sphereRad;

                    float theta = 2f * Mathf.PI * Random.value;

                    Vector3 bottomOffset = new Vector3( r * Mathf.Cos( theta ), -y, r * Mathf.Sin( theta ) );

                    Vector3 sampleBottomPos = transform.position + bottomOffset;

                    Vector2 uv;
                    wdc.WorldPosToUV( sampleBottomPos, out uv );

                    Color sample = _copiedWaveData.GetPixelBilinear( uv.x, uv.y );

                    float oceany = sample.r + sample.b;

                    // bottom of water column that intersects sphere
                    float y0 = sampleBottomPos.y;
                    // top of water column that intersects sphere
                    float y1 = Mathf.Min( oceany, y0 + 2f * Mathf.Abs( bottomOffset.y ) );

                    if( y1 <= y0 )
                        continue; // fully emerged

                    // apply buoyancy - weight of displaced fluid
                    float a = A / (float)SAMPLE_COUNT;
                    float v = a * (y1 - y0);

                    displacedV += v;

                    Vector3 centroid = sampleBottomPos + Vector3.up * (y1 - y0) / 2f;
                    _rigidbody.AddForceAtPosition( -Physics.gravity * OceanRenderer.WATER_DENSITY * v, centroid );
                }

                _rigidbody.drag = Mathf.Lerp( _dragLinEmerged, _dragLinSubmerged, displacedV / sphereVol );
                _rigidbody.angularDrag = Mathf.Lerp( _dragRotEmerged, _dragRotSubmerged, displacedV / sphereVol );

                if( _displacedVLast != -1f )
                {
                    float vDiff = displacedV - _displacedVLast;
                    _mat.SetFloat( "_displacedVPerLod", 0.05f * vDiff / OceanRenderer.Instance._lodCount );
                }

                _displacedVLast = displacedV;

                _waveDataSampleTime = Time.realtimeSinceStartup - startTime;
            }

            return 0f;
        }
    }
}
