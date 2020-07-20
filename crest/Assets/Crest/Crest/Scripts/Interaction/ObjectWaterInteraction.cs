// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Drives object/water interaction - sets parameters each frame on material that renders into the dynamic wave sim.
    /// </summary>
    public partial class ObjectWaterInteraction : MonoBehaviour
    {
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

        FloatingObjectBase _boat;
        Vector3 _posLast;
        Vector3 _localOffset;

        SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();

        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        private void Start()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

#if UNITY_EDITOR
            if (EditorApplication.isPlaying && !Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                enabled = false;
                return;
            }
#endif

            _localOffset = transform.localPosition;
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();

            _boat = GetComponentInParent<FloatingObjectBase>();
            if (_boat == null)
            {
                _boat = transform.parent.gameObject.AddComponent<ObjectWaterInteractionAdaptor>();
            }
        }

        void LateUpdate()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

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

            transform.position = transform.parent.TransformPoint(_localOffset) + _velocityPositionOffset * _boat.Velocity;

            var ocean = OceanRenderer.Instance;

            // feed in water velocity
            var vel = (transform.position - _posLast) / ocean.DeltaTimeDynamics;
            if (ocean.DeltaTimeDynamics < 0.0001f)
            {
                vel = Vector3.zero;
            }

            {
                _sampleFlowHelper.Init(transform.position, _boat.ObjectWidth);
                _sampleFlowHelper.Sample(out var surfaceFlow);
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

            float dt;
            ocean._lodDataDynWaves.GetSimSubstepData(ocean.DeltaTimeDynamics, out _, out dt);
            float weight = _boat.InWater ? 1f / simsActive : 0f;

            _renderer.GetPropertyBlock(_mpb);

            _mpb.SetVector("_Velocity", vel);
            _mpb.SetFloat("_Weight", weight);
            _mpb.SetFloat("_SimDeltaTime", dt);

            _renderer.SetPropertyBlock(_mpb);

            _posLast = transform.position;
        }
    }

#if UNITY_EDITOR
    public partial class ObjectWaterInteraction : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (!ocean.CreateDynamicWaveSim)
            {
                showMessage
                (
                    "<i>ObjectWaterInteraction</i> requires dynamic wave simulation to be enabled on <i>OceanRenderer</i>.",
                    ValidatedHelper.MessageType.Error, ocean
                );

                isValid = false;
            }

            if (transform.parent == null)
            {
                showMessage
                (
                    "<i>ObjectWaterInteraction</i> script requires a parent <i>GameObject</i>.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }

            if (GetComponent<RegisterDynWavesInput>() == null)
            {
                showMessage
                (
                    "<i>ObjectWaterInteraction</i> script requires <i>RegisterDynWavesInput</i> script to be present.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }

            if (GetComponent<Renderer>() == null)
            {
                showMessage
                (
                    "<i>ObjectWaterInteraction</i> script requires <i>Renderer</i> component.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }

            return isValid;
        }

        [CustomEditor(typeof(ObjectWaterInteraction), true), CanEditMultipleObjects]
        class ObjectWaterInteractionEditor : ValidatedEditor { }
    }
#endif
}
