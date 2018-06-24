// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

[System.Serializable]
public class DynamicBump
{
    public MeshRenderer _meshRend;

    [HideInInspector]
    public Vector3 _localOffset;

    [HideInInspector]
    public float _baseAmp;

    public void Init(Transform parent)
    {
        _baseAmp = _meshRend.material.GetFloat("_Amplitude");
        _localOffset = parent.InverseTransformPoint(_meshRend.transform.position);
    }

    public void Update(Transform parent, BoatAlignNormal boat, float amp)
    {
        _meshRend.material.SetFloat("_Amplitude", amp);

        // this line is key to making sure dynamic water fx are aligned with actual position of boat. the
        // fx will be copied into the displacement texture buffer, which is displaced afterwards, so this
        // line inverts that displacement to compensate. alternatively the vertex shader of the FX geom could
        // use iteration to find the appropriate place to render into the displacement texture, but this is easier
        // and seems to work well enough so far - although it would get worse with chop > 1.
        _meshRend.transform.position = parent.TransformPoint(_localOffset) - boat.DisplacementToBoat;
    }
}

public class BoatFX : MonoBehaviour {

    public float _fastSpeed = 11f;

    public DynamicBump[] _translationBumps;

    public float _displaceMulUp = 0.1f;
    public float _displaceMulDown = -0.5f;
    public DynamicBump[] _volumeDisplace;

    BoatAlignNormal _boat;
    Rigidbody _rb;

	void Start () {
        _rb = GetComponent<Rigidbody>();
        _boat = GetComponent<BoatAlignNormal>();

        foreach (var bump in _translationBumps)
        {
            bump.Init(transform);
        }
    }
	
	void Update () {
        var rbVel = _rb.velocity;
        rbVel.y = 0f;
        float speedParam = rbVel.magnitude / _fastSpeed;
        if (!_boat.InWater)
        {
            speedParam = 0f;
        }

        foreach (var bump in _translationBumps)
        {
            bump.Update(transform, _boat, speedParam * bump._baseAmp);
        }

        float displaceMul = _boat.VelocityRelativeToWater.y > 0f ? _displaceMulUp : _displaceMulDown;
        foreach (var bump in _volumeDisplace)
        {
            bump.Update(transform, _boat, _boat.InWater ? _boat.VelocityRelativeToWater.y * displaceMul : 0f);
        }
    }
}
