using UnityEngine;

public class BoatFX : MonoBehaviour {

    public float _fastSpeed = 11f;

    public MeshRenderer[] _translationBumps;
    float[] _translationBumpAmps;

    public float _displaceMul = 0.1f;
    public MeshRenderer[] _volumeDisplace;

    BoatAlignNormal _boat;
    Rigidbody _rb;

	void Start () {
        _rb = GetComponent<Rigidbody>();
        _boat = GetComponent<BoatAlignNormal>();

        _translationBumpAmps = new float[_translationBumps.Length];
        for (int i = 0; i < _translationBumps.Length; i++)
        {
            _translationBumpAmps[i] = _translationBumps[i].material.GetFloat("_Amplitude");
        }

    }
	
	void Update () {
        float speedParam = _rb.velocity.magnitude / _fastSpeed;
        if (!_boat.InWater)
        {
            speedParam = 0f;
        }

        for (int i = 0; i < _translationBumps.Length; i++)
        {
            _translationBumps[i].material.SetFloat("_Amplitude", speedParam * _translationBumpAmps[i]);
        }

        if (_boat.InWater)
        {
            foreach (var mr in _volumeDisplace)
            {
                mr.material.SetFloat("_Amplitude", _boat.VelocityRelativeToWater.y * _displaceMul);
            }
        }
    }
}
