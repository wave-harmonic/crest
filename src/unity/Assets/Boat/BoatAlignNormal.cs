using UnityEngine;

namespace Crest
{
    public class BoatAlignNormal : MonoBehaviour
    {
        public float _bottomH = -1f;
        public bool _debugDraw = false;
        public float _overrideProbeRadius = -1f;
        public float _buoyancyCoeff = 40000f;
        public float _boyancyTorque = 2f;

        public float _forceHeightOffset = -1f;
        public float _enginePower = 10000f;
        public float _turnPower = 100f;

        public float _boatWidth = 2f;

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

            int minIdx = _waves.GetFirstComponentIndex(_boatWidth);
            var undispPos = _waves.GetPositionDisplacedToPositionExpensive(ref position, 0f, minIdx);
            var displacement = _waves.GetDisplacement(ref undispPos, 0f, minIdx);
            var normal = _waves.GetNormal(ref undispPos, 0f, minIdx);
            var velWater = _waves.GetSurfaceVelocity(ref undispPos, 0f, minIdx);

            var dispPos = undispPos + displacement;
            float height = dispPos.y;

            float bottomDepth = height - transform.position.y - _bottomH;

            if (bottomDepth <= 0f)
                return;

            var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
            _rb.AddForce(buoyancy, ForceMode.Acceleration);


            // apply drag relative to water
            var forcePosition = _rb.position + _forceHeightOffset * Vector3.up;
            _rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, (velWater - _rb.velocity)) * _dragInWaterUp, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(Vector3.right * Vector3.Dot(Vector3.right, (velWater - _rb.velocity)) * _dragInWaterRight, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(Vector3.forward * Vector3.Dot(Vector3.forward, (velWater - _rb.velocity)) * _dragInWaterForward, forcePosition, ForceMode.Acceleration);

            float forward = Input.GetAxis("Vertical");
            _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);

            float sideways = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
            _rb.AddTorque(transform.up * _turnPower * sideways, ForceMode.Acceleration);

            // align to normal
            var current = transform.up;
            var target = normal;
            var torque = Vector3.Cross(current, target);
            _rb.AddTorque(torque * _boyancyTorque, ForceMode.Acceleration);
        }
    }
}
