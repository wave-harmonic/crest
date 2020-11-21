using UnityEngine;

public class TestScreenResolution : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] Vector2 _ScreenResolution;

    [Header("Debug Display")]
    [SerializeField] Vector2 _CurrentScreenResolution;

    void OnEnable()
    {
        var camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        if (_ScreenResolution != Vector2.zero)
        {
            if (_ScreenResolution.x == 0)
            {
                _ScreenResolution.x = camera.pixelWidth;
            }

            if (_ScreenResolution.y == 0)
            {
                _ScreenResolution.y = camera.pixelHeight;
            }

            if (camera.pixelRect.size != _ScreenResolution)
            {
                var center = (camera.pixelRect.size - _ScreenResolution) * 0.5f;
                camera.pixelRect = new Rect(center, _ScreenResolution);
            }
        }
    }

    void Update() => _CurrentScreenResolution = new Vector2(Screen.width, Screen.height);
}
