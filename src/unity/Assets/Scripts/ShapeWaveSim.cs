using UnityEngine;

namespace OceanResearch
{
    public class ShapeWaveSim : MonoBehaviour
    {
        Renderer _rend;
        static Material _matCombineSims;

        void Start()
        {
            _rend = GetComponent<Renderer>();

            if( _matCombineSims == null )
                _matCombineSims = new Material( Shader.Find( "Ocean/Shape/Sim/Combine" ) );
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
            }
        }

        // combine/accumulate sim results together
        public static void OnShapeCamerasFinishedRendering()
        {
            var cams = OceanRenderer.Instance.Builder._shapeCameras;
            for( int L = cams.Length - 2; L >= 0; L-- )
            {
                // save the projection params to enable combining results across multiple shape textures
                cams[L].GetComponent<WaveDataCam>().ApplyMaterialParams( 0, _matCombineSims );
                cams[L + 1].GetComponent<WaveDataCam>().ApplyMaterialParams( 1, _matCombineSims );

                // accumulate simulation results down the lod chain - combine L+1 into L
                Graphics.Blit( cams[L + 1].GetComponent<PingPongRts>()._targetThisFrame, cams[L].GetComponent<PingPongRts>()._targetThisFrame, _matCombineSims );
            }
        }
    }
}
