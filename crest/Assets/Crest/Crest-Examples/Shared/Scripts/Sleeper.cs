// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

/// <summary>
/// Adds a sleep/freeze into the update, can be used to inflate the frame time.
/// </summary>
public class Sleeper : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    public int _sleepMs = 0;
    public bool _jitter = false;
    public int _sleepStride = 1;

    void Update()
    {
        if (Crest.OceanRenderer.FrameCount % _sleepStride == 0)
        {
            var sleep = _jitter ? (int)(Random.value * _sleepMs) : _sleepMs;
            System.Threading.Thread.Sleep(sleep);
        }
    }
}
