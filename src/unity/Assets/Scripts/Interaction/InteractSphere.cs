// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    // First experiment with object interacting with water. Assumes capsule is always touching water, does
    // not take current water height into account yet
    public class InteractSphere : MonoBehaviour
    {
        // copiedTex is public here instead of being created on the fly so that it can be hooked up to a texture asset
        // which has cpu access flags set. this didnt make a difference.
        public Texture2D _copiedTex;

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

        //Texture2D _copiedTex = null;
        float _readTime = 0f;
        private void OnGUI()
        {
            if( _copiedTex != null )
            {
                GUI.color = Color.white;
                GUI.DrawTexture( new Rect( 165f, 5f, _copiedTex.width, _copiedTex.height ), _copiedTex, ScaleMode.ScaleAndCrop, false );
                GUI.Label( new Rect( 170f, 10f, _copiedTex.width, 25f ), (_readTime * 1000f).ToString( "0.00" ) + "ms" );
            }
        }

        float ComputeIntersectionVolume()
        {
            Camera[] cams = OceanRenderer.Instance.Builder._shapeCameras;
            foreach( var cam in cams )
            {
                var wdc = cam.GetComponent<WaveDataCam>();
                Rect bounds = wdc.ShapeBounds;

                Vector2 thisPos;
                thisPos.x = transform.position.x;
                thisPos.y = transform.position.z;
                if( bounds.Contains(thisPos) )
                {
                    var src = wdc.GetComponent<PingPongRts>()._lastFrameSource;
                    if( !src )
                        continue;

                    //if( _copiedTex == null || _copiedTex.width != src.width )
                    //{
                    //    _copiedTex = new Texture2D( src.width, src.height, TextureFormat.RGBAFloat, false );
                    //}

                    RenderTexture bkp = RenderTexture.active;
                    RenderTexture.active = src;

                    float startTime = Time.realtimeSinceStartup;
                    _copiedTex.ReadPixels( new Rect( 0, 0, src.width, src.height ), 0, 0 );
                    _copiedTex.Apply();

                    RenderTexture.active = bkp;

                    // i consistently see this readTime is on the order of the frame time, so it is stalling the GPU, even
                    // when reading a rendertexture that has not been touched for many frames!
                    _readTime = Time.realtimeSinceStartup - startTime;
                    //Debug.Log( "Elapsed time: " + ((endTime - startTime) * 1000f) + "ms" );

                    break;
                }
            }
            return 0f;
        }
    }
}
