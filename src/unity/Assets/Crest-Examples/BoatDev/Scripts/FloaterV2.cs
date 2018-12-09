using Crest;
using System;
using UnityEngine;

public class FloaterV2 : MonoBehaviour
{
    public Vector3 CenterOfMass;
    public FloaterForcePoints[] ForcePoints;

    [SerializeField] bool _playerControlled = true;
    [SerializeField] float EnginePower = 10;
    [SerializeField] float TurnPower = 0.5f;

    private const float WATER_DENSITY = 1000;

    public float _minSpatialLength = 12f;
    public float _forceMultiplier = 10f;

    Rigidbody _rigidBody;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();


        if (_rigidBody != null)
            _rigidBody.centerOfMass = CenterOfMass;
    }

    private void FixedUpdate()
    {
        if (GPUReadbackDisps.Instance)
        {
            GPUReadbackDisps.Instance.ProcessRequests();
        }

        FixedUpdateEngine();

        FixedUpdateBuoyancy();
    }

    void FixedUpdateEngine()
    {
        if (!_playerControlled)
            return;

        var forcePosition = _rigidBody.position;

        var forward = Input.GetAxis("Vertical");
        _rigidBody.AddForceAtPosition(transform.forward * EnginePower * forward, forcePosition, ForceMode.Acceleration);

        var sideways = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);

        Vector3 heel = transform.forward;

        _rigidBody.AddTorque((transform.up + heel) * TurnPower * sideways, ForceMode.Acceleration);
    }

    void FixedUpdateBuoyancy()
    {
        float archimedesForceMagnitude = WATER_DENSITY * Mathf.Abs(Physics.gravity.y);
        var collProvider = OceanRenderer.Instance.CollisionProvider;

        for (int i = 0; i < ForcePoints.Length; i++)
        {
            FloaterForcePoints point = ForcePoints[i];

            Vector3 transformedPoint = transform.TransformPoint(point.OffSetPosition + new Vector3(0, CenterOfMass.y, 0));

            Vector3 undispPos;
            if (!collProvider.ComputeUndisplacedPosition(ref transformedPoint, out undispPos, _minSpatialLength))
            {
                // If we couldn't get wave shape, assume flat water at sea level
                undispPos = transformedPoint;
                undispPos.y = OceanRenderer.Instance.SeaLevel;
            }

            var waterSurfaceVel = Vector3.zero;

            bool dispValid, velValid;
            collProvider.SampleDisplacementVel(ref undispPos, out point.Displaced, out dispValid, out waterSurfaceVel, out velValid, _minSpatialLength);

            var dispPos = undispPos + point.Displaced;

            float height;
            collProvider.SampleHeight(ref transformedPoint, out height, _minSpatialLength);

            float distance = dispPos.y - transformedPoint.y;

            if (height - transformedPoint.y > 0)
            {
                _rigidBody.AddForceAtPosition(archimedesForceMagnitude * distance * Vector3.up * point.Factor * _forceMultiplier, transformedPoint);
            }
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < ForcePoints.Length; i++)
        {
            FloaterForcePoints point = ForcePoints[i];

            Vector3 transformedPoint = transform.TransformPoint(point.OffSetPosition + new Vector3(0, CenterOfMass.y, 0));

            Gizmos.color = Color.red;
            Gizmos.DrawCube(transformedPoint, Vector3.one * 0.5f);
        }
    }
}

[Serializable]
public class FloaterForcePoints
{
    public float Factor;

    public Vector3 OffSetPosition;

    [System.NonSerialized]
    public Vector3 Displaced;
}
