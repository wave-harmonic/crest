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
        var pos = transform.position;
        float height;
        if (OceanRenderer.Instance.CollisionProvider.SampleHeight(ref pos, out height))
        {
            pos.y = height;
            transform.position = pos;
        }

        // if you are taking multiple samples over an area, setup up the collision sampling state first by calling
        // PrewarmForSamplingArea()
    }
}
