using UnityEngine;

public class FeedVelocityToExtrude : MonoBehaviour {

    public BoatAlignNormal _boat;
    
    Vector3 _posLast;

    [HideInInspector]
    public Vector3 _localOffset;

    [Range(0f, 10f)]
    public float _noiseFreq = 6f;

    [Range(0f, 1f)]
    public float _noiseAmp = 0.5f;

    [Range(0f, 3f)]
    public float _weight = 2f;
    [Range(0f, 2f)]
    public float _weightUpDownMul = 0.5f;

    [Tooltip("Teleport speed (km/h) - if the calculated speed is larger than this amount, the object is deemed to have teleported and the computed velocity is discarded.")]
    public float _teleportSpeed = 500f;
    public bool _warnOnTeleport = false;
    [Tooltip("Maximum speed clamp (km/h), useful for controlling/limiting wake.")]
    public float _maxSpeed = 100f;
    public bool _warnOnSpeedClamp = false;

    Material _mat;

    private void Start()
    {
        _localOffset = transform.localPosition;

        _mat = GetComponent<Renderer>().material;

        gameObject.layer = LayerMask.NameToLayer("SimWave");
    }

    void LateUpdate()
    {
        var disp = _boat ? _boat.DisplacementToBoat : Vector3.zero;
        transform.position = transform.parent.TransformPoint(_localOffset) - disp;

        float rnd = 1f + _noiseAmp * (2f * Mathf.PerlinNoise(_noiseFreq * Time.time, 0.5f) - 1f);
        Vector3 vel = (transform.position - _posLast) / Time.deltaTime;
        vel.y *= _weightUpDownMul;

        float speedKmh = vel.magnitude * 3.6f;
        if (speedKmh > _teleportSpeed)
        {
            // teleport detected
            vel *= 0f;

            if (_warnOnTeleport)
            {
                Debug.LogWarning("Teleport detected (speed = " + speedKmh.ToString() + "), velocity discarded.", this);
            }
        }
        else if (speedKmh > _maxSpeed)
        {
            // limit speed to max
            vel *= _maxSpeed / speedKmh;

            if (_warnOnSpeedClamp)
            {
                Debug.LogWarning("Speed (" + speedKmh.ToString() + ") exceeded max limited, clamped.", this);
            }
        }
        
        _mat.SetVector("_Velocity", rnd * vel);
        _posLast = transform.position;

        _mat.SetFloat("_Weight", (_boat == null || _boat.InWater) ? _weight : 0f);

        _mat.SetFloat("_SimDeltaTime", Mathf.Min(Time.deltaTime, Crest.LodDataPersistent.MAX_SIM_DELTA_TIME));
    }
}
