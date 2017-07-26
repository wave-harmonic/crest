// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace OceanResearch
{
    /// <summary>
    /// Offsets this gameobject from a provided viewer.
    /// </summary>
    public class SphereOffset : MonoBehaviour
    {
        public float _radiusMultiplier = 2f;
        public Transform _viewpoint;

        float _radius;

        // the script execution order ensures this executes before WaveDataCam::LateUpdate and 
        void LateUpdate()
        {
            // radius is altitude difference
            _radius = Mathf.Abs( transform.position.y - _viewpoint.position.y );

            // multiply up (optional)
            _radius *= _radiusMultiplier;

            Vector3 pos = _viewpoint.position + _viewpoint.forward * _radius;

            // maintain y coordinate - sea level
            pos.y = transform.position.y;

            transform.position = pos;
        }

#if UNITY_EDITOR
        public bool _debugDraw = false;
        void OnDrawGizmos()
        {
            if( _debugDraw && UnityEditor.EditorApplication.isPlaying )
            {
                UnityEditor.Handles.DrawWireDisc( _viewpoint.position, _viewpoint.right, _radius );
                UnityEditor.Handles.DrawLine( _viewpoint.position, _viewpoint.position + _radius * _viewpoint.forward );
                UnityEditor.Handles.DrawLine( _viewpoint.position + _radius * _viewpoint.forward, transform.position );
            }
        }
#endif
    }
}
