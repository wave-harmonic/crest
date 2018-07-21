using UnityEngine;

public class LerpCam : MonoBehaviour
{
    public float _lerpAlpha = 0.1f;
    public Transform _targetPos;
    public Transform _targetLookatPos;
    public float _lookatOffset = 5f;

	void Update()
    {
        if (Crest.OceanRenderer.Instance._freezeTime) return;

        transform.position = Vector3.Lerp(transform.position, _targetPos.position, _lerpAlpha);
        transform.LookAt(_targetLookatPos.position + _lookatOffset * Vector3.up);
	}
}
