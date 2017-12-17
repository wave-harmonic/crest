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

            float x = 5f, y = 0f;
            float w = 150, h = 25f;

            GUI.color = Color.black * 0.7f;
            GUI.DrawTexture( new Rect( 0, 0, w + 2f * x, Screen.height ), Texture2D.whiteTexture );
            GUI.color = Color.white;

            GUI.Label( new Rect( x, y, w, h ), string.Format( "Wind speed: {0} km/h", (ShapeGerstner.Instance._windSpeed * 3.6f).ToString( "0.00" ) ) ); y += h;
            ShapeGerstner.Instance._windSpeed = GUI.HorizontalSlider( new Rect( x, y, w, h ), ShapeGerstner.Instance._windSpeed * 3.6f, 0f, 60f ) / 3.6f; y += h;

            GUI.Label( new Rect( x, y, w, h ), string.Format( "Choppiness: {0}", ShapeGerstner.Instance._choppiness.ToString( "0.00" ) ) ); y += h;
            ShapeGerstner.Instance._choppiness = GUI.HorizontalSlider( new Rect( x, y, w, h ), ShapeGerstner.Instance._choppiness, 0f, 1f ); y += h;

            RenderWireFrame._wireFrame = GUI.Toggle( new Rect( x, y, w, h ), RenderWireFrame._wireFrame, "Wireframe" ); y += h;

            OceanRenderer.Instance._freezeTime = GUI.Toggle( new Rect( x, y, w, h ), OceanRenderer.Instance._freezeTime, "Freeze waves" ); y += h;

            GUI.changed = false;
            OceanRenderer.Instance._enableSmoothLOD = GUI.Toggle( new Rect( x, y, w, h ), OceanRenderer.Instance._enableSmoothLOD, "Enable smooth LOD" ); y += h;
            if( GUI.changed ) OceanRenderer.Instance.SetSmoothLODsShaderParam();

            GUI.Label( new Rect( x, y, w, h ), string.Format( "Min verts per wave: {0}", OceanRenderer.Instance._minTexelsPerWave.ToString( "0.00" ) ) ); y += h;
            OceanRenderer.Instance._minTexelsPerWave = GUI.HorizontalSlider( new Rect( x, y, w, h ), OceanRenderer.Instance._minTexelsPerWave, 0, 15 ); y += h;


            _showSimTargets = GUI.Toggle( new Rect( x, y, w, h ), _showSimTargets, "Show sim data" ); y += h;

            if( GUI.Button( new Rect( x, y, w, h ), "Clear sim data" ) )
            {
                foreach( var cam in OceanRenderer.Instance.Builder._shapeCameras )
                {
                    Graphics.Blit( Texture2D.blackTexture, cam.GetComponent<PingPongRts>()._targetThisFrame );
                }
            }
            y += h;


            // draw source textures to screen
            if( _showSimTargets )
            {
                int ind = 0;
                foreach( var cam in OceanRenderer.Instance.Builder._shapeCameras )
                {
                    if( !cam ) continue;

                    var pp = cam.GetComponent<PingPongRts>();
                    if( !pp ) continue;
                    if( pp._sourceThisFrame == null ) continue;

                    float b = 7f;
                    h = Screen.height / (float)OceanRenderer.Instance.Builder._shapeCameras.Length;
                    w = h + b;
                    x = Screen.width - w;
                    y = ind * h;

                    GUI.color = Color.black * 0.7f;
                    GUI.DrawTexture( new Rect( x, y, w, h ), Texture2D.whiteTexture );
                    GUI.color = Color.white;
                    GUI.DrawTexture( new Rect( x + b, y + b / 2f, h - b, h - b ), pp._sourceThisFrame, ScaleMode.ScaleAndCrop, false );

                    ind++;
                }
            }

            GUI.color = bkp;
        }
    }
}
