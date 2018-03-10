using UnityEngine;

public class FeedVelocityToExtrude : MonoBehaviour {

    public BoatAlignNormal _boat;
    
    Vector3 _posLast;

    [HideInInspector]
    public Vector3 _localOffset;

    private void Start()
    {
        _localOffset = transform.localPosition;
    }

    void LateUpdate()
    {
        var disp = _boat ? _boat.DisplacementToBoat : Vector3.zero;
        transform.position = transform.parent.TransformPoint(_localOffset) - disp;

        GetComponent<Renderer>().material.SetVector("_Velocity", (transform.position - _posLast) / Time.deltaTime);
        _posLast = transform.position;
	}
}
