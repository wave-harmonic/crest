// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Takes an undisplaced position at the sea level, and then moves this gameobject to the point that the ocean would displace it to.
    /// </summary>
    public class TrackOceanSurface : MonoBehaviour
    {
        public Vector3 _basePosition;
        SamplingData _samplingData = new SamplingData();

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
            var collision = OceanRenderer.Instance.CollisionProvider;

            var samplingRect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
            if (!collision.GetSamplingData(ref samplingRect, 0f, _samplingData)) return;

            Vector3 disp;
            if (collision.SampleDisplacement(ref _basePosition, _samplingData, out disp))
            {
                transform.position = _basePosition + disp;
            }

            collision.ReturnSamplingData(_samplingData);
        }
    }
}
