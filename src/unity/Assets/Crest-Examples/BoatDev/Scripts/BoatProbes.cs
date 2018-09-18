using UnityEngine;

namespace Crest
{
    [System.Serializable]
    public class BuoyancyProbeSphere
    {
        public Vector3 _localPosition;
        public float _r = 1f;

        //[HideInInspector]
        //public float _volume = 1f;
        [HideInInspector]
        public Vector3 _position;
        [HideInInspector]
        public float _waterDensity = 1000f;
        [HideInInspector]
        public float _dragInWater = 370f;

        public void ApplyForceToRB(ShapeGerstnerBatched _waves, Rigidbody _rb)
        {
            Vector3 undispPos = _waves.GetPositionDisplacedToPosition(_position, 0f);
            Vector3 displacement = _waves.SampleDisplacement(undispPos, 0f);
            float height = OceanRenderer.Instance.SeaLevel + displacement.y;

            Vector3 onSeaLevel = _position;
            onSeaLevel.y = OceanRenderer.Instance.SeaLevel;

            // h is the height of the water relative to the center of the sphere
            float h = height - _position.y + _r;
            h = Mathf.Min(h, 2f * _r);

            float submergedness = Mathf.Clamp01(h / (2f * _r));
            if (submergedness < 0.001f)
            {
                // not in water
                return;
            }

            // volume of spherical cap, from https://en.wikipedia.org/wiki/Spherical_cap
            float Vdisp = Mathf.PI * h * h * (3f * _r - h) / 3f;
            // scale up to full volume
            //float Vfull = 4f * Mathf.PI * _r * _r * _r / 3f;
            float V = Vdisp;// _volume * Vdisp / Vfull;

            float rotAmt = .1f;

            float F = V * _waterDensity * Physics.gravity.magnitude;
            _rb.AddForceAtPosition(-F * Physics.gravity.normalized, Vector3.Lerp(_rb.position, _position, rotAmt));
            Debug.DrawLine(_position, _position - F * Physics.gravity.normalized, Color.red * 0.5f);

            // apply drag relative to water
            var vel = _waves.GetSurfaceVelocity(undispPos, 0f);
            var deltaV = _rb.velocity - vel;
            // approximation - interpolate drag based on how submerged the sphere is
            _rb.AddForceAtPosition(-submergedness * _dragInWater * deltaV, Vector3.Lerp(_rb.position, _position, rotAmt));
            Debug.DrawLine(_position, _position - submergedness * _dragInWater * deltaV, Color.white);
        }

        public void DebugDraw()
        {
            Debug.DrawLine(_position - Vector3.up * _r, _position + Vector3.up * _r);
            Debug.DrawLine(_position - Vector3.forward * _r, _position + Vector3.forward * _r);
            Debug.DrawLine(_position - Vector3.right * _r, _position + Vector3.right * _r);
        }
    }

    public class BoatProbes : MonoBehaviour
    {
        public BuoyancyProbeSphere[] _probes;

        [Delayed]
        public float _densityRelativeToSeaWater = 1f;
        public bool _debugDraw = false;
        [Delayed]
        public float _volume = 100f;
        public float _probePositionMultiplier = 1.5f;

        const float SEA_WATER_DENSITY = 1023.6f; // kg/m3 - depends on conditions - https://en.wikipedia.org/wiki/Seawater

        public float _overrideProbeRadius = -1f;

        Rigidbody _rb;
        ShapeGerstnerBatched _waves;

        public float _dragInWater = 370f;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _waves = FindObjectOfType<ShapeGerstnerBatched>();
        }

        void FixedUpdate()
        {
            //float r = 0.5f * transform.localScale.x;
            //float V = 4f * Mathf.PI * r * r * r / 3f;
            //_rb.mass = _densityRelativeToSeaWater * SEA_WATER_DENSITY * V;
            _rb.mass = _volume * _densityRelativeToSeaWater * SEA_WATER_DENSITY;
            //_rb.ResetInertiaTensor();
            //_rb.inertiaTensor *= 4f;

            //float probeVolume = _volume / _probes.Length;

            foreach (var probe in _probes)
            {
                if (_overrideProbeRadius != -1f)
                {
                    probe._r = _overrideProbeRadius;
                }

                probe._dragInWater = _dragInWater;
                probe._waterDensity = SEA_WATER_DENSITY;
                probe._position = transform.TransformPoint(_probePositionMultiplier * probe._localPosition);

                probe.ApplyForceToRB(_waves, _rb);

                if (_debugDraw)
                {
                    probe.DebugDraw();
                }
            }
        }
    }
}
