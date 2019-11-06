// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Tries to dynamically batch height query points on the fly. 
    /// </summary>
    public class DWPAdaptor
    {
        class QueryToken { }
        List<QueryToken> _tokens = new List<QueryToken>();

        int _queryCount = 0;

        SamplingData _samplingData = new SamplingData();

        Vector3[] _queryScratch = new Vector3[] { Vector3.zero };
        float[] _resultsScratch = new float[] { 0f };

        [Tooltip("Width of object (for a 2m x 4m boat, set this to 2). The larger this value, the more filtered/smooth the wave response will be."), SerializeField]
        float _minSpatialLength = 0f;

        public DWPAdaptor()
        {
            _samplingData._minSpatialLength = _minSpatialLength;
        }

        public float GetWaterHeight(Vector3 position)
        {
            _queryCount++;

            // Create token object for the query if needed
            if (_tokens.Count < _queryCount)
            {
                _tokens.Add(new QueryToken());
            }

            _queryScratch[0] = position;
            var result = OceanRenderer.Instance.CollisionProvider.Query(
                _tokens[_queryCount - 1].GetHashCode(), _samplingData, _queryScratch, _resultsScratch, null, null
                );

            return _resultsScratch[0];
        }

        // Must be called after queries are made
        public void Update()
        {
            _queryCount = 0;
        }
    }
}
