using UnityEngine;

public class FeedVelocityToExtrude : MonoBehaviour {

    Vector3 _posLast;

	void LateUpdate() {

        GetComponent<Renderer>().material.SetVector("_Velocity", (transform.position - _posLast) / Time.deltaTime);
        _posLast = transform.position;
	}
}
