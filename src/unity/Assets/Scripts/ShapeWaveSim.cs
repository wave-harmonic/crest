using UnityEngine;

namespace Crest
{
    public class ShapeWaveSim : MonoBehaviour
    {
        public Material _matCombineSims;

        public static bool _captureShape = true;

        Renderer _rend;

        void Start()
        {
            _rend = GetComponent<Renderer>();
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

        // combine/accumulate sim results together
        public void OnShapeCamerasFinishedRendering()
        {
            if( Shader.GetGlobalFloat( "_MyDeltaTime" ) <= Mathf.Epsilon )
                return;

            var cams = OceanRenderer.Instance.Builder._shapeCameras;
            for( int L = cams.Length - 2; L >= 0; L-- )
            {
                // save the projection params to enable combining results across multiple shape textures
                cams[L].GetComponent<WaveDataCam>().ApplyMaterialParams( 0, _matCombineSims );
                cams[L + 1].GetComponent<WaveDataCam>().ApplyMaterialParams( 1, _matCombineSims );

                // accumulate simulation results down the lod chain - combine L+1 into L
                Graphics.Blit( cams[L + 1].GetComponent<PingPongRts>()._targetThisFrame, cams[L].GetComponent<PingPongRts>()._targetThisFrame, _matCombineSims );
            }

            // this makes sure the dt goes to 0 so that if the editor is paused, the simulation will stop progressing. this could
            // be made editor only, but that could lead to some very confusing bugs/behaviour, so leaving it like this for now.
            Shader.SetGlobalFloat( "_MyDeltaTime", 0f );
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
