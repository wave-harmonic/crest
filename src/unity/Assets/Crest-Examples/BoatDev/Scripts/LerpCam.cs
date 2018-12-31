// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

public class LerpCam : MonoBehaviour
{
    [SerializeField] float _lerpAlpha = 0.1f;
    [SerializeField] Transform _targetPos = null;
    [SerializeField] Transform _targetLookatPos = null;
    [SerializeField] float _lookatOffset = 5f;
    [SerializeField] float _minHeightAboveWater = 0.5f;

    SamplingData _samplingData = new SamplingData();

    void Update()
    {
        var targetPos = _targetPos.position;
        var rect = new Rect(transform.position, Vector3.zero);
        var collProvider = OceanRenderer.Instance.CollisionProvider;
        if (collProvider.GetSamplingData(ref rect, 0f, _samplingData))
        {
            float h;
            if (OceanRenderer.Instance != null && collProvider.SampleHeight(ref targetPos, _samplingData, out h))
            {
                targetPos.y = Mathf.Max(targetPos.y, h + _minHeightAboveWater);
            }
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, _lerpAlpha * Time.deltaTime * 60f);
        transform.LookAt(_targetLookatPos.position + _lookatOffset * Vector3.up);

        collProvider.ReturnSamplingData(_samplingData);
    }
}
