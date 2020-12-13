// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

/// <summary>
/// Places the game object on the water surface by moving it vertically.
/// </summary>
public class OceanSampleHeightDemo : MonoBehaviour
{
    [Tooltip("Some query systems return results with latency. This applies a correction to compensate.")]
    public bool _compensateLatency = true;

    SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    void Update()
    {
        // Assume a primitive like a sphere or box.
        var r = transform.lossyScale.magnitude;
        _sampleHeightHelper.Init(transform.position, 2f * r);

        float height;
        bool result = _compensateLatency ? _sampleHeightHelper.SampleWithLantencyCompensation(out height) : _sampleHeightHelper.Sample(out height);

        if (result)
        {
            var pos = transform.position;
            pos.y = height;
            transform.position = pos;
        }
    }
}
