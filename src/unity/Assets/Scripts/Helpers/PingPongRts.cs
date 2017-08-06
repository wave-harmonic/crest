using UnityEngine;

public class PingPongRts : MonoBehaviour
{
    public RenderTexture _rtA;
    public RenderTexture _rtB;
    RenderTexture _targetThisFrame;
    public string[] _sourceShaderSamplerNames;
    public Material _hackSetTexture;
    public RenderTexture _source_1;

	void Update()
    {
        RenderTexture sourceThisFrame;
        UpdatePingPong( out sourceThisFrame );

        Cam.targetTexture = _targetThisFrame;

        foreach( string rtName in _sourceShaderSamplerNames )
        {
            Shader.SetGlobalTexture( rtName, sourceThisFrame );
            // the gobal texture doesnt seem to work, so setting it manually as a hack.. for now..
            if( _hackSetTexture != null )
            {
                _hackSetTexture.SetTexture( rtName, sourceThisFrame );
                _hackSetTexture.SetTexture( "_WavePPTSource_1", _source_1 );
            }
        }
    }

    void UpdatePingPong( out RenderTexture sourceThisFrame )
    {
        // switch RTs
        sourceThisFrame = _targetThisFrame;
        _targetThisFrame = _targetThisFrame == _rtA ? _rtB : _rtA;

        // make a copy of the target
        Graphics.Blit( _targetThisFrame, _source_1 );

        if( _targetThisFrame == null )
        {
            Debug.LogWarning( "One or both of the RTs are not specified.", this );
        }
    }

    Camera _cam; Camera Cam { get { return _cam != null ? _cam : (_cam = GetComponent<Camera>()); } }
}
