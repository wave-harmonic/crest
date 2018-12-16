// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
