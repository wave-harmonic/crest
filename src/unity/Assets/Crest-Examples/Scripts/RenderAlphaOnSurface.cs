using UnityEngine;
using Crest;

public class RenderAlphaOnSurface : MonoBehaviour
{
    public bool _vertMatching = true;

    Material _mat;

	void Start()
    {
        _mat = GetComponent<Renderer>().material;

        if (_vertMatching)
        {
            GetComponent<MeshFilter>().mesh = OceanBuilder.BuildOceanPatch(OceanBuilder.PatchType.Interior, OceanRenderer.Instance._baseVertDensity);
        }
    }

    private void LateUpdate()
    {
        var pwm = new PropertyWrapperMaterial(_mat);

        var rect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
        var idx = WaveDataCam.SuggestCollisionLOD(rect);
        
        if (idx > -1)
        {
            var wdcs = OceanRenderer.Instance.Builder._shapeWDCs;
            wdcs[idx + 0].ApplyMaterialParams(0, pwm);
            wdcs[idx + 1].ApplyMaterialParams(1, pwm);

            if(_vertMatching)
            {
                float scale = Mathf.Pow(2f, Mathf.Round(Mathf.Log(wdcs[idx + 0].transform.lossyScale.x) / Mathf.Log(2f)));
                transform.localScale = new Vector3(scale, 1f, scale);

                pwm.SetVector("_GeomData", new Vector4(transform.localScale.x / OceanRenderer.Instance._baseVertDensity, 0f, 0f, OceanRenderer.Instance._baseVertDensity));
            }

            // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
            bool needToBlendOutShape = idx == 0 && OceanRenderer.Instance.ScaleCouldIncrease;
            float meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

            // blend furthest normals scale in/out to avoid pop, if scale could reduce
            bool needToBlendOutNormals = idx == wdcs.Length - 1 && OceanRenderer.Instance.ScaleCouldDecrease;
            float farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            pwm.SetVector("_InstanceData", new Vector4(meshScaleLerp, farNormalsWeight, idx));
        }
    }
}
