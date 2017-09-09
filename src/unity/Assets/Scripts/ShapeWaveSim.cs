using UnityEngine;
using UnityEngine.Assertions;

public class ShapeWaveSim : MonoBehaviour
{
    void OnWillRenderObject()
    {
        var rend = GetComponent<Renderer>();
        Assert.IsNotNull( rend );

        SetSampler( Camera.current, rend.material, "_WavePPTSource" );

        Camera nextScaleCam = GetNextScaleShapeCam( Camera.current );
        SetSampler( nextScaleCam, rend.material, "_WavePPTSourceNextScale" );

        Camera prevScaleCam = GetPrevScaleShapeCam( Camera.current );
        SetSampler( prevScaleCam, rend.material, "_WavePPTSourcePrevScale" );

        rend.material.SetFloat( "_IsSmallestScale", prevScaleCam == null ? 1f : 0f );
    }

    static void SetSampler( Camera cam, Material mat, string samplerName )
    {
        if( cam )
        {
            PingPongRts ppPrev = cam.GetComponent<PingPongRts>();
            if( ppPrev )
            {
                mat.SetTexture( samplerName, ppPrev.sourceThisFrame );
                mat.SetTexture( samplerName + "_Prev", ppPrev._rtPrev );
            }
        }
        else
        {
            mat.SetTexture( samplerName, Texture2D.blackTexture );
            mat.SetTexture( samplerName + "_Prev", Texture2D.blackTexture );
        }
    }

    static Camera GetPrevScaleShapeCam( Camera current )
    {
        int ind = GetShapeCamIndex( current );
        if( ind < 1 ) return null;
        return OceanResearch.OceanRenderer.Instance.Builder._shapeCameras[ind - 1];
    }

    static Camera GetNextScaleShapeCam( Camera current )
    {
        int ind = GetShapeCamIndex( current );
        if( ind < 0 || ind >= OceanResearch.OceanRenderer.Instance.Builder._shapeCameras.Length - 1 ) return null;
        return OceanResearch.OceanRenderer.Instance.Builder._shapeCameras[ind + 1];
    }

    public static int GetShapeCamIndex( Camera cam )
    {
        for( int i = 0; i < OceanResearch.OceanRenderer.Instance.Builder._shapeCameras.Length; i++ )
            if( OceanResearch.OceanRenderer.Instance.Builder._shapeCameras[i] == cam )
                return i;
        return -1;
    }
}
