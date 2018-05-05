// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest.Placement
{
    /// <summary>
    /// Positions this GameObject at a flatland position which shares XZ coordinates with a target transform.
    /// </summary>
    public class PlaceUnderTransform : MonoBehaviour
    {
        public Transform _viewpoint;

        // the script execution order ensures this executes early in the ocean late update
        void LateUpdate()
        {
            Vector3 pos = _viewpoint.position;

            // maintain y coordinate - sea level
            pos.y = transform.position.y;

            transform.position = pos;
        }
    }
}
