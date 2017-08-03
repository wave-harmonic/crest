using UnityEngine;

public class PingPongRts : MonoBehaviour
{
    public RenderTexture _rtA;
    public RenderTexture _rtB;
    RenderTexture _targetThisFrame;
    public string[] _sourceShaderSamplerNames;

	void Update()
    {
        RenderTexture sourceThisFrame;
        UpdatePingPong( out sourceThisFrame );

        Cam.targetTexture = _targetThisFrame;

        foreach( string rtName in _sourceShaderSamplerNames )
        {
            Shader.SetGlobalTexture( rtName, sourceThisFrame );
        }
	}

    void UpdatePingPong( out RenderTexture sourceThisFrame )
    {
        // switch RTs
        sourceThisFrame = _targetThisFrame;
        _targetThisFrame = _targetThisFrame == _rtA ? _rtB : _rtA;

        if( _targetThisFrame == null )
        {
            Debug.LogWarning( "One or both of the RTs are not specified.", this );
        }
    }

    Camera _cam; Camera Cam { get { return _cam != null ? _cam : (_cam = GetComponent<Camera>()); } }
}
