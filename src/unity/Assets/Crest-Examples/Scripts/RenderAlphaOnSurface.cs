using UnityEngine;
using Crest;

public class RenderAlphaOnSurface : MonoBehaviour
{
    public int _lodIdx = 1;

    Material _mat;

	void Start()
    {
        _mat = GetComponent<Renderer>().material;

        GetComponent<MeshFilter>().mesh = OceanBuilder.BuildOceanPatch(OceanBuilder.PatchType.Interior, OceanRenderer.Instance._baseVertDensity);
	}

    private void Update()
    {
        var b = GetComponent<Renderer>().bounds;
        var rect = new Rect(b.min.x, b.min.z, 2f, 2f);// b.extents.x, b.extents.z);
        var idx = WaveDataCam.SuggestCollisionLOD(rect);
        if (idx > -1)
        {
            var wdcs = OceanRenderer.Instance.Builder._shapeWDCs;
            wdcs[idx + 0].ApplyMaterialParams(0, new PropertyWrapperMaterial(_mat));
            wdcs[idx + 1].ApplyMaterialParams(1, new PropertyWrapperMaterial(_mat));

            float scale = Mathf.Pow(2f, Mathf.Round(Mathf.Log(wdcs[idx + 0].transform.lossyScale.x) / Mathf.Log(2f)));
            transform.localScale = new Vector3(scale/2f, 1f, scale);
        }
    }
}
