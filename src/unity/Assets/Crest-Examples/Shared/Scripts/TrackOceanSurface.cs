using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Takes an undisplaced position at the sea level, and then moves this gameobject to the point that the ocean would displace it to.
    /// </summary>
    public class TrackOceanSurface : MonoBehaviour
    {
        public Vector3 _basePosition;

        private void Start()
        {
            _basePosition.y = OceanRenderer.Instance.SeaLevel;

            Place();
        }

        void Update()
        {
            Place();
        }

        void Place()
        {
            Vector3 disp;
            OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref _basePosition, out disp, 0f);
            transform.position = _basePosition + disp;
        }
    }
}
