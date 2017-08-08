using UnityEngine;
using UnityEngine.Assertions;

public class ShapeWaveSim : MonoBehaviour
{
    void OnWillRenderObject()
    {
        PingPongRts pp = Camera.current.GetComponent<PingPongRts>();
        if( pp == null )
            return;

        var rend = GetComponent<Renderer>();
        Assert.IsNotNull( rend );

        rend.material.SetTexture( "_WavePPTSource", pp.sourceThisFrame );
        rend.material.SetTexture( "_WavePPTSource_Prev", pp._rtPrev );
    }
}
