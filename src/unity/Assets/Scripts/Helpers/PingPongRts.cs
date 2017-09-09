using UnityEngine;

public class PingPongRts : MonoBehaviour
{
    public RenderTexture _rtA, _rtB, _rtPrev;
    public RenderTexture _targetThisFrame;
    public RenderTexture sourceThisFrame;

    public void InitRTs( RenderTexture rtA, RenderTexture rtB, RenderTexture rtPrev )
    {
        _rtA = rtA;
        _rtB = rtB;
        _rtPrev = rtPrev;
    }

    void Update()
    {
        UpdatePingPong( out sourceThisFrame );

        Cam.targetTexture = _targetThisFrame;
    }

    void UpdatePingPong( out RenderTexture sourceThisFrame )
    {
        // switch RTs
        sourceThisFrame = _targetThisFrame;
        _targetThisFrame = _targetThisFrame == _rtA ? _rtB : _rtA;

        // make a copy of the target
        Graphics.Blit( _targetThisFrame, _rtPrev );

        if( _targetThisFrame == null )
        {
            Debug.LogWarning( "One or both of the RTs are not specified.", this );
        }
    }

    void OnGUI()
    {
        // draw source textures to screen
        if( sourceThisFrame )
        {
            int ind = ShapeWaveSim.GetShapeCamIndex( Cam );
            if( ind < 0 ) return;

            float b = 7f;
            float h = Screen.height/(float)OceanResearch.OceanRenderer.Instance.Builder._shapeCameras.Length, w = h + b;
            float x = Screen.width - w, y = ind * h;

            GUI.color = Color.black * 0.7f;
            GUI.DrawTexture( new Rect( x, y, w, h ), Texture2D.whiteTexture );
            GUI.color = Color.white;
            GUI.DrawTexture( new Rect( x + b, y + b / 2f, h - b, h - b ), sourceThisFrame );
        }
    }

    Camera _cam; Camera Cam { get { return _cam != null ? _cam : (_cam = GetComponent<Camera>()); } }
}
