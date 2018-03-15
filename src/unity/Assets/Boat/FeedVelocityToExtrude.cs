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

    Material _mat;

    private void Start()
    {
        _localOffset = transform.localPosition;

        _mat = GetComponent<Renderer>().material;
    }

    void LateUpdate()
    {
        var disp = _boat ? _boat.DisplacementToBoat : Vector3.zero;
        transform.position = transform.parent.TransformPoint(_localOffset) - disp;

        float rnd = 1f + _noiseAmp * (2f * Mathf.PerlinNoise(_noiseFreq * Time.time, 0.5f) - 1f);
        _mat.SetVector("_Velocity", rnd * (transform.position - _posLast) / Time.deltaTime);
        _posLast = transform.position;

        _mat.SetFloat("_Weight", (_boat == null || _boat.InWater) ? 1f : 0f);
    }
}
