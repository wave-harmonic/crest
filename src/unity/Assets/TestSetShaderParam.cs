using UnityEngine;

public class TestSetShaderParam : MonoBehaviour {

    Renderer _rend;
    MaterialPropertyBlock _mpb;

    void Start () {
        _rend = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void Update () {
        _rend.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Glossiness", Random.value);
        _rend.SetPropertyBlock(_mpb);

        //_rend.material.SetFloat("_Glossiness", Random.value);
	}
}
