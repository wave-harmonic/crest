// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

public class Buoy : MonoBehaviour
{
    [SerializeField] float _bottomH = -2f;
    [SerializeField] float _buoyancyCoeff = 2f;
    [SerializeField] float _dragInWater = 1f;

    [SerializeField] float _boatWidth = 1f;

    [SerializeField] bool _debugDrawSurroundingColl = false;
    [SerializeField] bool _debugValidateCollision = true;

    Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Trigger processing of displacement textures that have come back this frame. This will be processed
        // anyway in Update(), but FixedUpdate() is earlier so make sure it's up to date now.
        if (GPUReadbackDisps.Instance)
        {
            GPUReadbackDisps.Instance.ProcessRequests();
        }

        var collProvider = OceanRenderer.Instance.CollisionProvider;
        var position = transform.position;

        if (_debugValidateCollision)
        {
            var result = collProvider.CheckAvailability(ref position, _boatWidth);
            if (result != AvailabilityResult.DataAvailable)
            {
                Debug.LogWarning("Validation failed: " + result.ToString() + ". See comments on the AvailabilityResult enum.", this);
            }
        }


        float height;
        collProvider.SampleHeight(ref position, out height, _boatWidth);

        float bottomDepth = height - transform.position.y - _bottomH;

        bool inWater = bottomDepth > 0f;
        if (!inWater)
        {
            return;
        }

        var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
        _rb.AddForce(buoyancy, ForceMode.Acceleration);


        // apply drag relative to water
        var forcePosition = _rb.position;
        _rb.AddForceAtPosition(-_rb.velocity * _dragInWater, forcePosition, ForceMode.Acceleration);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (_debugDrawSurroundingColl)
        {
            UpdateDebugDrawSurroundingColl();
        }
    }

    private void UpdateDebugDrawSurroundingColl()
    {
        float r = 5f;
        float steps = 10;
        for (float i = 0; i < steps; i++)
        {
            for (float j = 0; j < steps; j++)
            {
                Vector3 pos = new Vector3(((i + 0.5f) - steps / 2f) * r, 0f, ((j + 0.5f) - steps / 2f) * r);
                pos.x += transform.position.x;
                pos.z += transform.position.z;

                Vector3 disp;
                if (OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref pos, out disp, _boatWidth))
                {
                    DebugDrawCross(pos + disp, 1f, Color.green);
                }
                else
                {
                    DebugDrawCross(pos, 0.25f, Color.red);
                }
            }
        }
    }

    void DebugDrawCross(Vector3 pos, float r, Color col)
    {
        Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col);
        Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col);
        Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col);
    }
#endif
}
