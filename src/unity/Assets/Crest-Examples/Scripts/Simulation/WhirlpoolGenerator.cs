using UnityEngine;

public class WhirlpoolGenerator : MonoBehaviour
{
    MeshRenderer _mr;
    Material _mat;

	void Start()
    {
        _mr = GetComponent<MeshRenderer>();
        _mat = _mr.material;
	}
}
