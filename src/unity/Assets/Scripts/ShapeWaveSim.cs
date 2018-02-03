using UnityEngine;

namespace Crest
{
    public class ShapeWaveSim : MonoBehaviour
    {
        Renderer _rend;

        void Start()
        {
            _rend = GetComponent<Renderer>();
            _rend.material = new Material(Shader.Find("Ocean/Shape/Sim/2D Wave Equation"));
        }

        void OnWillRenderObject()
        {
            var ppPrev = Camera.current.GetComponent<PingPongRts>();
            if( ppPrev )
            {
                _rend.material.SetTexture( "_WavePPTSource", ppPrev._sourceThisFrame );
            }

            var wdc = Camera.current.GetComponent<WaveDataCam>();
            if( wdc )
            {
                Vector3 posDelta = wdc._renderData._posSnapped - wdc._renderData._posSnappedLast;

                _rend.material.SetVector( "_CameraPositionDelta", posDelta );

                // set ocean lod data so that the sim shader can read the ocean depth. however don't assign the wave heights - it would
                // try to assign the wave heights we are about to render to which is not cool
                wdc.ApplyMaterialParams( 0, _rend.material, false, true );
            }
        }

        public void OnOceanScaleChange( bool newScaleSmaller )
        {
            // copy wave sources up or down chain
            var cams = OceanRenderer.Instance.Builder._shapeCameras;
            if( newScaleSmaller )
            {
                // accumulate simulation results UP the lod chain - combine L into L+1

                for( int L = cams.Length - 2; L >= 0; L-- )
                {
                    Graphics.Blit( cams[L].GetComponent<PingPongRts>()._sourceThisFrame, cams[L + 1].GetComponent<PingPongRts>()._sourceThisFrame );
                    cams[L + 1].GetComponent<WaveDataCam>()._renderData._posSnappedLast = cams[L].GetComponent<WaveDataCam>()._renderData._posSnappedLast;
                }
                Graphics.Blit( Texture2D.blackTexture, cams[0].GetComponent<PingPongRts>()._sourceThisFrame );
                cams[0].GetComponent<WaveDataCam>()._renderData._posSnappedLast = Vector3.zero;
            }
            else
            {
                // accumulate simulation results DOWN the lod chain - combine L into L-1

                for( int L = 1; L < cams.Length; L++ )
                {
                    Graphics.Blit( cams[L].GetComponent<PingPongRts>()._sourceThisFrame, cams[L - 1].GetComponent<PingPongRts>()._sourceThisFrame );
                    cams[L - 1].GetComponent<WaveDataCam>()._renderData._posSnappedLast = cams[L].GetComponent<WaveDataCam>()._renderData._posSnappedLast;
                }
                Graphics.Blit( Texture2D.blackTexture, cams[cams.Length - 1].GetComponent<PingPongRts>()._sourceThisFrame );
                cams[cams.Length - 1].GetComponent<WaveDataCam>()._renderData._posSnappedLast = Vector3.zero;
            }
        }

        static ShapeWaveSim _instance;
        public static ShapeWaveSim Instance { get { return _instance != null ? _instance : (_instance = FindObjectOfType<ShapeWaveSim>()); } }
    }
}
