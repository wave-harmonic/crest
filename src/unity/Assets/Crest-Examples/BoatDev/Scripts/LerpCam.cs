using UnityEngine;

public class LerpCam : MonoBehaviour
{
    public float _lerpAlpha = 0.1f;
    public Transform _targetPos;
    public Transform _targetLookatPos;
    public float _lookatOffset = 5f;

	void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPos.position, _lerpAlpha * Time.deltaTime * 60f);
        transform.LookAt(_targetLookatPos.position + _lookatOffset * Vector3.up);
	}
}
