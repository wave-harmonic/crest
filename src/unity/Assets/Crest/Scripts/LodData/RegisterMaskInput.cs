// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Assign this to depth masks - objects that will occlude the water. This ensures that the mask will render before any of the ocean surface.
    /// </summary>
    public class RegisterMaskInput : MonoBehaviour
    {
        void Start()
        {
            // Render before the surface mesh
            GetComponent<Renderer>().sortingOrder = -LodDataMgr.MAX_LOD_COUNT - 1;
        }
    }
}
