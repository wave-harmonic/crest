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
            // Todo - garbage
            Vector3[] queryPoints = new Vector3[] { _basePosition };
            Vector3[] results = new Vector3[1];

            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), 0f, queryPoints, results, null, null)))
            {
                transform.position = _basePosition + results[0];
            }
        }
    }
}
