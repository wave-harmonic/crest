using UnityEngine;

[ExecuteAlways]
public class ResolutionDebugger : MonoBehaviour
{
    [SerializeField] int _screenWidth = 0;
    [SerializeField] int _screenHeight = 0;

    void Update()
    {
        _screenWidth = Screen.width;
        _screenHeight = Screen.height;
    }
}
