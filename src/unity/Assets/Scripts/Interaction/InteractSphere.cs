// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    // First experiment with object interacting with water. Assumes capsule is always touching water, does
    // not take current water height into account yet
    public class InteractSphere : MonoBehaviour
    {
        Texture2D _copiedWaveData;

        float _texReadTime = 0f;
        float _waveDataSampleTime = 0f;

        //public Shader _shader;

        //float _lastY = 0f;

        //Material _mat;

        void Start()
        {
            //_lastY = transform.position.y;

            //_mat = new Material( _shader );

            //GetComponent<Renderer>().material = _mat;
        }

        void LateUpdate()
        {
            //float dy = _lastY - transform.position.y;

            //float a = transform.lossyScale.x / 2f;
            //float b = transform.lossyScale.z / 2f;
            //float A = Mathf.PI * a * b;

            //float V = dy * A;

            //float VperLod = V / (float)OceanRenderer.Instance._lodCount;

            //_mat.SetFloat( "_displacedVPerLod", VperLod );

            //_lastY = transform.position.y;

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
            Vector2 thisPos;
            thisPos.x = transform.position.x;
            thisPos.y = transform.position.z;

            WaveDataCam wdc = null;
            bool done = false;

            Camera[] cams = OceanRenderer.Instance.Builder._shapeCameras;
            foreach( var cam in cams )
            {
                var thisWDC = cam.GetComponent<WaveDataCam>();

                if( thisWDC.ShapeBounds.Contains(thisPos) )
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
                    _copiedWaveData.ReadPixels( new Rect( 0, 0, src.width, src.height ), 0, 0 );
                    _copiedWaveData.Apply();

                    RenderTexture.active = bkp;

                    // i consistently see this readTime is on the order of the frame time, so it is stalling the GPU, even
                    // when reading a rendertexture that has not been touched for many frames!
                    _texReadTime = Time.realtimeSinceStartup - startTime;
                    //Debug.Log( "Elapsed time: " + ((endTime - startTime) * 1000f) + "ms" );

                    done = true;
                    break;
                }
            }

            if( done )
            {
                float startTime = Time.realtimeSinceStartup;

                const int SAMPLE_COUNT = 10;
                float aveY = 0f;
                for( int i = 0; i < SAMPLE_COUNT; i++ )
                {
                    Vector2 pos2 = Random.insideUnitCircle;
                    pos2.x *= transform.lossyScale.x;
                    pos2.y *= transform.lossyScale.z;

                    Vector3 pos3 = transform.position + pos2.x * transform.right + pos2.y * transform.forward;
                    Vector2 uv;
                    wdc.WorldPosToUV( pos3, out uv );

                    Color sample = _copiedWaveData.GetPixelBilinear( uv.x, uv.y );

                    float h = sample.r + sample.b;

                    aveY += h;
                }
                aveY /= (float)SAMPLE_COUNT;

                _waveDataSampleTime = Time.realtimeSinceStartup - startTime;

                Vector3 pos = transform.position;
                pos.y = aveY;
                transform.position = pos;
            }

            return 0f;
        }
    }
}
