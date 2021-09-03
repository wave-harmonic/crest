// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

/// <summary>
/// Places the game object on the water surface by moving it vertically.
/// </summary>
[AddComponentMenu(Crest.Internal.Constants.MENU_PREFIX_EXAMPLE + "Ocean Sample Height Demo")]
public class OceanSampleHeightDemo : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    void Update()
    {
        // Assume a primitive like a sphere or box.
        var r = transform.lossyScale.magnitude;
        _sampleHeightHelper.Init(transform.position, 2f * r);

        if (_sampleHeightHelper.Sample(out var height))
        {
            var pos = transform.position;
            pos.y = height;
            transform.position = pos;
        }
    }
}
