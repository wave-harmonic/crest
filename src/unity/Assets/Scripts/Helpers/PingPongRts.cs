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

    Camera _cam; Camera Cam { get { return _cam != null ? _cam : (_cam = GetComponent<Camera>()); } }
}
