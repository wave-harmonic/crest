// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Provides a cache layer on top of a collision provider, with the specific purpose of caching height requests.
    /// Sampling water height in Crest involves inverting a displacement texture on the fly, so using a cache gives
    /// a big speedup. Code based on pull request #14 from user dizzy2003.
    /// </summary>
    public class CollProviderCache : ICollProvider
    {
        public float _cacheBucketSize = 0.1f;

        ICollProvider _collProvider;
        int _cacheHits, _cacheHitsLastFrame;
        int _cacheChecks, _cacheChecksLastFrame;
        float _cacheBucketSizeRecip = 0f;

        readonly Dictionary<uint, float> _waterHeightCache = new Dictionary<uint, float>();

        public CollProviderCache(ICollProvider collProvider)
        {
            _collProvider = collProvider;
        }

        public void ClearCache()
        {
            _cacheBucketSizeRecip = 1f / Mathf.Max(_cacheBucketSize, 0.00001f);

            _cacheChecksLastFrame = _cacheChecks;
            _cacheChecks = 0;
            _cacheHitsLastFrame = _cacheHits;
            _cacheHits = 0;

            _waterHeightCache.Clear();
        }

        uint CalcHash(Vector3 wp)
        {
            int x = (int)(wp.x * _cacheBucketSizeRecip);
            int z = (int)(wp.z * _cacheBucketSizeRecip);
            return (uint)(x + 32768 + ((z + 32768) << 16));
        }

        // displacement queries are not cached - only the height computations
        public bool SampleDisplacement(Vector3 worldPos, out Vector3 displacement)
        {
            return _collProvider.SampleDisplacement(worldPos, out displacement);
        }
        public bool SampleDisplacement(Vector3 worldPos, out Vector3 displacement, float minSpatialLength)
        {
            return _collProvider.SampleDisplacement(worldPos, out displacement, minSpatialLength);
        }

        public bool SampleHeight(Vector3 worldPos, out float height)
        {
            var hash = CalcHash(worldPos);

            _cacheChecks++;
            if (_waterHeightCache.TryGetValue(hash, out height))
            {
                // got it from the cache!
                _cacheHits++;
                return true;
            }

            // compute the height
            bool success = _collProvider.SampleHeight(worldPos, out height);

            // populate cache (regardless of success for now)
            _waterHeightCache.Add(hash, height);

            return success;
        }

        public void PrewarmForSamplingArea(Rect areaXZ)
        {
            // nothing to do here
        }
        public void PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            // nothing to do here
        }
        public bool SampleDisplacementInArea(Vector3 worldPos, out Vector3 displacement)
        {
            return _collProvider.SampleDisplacement(worldPos, out displacement);
        }
        public bool SampleHeightInArea(Vector3 worldPos, out float height)
        {
            return SampleHeight(worldPos, out height);
        }

        public bool SampleNormal(Vector3 undisplacedWorldPos, out Vector3 normal)
        {
            return _collProvider.SampleNormal(undisplacedWorldPos, out normal);
        }
        public bool SampleNormal(Vector3 undisplacedWorldPos, out Vector3 normal, float minSpatialLength)
        {
            return _collProvider.SampleNormal(undisplacedWorldPos, out normal, minSpatialLength);
        }

        public bool ComputeUndisplacedPosition(Vector3 worldPos, out Vector3 undisplacedWorldPos)
        {
            return _collProvider.ComputeUndisplacedPosition(worldPos, out undisplacedWorldPos);
        }

        public int CacheChecks { get { return _cacheChecksLastFrame; } }
        public int CacheHits { get { return _cacheHitsLastFrame; } }
    }
}
