// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class ObjectWaterInteractionAdaptor : FloatingObjectBase
    {
        public override float ObjectWidth => 0f;

        public override bool InWater => _hasWaterData ? transform.position.y - _height <= 0f : false;

        public override Vector3 Velocity => _hasVelocity ? _velocity : Vector3.zero;

        public override Vector3 CalculateDisplacementToObject()
        {
            return _hasWaterData ? _resultDisps[0] : Vector3.zero;
        }

        Vector3[] _queryPoints = new Vector3[1];
        Vector3[] _resultDisps = new Vector3[1];

        float _height = -float.MaxValue;

        Vector3 _velocity;
        Vector3 _lastPos;

        bool _hasWaterData = false;
        bool _hasVelocity = false;

        private void Update()
        {
            _queryPoints[0] = transform.position;
            var result = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), ObjectWidth, _queryPoints, _resultDisps, null, null);
            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(result))
            {
                _hasWaterData = true;

                _height = OceanRenderer.Instance.SeaLevel + _resultDisps[0].y;
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
