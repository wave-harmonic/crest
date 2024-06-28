// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Object Water Interaction Adaptor")]
    public class ObjectWaterInteractionAdaptor : FloatingObjectBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public override float ObjectWidth => 0f;

        public override bool InWater => _hasWaterData ? transform.position.y - _height <= 0f : false;

        public override Vector3 Velocity => _hasVelocity ? _velocity : Vector3.zero;

        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

        float _height = -float.MaxValue;

        Vector3 _velocity;
        Vector3 _lastPos;

        bool _hasWaterData = false;
        bool _hasVelocity = false;

        private void Update()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            _sampleHeightHelper.Init(transform.position, ObjectWidth, true);

            if (_sampleHeightHelper.Sample(out Vector3 displacement, out var _, out var _))
            {
                _hasWaterData = true;

                _height = OceanRenderer.Instance.SeaLevel + displacement.y;
            }

            if (Time.deltaTime > 0.00001f)
            {
                if (!_hasVelocity)
                {
                    _lastPos = transform.position;
                }

                _velocity = (transform.position - _lastPos) / Time.deltaTime;
                _lastPos = transform.position;

                _hasVelocity = true;
            }
        }
    }
}
