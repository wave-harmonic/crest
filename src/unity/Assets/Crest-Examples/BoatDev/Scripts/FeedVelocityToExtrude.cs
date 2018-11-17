// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

public class FeedVelocityToExtrude : MonoBehaviour {

    public BoatAlignNormal _boat;

    Vector3 _posLast;

    [HideInInspector]
    public Vector3 _localOffset;

    [Range(0f, 10f)]
    public float _noiseFreq = 6f;

    [Range(0f, 1f)]
    public float _noiseAmp = 0.5f;

    [Range(0f, 20f)]
    public float _weight = 6f;
    [Range(0f, 2f)]
    public float _weightUpDownMul = 0.5f;

    [Tooltip("Teleport speed (km/h) - if the calculated speed is larger than this amount, the object is deemed to have teleported and the computed velocity is discarded.")]
    public float _teleportSpeed = 500f;
    public bool _warnOnTeleport = false;
    [Tooltip("Maximum speed clamp (km/h), useful for controlling/limiting wake.")]
    public float _maxSpeed = 100f;
    public bool _warnOnSpeedClamp = false;

    Material _mat;

    private void Start()
    {
        if (OceanRenderer.Instance == null || !OceanRenderer.Instance._createDynamicWaveSim)
        {
            enabled = false;
            return;
        }

        _localOffset = transform.localPosition;

        _mat = GetComponent<Renderer>().material;
    }

    void LateUpdate()
    {
        // which lod is this object in (roughly)?
        Rect thisRect = new Rect(new Vector2(transform.position.x, transform.position.z), Vector3.zero);
        int minLod = LodDataMgrAnimWaves.SuggestDataLOD(thisRect);
        if (minLod == -1)
        {
            // outside all lods, nothing to update!
            return;
        }

        // how many active wave sims currently apply to this object - ideally this would eliminate sims that are too
        // low res, by providing a max grid size param
        int simsPresent, simsActive;
        LodDataMgrDynWaves.CountWaveSims(minLod, out simsPresent, out simsActive);

        // counting non-existent sims is expensive - stop updating if none found
        if(simsPresent == 0)
        {
            enabled = false;
            return;
        }

        // no sims running - abort. don't bother switching off renderer - camera wont be active
        if (simsActive == 0)
            return;

        var disp = _boat ? _boat.DisplacementToBoat : Vector3.zero;
        transform.position = transform.parent.TransformPoint(_localOffset) - disp;

        float rnd = 1f + _noiseAmp * (2f * Mathf.PerlinNoise(_noiseFreq * OceanRenderer.Instance.CurrentTime, 0.5f) - 1f);
        // feed in water velocity
        Vector3 vel = (transform.position - _posLast) / Time.deltaTime;

        if (OceanRenderer.Instance._simSettingsFlow != null &&
            OceanRenderer.Instance._simSettingsFlow._readbackData &&
            GPUReadbackFlow.Instance)
        {
            Vector2 surfaceFlow;
            Vector3 position = transform.position;
            GPUReadbackFlow.Instance.SampleFlow(ref position, out surfaceFlow, _boat._boatWidth);
            vel -= new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
        }
        vel.y *= _weightUpDownMul;

        float speedKmh = vel.magnitude * 3.6f;
        if (speedKmh > _teleportSpeed)
        {
            // teleport detected
            vel *= 0f;

            if (_warnOnTeleport)
            {
                Debug.LogWarning("Teleport detected (speed = " + speedKmh.ToString() + "), velocity discarded.", this);
            }
        }
        else if (speedKmh > _maxSpeed)
        {
            // limit speed to max
            vel *= _maxSpeed / speedKmh;

            if (_warnOnSpeedClamp)
            {
                Debug.LogWarning("Speed (" + speedKmh.ToString() + ") exceeded max limited, clamped.", this);
            }
        }

        _mat.SetVector("_Velocity", rnd * vel);
        _posLast = transform.position;

        _mat.SetFloat("_Weight", (_boat == null || _boat.InWater) ? _weight / simsActive : 0f);

        _mat.SetFloat("_SimDeltaTime", OceanRenderer.Instance._lodDataDynWaves.SimDeltaTime);
    }
}
