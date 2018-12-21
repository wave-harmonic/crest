// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Set this transform to ocean height
/// </summary>
public class OceanSampleHeightDemo : MonoBehaviour
{
    SamplingData _samplingData = new SamplingData();

    void Update()
    {
        // Assume a primitive like a sphere or box.
        var r = transform.lossyScale.magnitude;

        var collProvider = OceanRenderer.Instance.CollisionProvider;
        var rect = new Rect(new Vector2(transform.position.x - r, transform.position.z - r), 2f * r * Vector2.one);
        collProvider.GetSamplingData(ref rect, 2f * r, _samplingData);

        var pos = transform.position;
        float height;
        if (OceanRenderer.Instance.CollisionProvider.SampleHeight(ref pos, _samplingData, out height))
        {
            pos.y = height;
            transform.position = pos;
        }

        collProvider.ReturnSamplingData(_samplingData);
    }
}
