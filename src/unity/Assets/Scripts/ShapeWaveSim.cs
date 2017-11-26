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
        PingPongRts ppPrev = Camera.current.GetComponent<PingPongRts>();
        if( ppPrev )
        {
            _rend.material.SetTexture( "_WavePPTSource", ppPrev._sourceThisFrame );
        }
    }
}
