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
        int _cacheHit;
        int _cacheCheck;
        float _cacheBucketSizeRecip = 0f;

        readonly Dictionary<uint, float> _waterHeightCache = new Dictionary<uint, float>();

        public CollProviderCache(ICollProvider collProvider)
        {
            _collProvider = collProvider;
        }

        public void ClearCache()
        {
            _cacheBucketSizeRecip = 1f / Mathf.Max(_cacheBucketSize, 0.00001f);

            _cacheCheck = _cacheHit = 0;

            _waterHeightCache.Clear();
        }

        uint CalcHash(ref Vector3 wp)
        {
            int x = (int)(wp.x * _cacheBucketSizeRecip);
            int z = (int)(wp.z * _cacheBucketSizeRecip);
            return (uint)(x + 32768 + ((z + 32768) << 16));
        }

        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            return _collProvider.SampleDisplacement(ref worldPos, ref displacement);
        }

        public bool SampleHeight(ref Vector3 worldPos, ref float height)
        {
            var hash = CalcHash(ref worldPos);

            _cacheCheck++;
            if (_waterHeightCache.TryGetValue(hash, out height))
            {
                // got it from the cache!
                _cacheHit++;
                return true;
            }

            // compute the height
            bool success = _collProvider.SampleHeight(ref worldPos, ref height);

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
        public bool SampleDisplacementInArea(ref Vector3 worldPos, ref Vector3 displacement)
        {
            return _collProvider.SampleDisplacement(ref worldPos, ref displacement);
        }
        public bool SampleHeightInArea(ref Vector3 worldPos, ref float height)
        {
            return SampleHeight(ref worldPos, ref height);
        }
    }
}
