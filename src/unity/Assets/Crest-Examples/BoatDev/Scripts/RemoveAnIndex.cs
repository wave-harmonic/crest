using UnityEngine;

public class RemoveAnIndex : MonoBehaviour {

	void Start () {
        var mf = GetComponent<MeshFilter>();
        var indices = mf.mesh.GetIndices(0);
        var indices2 = new int[indices.Length - 3];

        for (int i = 0; i < indices2.Length; i++)
            indices2[i] = indices[i];

        mf.mesh.SetIndices(indices2, mf.mesh.GetTopology(0), 0);
	}
	
	void Update () {
		
	}
}
