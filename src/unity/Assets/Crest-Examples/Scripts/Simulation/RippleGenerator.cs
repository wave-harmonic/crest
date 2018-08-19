using UnityEngine;

public class RippleGenerator : MonoBehaviour {

    public float _warmUp = 3f;
    public float _onTime = 0.2f;
    public float _period = 4f;

    MeshRenderer _mr;
    Material _mat;

	void Start()
    {
        _mr = GetComponent<MeshRenderer>();
        _mr.enabled = false;
        _mat = _mr.material;
	}
	
	void Update()
    {
        float t = Time.time;
        if (t < _warmUp)
            return;
        t -= _warmUp;
        t = Mathf.Repeat(t, _period);
        _mr.enabled = t < _onTime;

        int simsPresent, simsActive;
        Crest.LodDataDynamicWaves.CountWaveSims(out simsPresent, out simsActive);
        if (simsPresent == 0)
        {
            enabled = false;
            return;
        }

        if (simsActive > 0)
        {
            _mat.SetFloat("_SimCount", simsActive);
        }
    }
}
