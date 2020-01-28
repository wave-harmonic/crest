// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
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
            return _hasWaterData ? _resultDisp : Vector3.zero;
        }

        float _height = -float.MaxValue;

        Vector3 _velocity;
        Vector3 _lastPos;
        Vector3 _resultDisp;

        bool _hasWaterData = false;
        bool _hasVelocity = false;

        private void Update()
        {
            int result;
            {
                NativeArray<Vector3> queryPoints = new NativeArray<Vector3>(1, Allocator.Temp);
                NativeArray<Vector3> resultDisps = new NativeArray<Vector3>(1, Allocator.Temp);
                queryPoints[0] = transform.position;
                result = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), ObjectWidth, queryPoints, resultDisps, default, default);
                _resultDisp = resultDisps[0];
                resultDisps.Dispose();
                queryPoints.Dispose();
            }

            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(result))
            {
                _hasWaterData = true;

                _height = OceanRenderer.Instance.SeaLevel + _resultDisp.y;
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
