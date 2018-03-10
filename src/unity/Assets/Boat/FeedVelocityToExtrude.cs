using UnityEngine;

public class FeedVelocityToExtrude : MonoBehaviour {

    public BoatAlignNormal _boat;
    
    Vector3 _posLast;

    [HideInInspector]
    public Vector3 _localOffset;

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

        _mat.SetVector("_Velocity", (transform.position - _posLast) / Time.deltaTime);
        _posLast = transform.position;

        _mat.SetFloat("_Weight", (_boat == null || _boat.InWater) ? 1f : 0f);
    }
}
