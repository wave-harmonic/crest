using UnityEngine;

namespace OceanResearch
{
    public class OceanDebugGUI : MonoBehaviour
    {
        public bool _showSimTargets = true;

        void OnGUI()
        {
            Color bkp = GUI.color;

            GUI.skin.toggle.normal.textColor = Color.white;
            GUI.skin.label.normal.textColor = Color.white;

            GUI.color = Color.black * 0.7f;
            GUI.DrawTexture( new Rect( 0, 0, 150, Screen.height ), Texture2D.whiteTexture );
            GUI.color = Color.white;

            RenderWireFrame._wireFrame = GUI.Toggle( new Rect( 0, 0, 75, 25 ), RenderWireFrame._wireFrame, "Wireframe" );

            OceanRenderer.Instance._freezeTime = GUI.Toggle( new Rect( 0, 25, 100, 25 ), OceanRenderer.Instance._freezeTime, "Freeze waves" );

            GUI.changed = false;
            OceanRenderer.Instance._enableSmoothLOD = GUI.Toggle( new Rect( 0, 50, 150, 25 ), OceanRenderer.Instance._enableSmoothLOD, "Enable smooth LOD" );
            if( GUI.changed ) OceanRenderer.Instance.SetSmoothLODsShaderParam();

            OceanRenderer.Instance._minTexelsPerWave = GUI.HorizontalSlider( new Rect( 0, 100, 150, 25 ), OceanRenderer.Instance._minTexelsPerWave, 0, 15 );
            GUI.Label( new Rect( 0, 75, 150, 25 ), string.Format( "Min verts per wave: {0}", OceanRenderer.Instance._minTexelsPerWave.ToString( "0.00" ) ) );


            _showSimTargets = GUI.Toggle( new Rect( 0, 120, 100, 25 ), _showSimTargets, "Show sim data" );

            // draw source textures to screen
            if( _showSimTargets )
            {
                int ind = 0;
                foreach( var cam in OceanRenderer.Instance.Builder._shapeCameras )
                {
                    if( !cam ) continue;

                    var pp = cam.GetComponent<PingPongRts>();
                    if( !pp ) continue;

                    float b = 7f;
                    float h = Screen.height / (float)OceanRenderer.Instance.Builder._shapeCameras.Length, w = h + b;
                    float x = Screen.width - w, y = ind * h;

                    GUI.color = Color.black * 0.7f;
                    GUI.DrawTexture( new Rect( x, y, w, h ), Texture2D.whiteTexture );
                    GUI.color = Color.white;
                    GUI.DrawTexture( new Rect( x + b, y + b / 2f, h - b, h - b ), pp._sourceThisFrame, ScaleMode.ScaleAndCrop, false );

                    ind++;
                }
            }

            if( GUI.Button( new Rect( 0, 145, 100, 25 ), "Clear sim data" ) )
            {
                foreach( var cam in OceanRenderer.Instance.Builder._shapeCameras )
                {
                    Graphics.Blit( Texture2D.blackTexture, cam.GetComponent<PingPongRts>()._targetThisFrame );
                }
            }

            GUI.color = bkp;
        }
    }
}
