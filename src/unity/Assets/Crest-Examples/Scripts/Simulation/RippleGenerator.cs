using UnityEngine;

public class RippleGenerator : MonoBehaviour {

    public float _warmUp = 3f;
    public float _onTime = 0.2f;
    public float _period = 4f;

    MeshRenderer _mr;

	void Start()
    {
        _mr = GetComponent<MeshRenderer>();
        _mr.enabled = false;
	}
	
	void Update()
    {
        float t = Time.time;
        if (t < _warmUp)
            return;
        t -= _warmUp;
        t = Mathf.Repeat(t, _period);
        _mr.enabled = t < _onTime;
	}
}
