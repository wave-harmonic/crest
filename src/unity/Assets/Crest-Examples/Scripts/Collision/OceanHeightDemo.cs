// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Set this transform to ocean height
/// </summary>
public class OceanHeightDemo : MonoBehaviour
{
	void Update ()
    {
        float scale = transform.lossyScale.x;

        var rect = new Rect(transform.position.x - scale / 2f, transform.position.z - scale / 2f, scale, scale);
        int lod = OceanRenderer.SuggestCollisionLOD(rect);

        if (lod > -1)
        {
            var pos = transform.position;
            var height = OceanRenderer.Instance.Builder._shapeWDCs[lod].GetHeightExpensive(ref pos);
            transform.position += Vector3.up * (height - pos.y);
        }
    }
}
