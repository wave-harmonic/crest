using UnityEngine;

public class ShapeWaveSim : MonoBehaviour
{
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

        var wdc = Camera.current.GetComponent<OceanResearch.WaveDataCam>();
        if( wdc )
        {
            Vector3 posDelta = wdc._renderData._posSnapped - wdc._lastPosition;

            _rend.material.SetVector( "_CameraPositionDelta", posDelta );
        }
    }
}
