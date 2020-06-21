// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class EmitParticlesWhenSubmerged : MonoBehaviour
    {
        [SerializeField] float _minVel = 0.1f;
        [SerializeField] AnimationCurve _initalVelVsWaterVel2;

        ParticleSystem _particleSystem;
        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

        float _lastHeight = -1e5f;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null)
            {
                Debug.LogError("No particle system attached, disabling EmitParticlesWhenSubmerged.", this);
                enabled = false;
                return;
            }
        }

        void Update()
        {
            if (_particleSystem.isEmitting)
                return;

            // Assume a primitive like a sphere or box.
            _sampleHeightHelper.Init(transform.position, 1f);

            float height = 0f;
            var dummy = Vector3.zero;
            var vel = Vector3.zero;
            if (_sampleHeightHelper.Sample(ref height, ref dummy, ref vel))
            {
                if (height > transform.position.y && _lastHeight <= transform.position.y && vel.y > _minVel)
                {
                    _particleSystem.Play();

                    var module = _particleSystem.main;
                    module.startSpeedMultiplier = _initalVelVsWaterVel2.Evaluate(vel.y / _minVel);
                    //Debug.Log("Start speed multiplier: " + _initalVelVsWaterVel2.Evaluate(vel.y / _minVel));
                }
                _lastHeight = height;
            }
        }
    }
}
