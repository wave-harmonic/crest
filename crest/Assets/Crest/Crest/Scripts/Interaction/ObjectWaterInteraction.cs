// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Drives object/water interaction - sets parameters each frame on material that renders into the dynamic wave sim.
    /// </summary>
    public class ObjectWaterInteraction : MonoBehaviour
    {
        [HideInInspector]
        public Vector3 _localOffset;

        [Range(0f, 10f), SerializeField]
        float _noiseFreq = 6f;

        [Range(0f, 1f), SerializeField]
        float _noiseAmp = 0.5f;

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

        RegisterDynWavesInput _dynWavesInput;
        FloatingObjectBase _boat;
        Vector3 _posLast;

        SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();

        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        private void Start()
        {
            if (OceanRenderer.Instance == null || !OceanRenderer.Instance.CreateDynamicWaveSim)
            {
                enabled = false;
                return;
            }

            if (transform.parent == null)
            {
                Debug.LogError("ObjectWaterInteraction script requires a parent GameObject.", this);
                enabled = false;
                return;
            }

            _localOffset = transform.localPosition;

            _dynWavesInput = GetComponent<RegisterDynWavesInput>();
            if (_dynWavesInput == null)
            {
                Debug.LogError("ObjectWaterInteraction script requires RegisterDynWavesInput script to be present.", this);
                enabled = false;
                return;
            }

            _boat = GetComponentInParent<FloatingObjectBase>();
            if (_boat == null)
            {
                _boat = transform.parent.gameObject.AddComponent<ObjectWaterInteractionAdaptor>();
            }

            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                Debug.LogError("ObjectWaterInteraction script requires Renderer component.", this);
                enabled = false;
                return;
            }

            _mpb = new MaterialPropertyBlock();
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

            var disp = _boat.CalculateDisplacementToObject();
            transform.position = transform.parent.TransformPoint(_localOffset) - disp + _velocityPositionOffset * _boat.Velocity;

            var ocean = OceanRenderer.Instance;

            var rnd = 1f + _noiseAmp * (2f * Mathf.PerlinNoise(_noiseFreq * ocean.CurrentTime, 0.5f) - 1f);
            // feed in water velocity
            var vel = (transform.position - _posLast) / ocean.DeltaTimeDynamics;
            if (ocean.DeltaTimeDynamics < 0.0001f)
            {
                vel = Vector3.zero;
            }

            if (QueryFlow.Instance)
            {
                _sampleFlowHelper.Init(transform.position, _boat.ObjectWidth);
                Vector2 surfaceFlow = Vector2.zero;
                _sampleFlowHelper.Sample(ref surfaceFlow);
                vel -= new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
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

            float dt; int steps;
            ocean._lodDataDynWaves.GetSimSubstepData(ocean.DeltaTimeDynamics, out steps, out dt);
            float weight = _boat.InWater ? 1f / simsActive : 0f;

            _renderer.GetPropertyBlock(_mpb);

            _mpb.SetVector("_Velocity", vel);
            _mpb.SetFloat("_Weight", weight);
            _mpb.SetFloat("_SimDeltaTime", dt);

            _renderer.SetPropertyBlock(_mpb);

            _posLast = transform.position;
        }
    }
}
