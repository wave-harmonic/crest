// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Set this transform to ocean height
/// </summary>
public class OceanSampleHeightDemo : MonoBehaviour
{
	void Update()
    {
        // If you are taking multiple samples over an area, setup up the collision sampling state first by calling
        // PrewarmForSamplingArea().

        // Assume a primitive like a sphere or box, providing this side length means high frequency waves
        // much shorter than the object will be ignored.
        float shapeLength = 2f * transform.lossyScale.magnitude;

        var pos = transform.position;
        float height;
        if (OceanRenderer.Instance.CollisionProvider.SampleHeight(ref pos, out height, shapeLength))
        {
            pos.y = height;
            transform.position = pos;
        }
    }
}
