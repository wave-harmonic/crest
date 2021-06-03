// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

public class LerpCam : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    [SerializeField] float _lerpAlpha = 0.1f;
    [SerializeField] Transform _targetPos = null;
    [SerializeField] Transform _targetLookatPos = null;
    [SerializeField] float _lookatOffset = 5f;
    [SerializeField] float _minHeightAboveWater = 0.5f;

    SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    void Update()
    {
        if (OceanRenderer.Instance == null)
        {
            return;
        }

        _sampleHeightHelper.Init(transform.position, 0f);
        _sampleHeightHelper.Sample(out var h);

        var targetPos = _targetPos.position;
        targetPos.y = Mathf.Max(targetPos.y, h + _minHeightAboveWater);

        transform.position = Vector3.Lerp(transform.position, targetPos, _lerpAlpha * OceanRenderer.Instance.DeltaTime * 60f);
        transform.LookAt(_targetLookatPos.position + _lookatOffset * Vector3.up);
    }
}
