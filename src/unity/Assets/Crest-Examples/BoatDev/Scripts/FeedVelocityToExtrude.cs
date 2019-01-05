// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

public class FeedVelocityToExtrude : MonoBehaviour
{
    [HideInInspector]
    public Vector3 _localOffset;

    [Range(0f, 10f), SerializeField]
    float _noiseFreq = 6f;

    [Range(0f, 1f), SerializeField]
    float _noiseAmp = 0.5f;

    [Range(0f, 20f), SerializeField]
    float _weight = 6f;
    [Range(0f, 2f), SerializeField]
    float _weightUpDownMul = 0.5f;

    [Tooltip("Teleport speed (km/h) - if the calculated speed is larger than this amount, the object is deemed to have teleported and the computed velocity is discarded."), SerializeField]
    float _teleportSpeed = 500f;
    [SerializeField]
    bool _warnOnTeleport = false;
    [Tooltip("Maximum speed clamp (km/h), useful for controlling/limiting wake."), SerializeField]
    float _maxSpeed = 100f;
    [SerializeField]
    bool _warnOnSpeedClamp = false;

    [SerializeField]
    float _velocityPositionOffset = 0.2f;

    Material _mat;
    IBoat _boat;
    Vector3 _posLast;
    SamplingData _samplingDataFlow = new SamplingData();

    private void Start()
    {
        if (OceanRenderer.Instance == null || !OceanRenderer.Instance._createDynamicWaveSim)
        {
            enabled = false;
            return;
        }

        _localOffset = transform.localPosition;

        _mat = GetComponent<Renderer>().material;
        _boat = GetComponentInParent<IBoat>();
    }

    void LateUpdate()
    {
        // which lod is this object in (roughly)?
        var thisRect = new Rect(new Vector2(transform.position.x, transform.position.z), Vector3.zero);
        var minLod = LodDataMgrAnimWaves.SuggestDataLOD(thisRect);
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
        if (simsPresent == 0)
        {
            enabled = false;
            return;
        }

        // no sims running - abort. don't bother switching off renderer - camera wont be active
        if (simsActive == 0)
            return;

        var disp = _boat != null ? _boat.DisplacementToBoat : Vector3.zero;
        transform.position = transform.parent.TransformPoint(_localOffset) - disp + _velocityPositionOffset * _boat.RB.velocity;

        var rnd = 1f + _noiseAmp * (2f * Mathf.PerlinNoise(_noiseFreq * OceanRenderer.Instance.CurrentTime, 0.5f) - 1f);
        // feed in water velocity
        var vel = (transform.position - _posLast) / Time.deltaTime;

        if (OceanRenderer.Instance._simSettingsFlow != null &&
            OceanRenderer.Instance._simSettingsFlow._readbackData &&
            GPUReadbackFlow.Instance)
        {
            var position = transform.position;
            var samplingArea = new Rect(position.x, position.z, 0f, 0f);
            GPUReadbackFlow.Instance.GetSamplingData(ref samplingArea, _boat.BoatWidth, _samplingDataFlow);

            Vector2 surfaceFlow;
            GPUReadbackFlow.Instance.SampleFlow(ref position, _samplingDataFlow, out surfaceFlow);

            vel -= new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

            GPUReadbackFlow.Instance.ReturnSamplingData(_samplingDataFlow);
        }
        vel.y *= _weightUpDownMul;

        var speedKmh = vel.magnitude * 3.6f;
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
