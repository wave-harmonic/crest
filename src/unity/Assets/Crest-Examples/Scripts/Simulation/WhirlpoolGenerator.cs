using UnityEngine;

public class WhirlpoolGenerator : MonoBehaviour
{
    public bool _animate = true;
    public float _warmUp = 3f;
    public float _onTime = 0.2f;
    public float _period = 4f;

    MeshRenderer _mr;
    Material _mat;

	void Start()
    {
        _mr = GetComponent<MeshRenderer>();
        // if(_animate)
        // {
        //     _mr.enabled = false;
        // }
        _mat = _mr.material;
	}

	void Update()
    {
        // which lod is this object in (roughly)?
        Rect thisRect = new Rect(new Vector2(transform.position.x, transform.position.z), Vector3.zero);
        int minLod = Crest.ReadbackDisplacementsForCollision.SuggestCollisionLOD(thisRect);
        if (minLod == -1)
        {
            // outside all lods, nothing to update!
            return;
        }

        _mat.SetFloat("_SimDeltaTime", Mathf.Min(Crest.LodDataPersistent.MAX_SIM_DELTA_TIME, Time.deltaTime));
    }
}
