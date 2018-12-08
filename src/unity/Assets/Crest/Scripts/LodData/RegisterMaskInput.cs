using UnityEngine;

namespace Crest
{
    public class RegisterMaskInput : MonoBehaviour
    {
        void Start()
        {
            // Render before the surface mesh
            GetComponent<Renderer>().sortingOrder = -LodDataMgr.MAX_LOD_COUNT - 1;
        }
    }
}
