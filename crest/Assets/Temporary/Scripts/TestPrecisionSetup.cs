using Crest;
using UnityEngine;

// Execute after OceanRenderer.
[DefaultExecutionOrder(201)]
public class TestPrecisionSetup : MonoBehaviour
{
    [SerializeField] bool _EnableGridFudge;

    [Range(2.0f, 2.1f)]
    [SerializeField] float _GridFudge = 2f;

    [SerializeField] bool _EnableScaleFudge;

    [Range(1.0f, 1.1f)]
    [SerializeField] float _ScaleFudge = 1f;

    [SerializeField] int _LodDataResolution = 384;

    [SerializeField] Vector2 _ScreenResolution;

    [SerializeField] Vector2 _CurrentScreenResolutionReadOnly;

    void OnEnable()
    {
        var camera = Camera.main;

        if (OceanRenderer.Instance == null || camera == null)
        {
            return;
        }

        if (_ScreenResolution.magnitude > 0)
        {
            if (camera.pixelRect.Contains(_ScreenResolution) && camera.pixelRect.size != _ScreenResolution)
            {
                var center = (camera.pixelRect.size - _ScreenResolution) * 0.5f;
                camera.pixelRect = new Rect(center, _ScreenResolution);
            }
            else
            {
                Debug.LogWarning($"Screen resolution ({_ScreenResolution}) must be smaller than game view ({camera.pixelRect.size}).");

            }
        }

        var material = OceanRenderer.Instance.OceanMaterial;

        if (_EnableGridFudge)
        {
            material.EnableKeyword("_APPLYGRIDFUDGE_ON");
        }

        material.SetFloat("_GridFudge", _GridFudge);

        if (_EnableScaleFudge)
        {
            material.EnableKeyword("_APPLYSCALEFUDGE_ON");
        }

        material.SetFloat("_ScaleFudge", _ScaleFudge);

        if (OceanRenderer.Instance.LodDataResolution != _LodDataResolution)
        {
            OceanRenderer.Instance._lodDataResolution = _LodDataResolution;
            OceanRenderer.Instance.Rebuild();
        }
    }

    void Update() => _CurrentScreenResolutionReadOnly = new Vector2(Screen.width, Screen.height);
}
