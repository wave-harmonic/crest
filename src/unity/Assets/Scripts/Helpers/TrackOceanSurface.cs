using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Takes an undisplaced position at the sea level, and then moves this gameobject to the point that the ocean would displace it to.
    /// </summary>
    public class TrackOceanSurface : MonoBehaviour
    {
        public Vector3 _basePosition;
        ShapeGerstnerBatched _gerstner;

        private void Start()
        {
            _basePosition.y = 0f;
            _gerstner = FindObjectOfType<ShapeGerstnerBatched>();
            transform.position = _basePosition + _gerstner.GetDisplacement(_basePosition, 0f);
        }

        void Update()
        {
            transform.position = _basePosition + _gerstner.GetDisplacement(_basePosition, 0f);
        }
    }
}
