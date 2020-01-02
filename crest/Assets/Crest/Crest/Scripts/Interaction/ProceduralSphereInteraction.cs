using UnityEngine;

namespace Crest
{
    public class ProceduralSphereInteraction : MonoBehaviour
    {
        float Radius => 0.5f * transform.lossyScale.x;

        [Range(0f, 10f), SerializeField]
        float _noiseFreq = 6f;

        [Range(0f, 1f), SerializeField]
        float _noiseAmp = 0.5f;

        [Range(-1f, 1f), SerializeField]
        float _weight = 1f;
        [Range(0f, 2f), SerializeField]
        float _weightUpDownMul = 0.5f;

        [SerializeField]
        float _velocityPositionOffset = 0.2f;

        [Header("Limits")]
        [Tooltip("Teleport speed (km/h) - if the calculated speed is larger than this amount, the object is deemed to have teleported and the computed velocity is discarded."), SerializeField]
        float _teleportSpeed = 500f;
        [SerializeField]
        bool _warnOnTeleport = false;
        [Tooltip("Maximum speed clamp (km/h), useful for controlling/limiting wake."), SerializeField]
        float _maxSpeed = 100f;
        [SerializeField]
        bool _warnOnSpeedClamp = false;

        RegisterDynWavesInput _dynWavesInput;
        FloatingObjectBase _boat;

        Vector3 _localPositionRest;
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

            _localPositionRest = transform.localPosition;

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

            // No sims running - abort. don't bother switching off renderer - camera wont be active
            if (simsActive == 0)
                return;

            var disp = _boat.CalculateDisplacementToObject();

            // Set position of interaction
            {
                var dispFlatLand = disp;
                dispFlatLand.y = 0f;
                var velBoat = _boat.Velocity;
                velBoat.y = 0f;
                transform.position = transform.parent.TransformPoint(_localPositionRest) - dispFlatLand + _velocityPositionOffset * velBoat;
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

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

            float weight = _weight / simsActive;

            // Weight based on submerged-amount of object
            {
                var waterHeight = disp.y + OceanRenderer.Instance.SeaLevel;
                var centerDepthInWater = waterHeight - transform.position.y;

                if (centerDepthInWater >= 0f)
                {
                    // Center in water
                    var prop = centerDepthInWater / Radius;
                    prop *= 0.5f;
                    weight *= Mathf.Exp(-prop * prop);
                }
                else
                {
                    // Center out of water
                    var height = -centerDepthInWater;
                    var heightProp = 1f - Mathf.Clamp01(height / Radius);
                    weight *= Mathf.Sqrt(heightProp);
                }
            }

            _renderer.GetPropertyBlock(_mpb);

            _mpb.SetVector("_Velocity", vel);
            _mpb.SetFloat("_Weight", weight);
            _mpb.SetFloat("_SimDeltaTime", dt);
            _mpb.SetFloat("_Radius", Radius);

            _renderer.SetPropertyBlock(_mpb);

            _posLast = transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
