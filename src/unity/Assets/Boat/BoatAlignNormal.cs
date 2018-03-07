using UnityEngine;

namespace Crest
{
    public class BoatAlignNormal : MonoBehaviour
    {
        public float _bottomH = -1f;
        public bool _debugDraw = false;
        public float _overrideProbeRadius = -1f;
        public float _buoyancyCoeff = 40000f;

        public float _enginePower = 10000f;
        public float _turnPower = 100f;

        Rigidbody _rb;
        ShapeGerstnerBase _waves;

        public float _dragInWaterUp = 20000f;
        public float _dragInWaterRight = 20000f;
        public float _dragInWaterForward = 20000f;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _waves = FindObjectOfType<ShapeGerstnerBase>();
        }

        void FixedUpdate()
        {
            var position = transform.position;

            var undispPos = _waves.GetPositionDisplacedToPositionExpensive(ref position, 0f);
            var displacement = _waves.GetDisplacement(ref undispPos, 0f);
            var dispPos = undispPos + displacement;
            float height = OceanRenderer.Instance.SeaLevel + displacement.y;

            float bottomDepth = height - transform.position.y - _bottomH;

            if (bottomDepth <= 0f)
                return;

            var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
            _rb.AddForce(buoyancy, ForceMode.Acceleration);


            // apply drag relative to water
            var velWater = _waves.GetSurfaceVelocity(ref undispPos, 0f);
            _rb.AddForce(Vector3.up * Vector3.Dot(Vector3.up, (velWater - _rb.velocity)) * _dragInWaterUp, ForceMode.Acceleration);
            _rb.AddForce(Vector3.right * Vector3.Dot(Vector3.right, (velWater - _rb.velocity)) * _dragInWaterRight, ForceMode.Acceleration);
            _rb.AddForce(Vector3.forward * Vector3.Dot(Vector3.forward, (velWater - _rb.velocity)) * _dragInWaterForward, ForceMode.Acceleration);

            float forward = Input.GetAxis("Vertical");
            _rb.AddForce(transform.forward * _enginePower * forward, ForceMode.Acceleration);

            float sideways = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
            _rb.AddTorque(transform.up * _turnPower * sideways, ForceMode.Acceleration);

            // align to normal
            var current = transform.up;
            Debug.DrawLine(dispPos, dispPos + _waves.GetNormal(ref undispPos, 0f), Color.white);
            //Debug.DrawLine(dispPos, undispPos, Color.white * 0.7f);
        }
    }
}
