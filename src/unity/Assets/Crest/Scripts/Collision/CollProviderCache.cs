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

        uint CalcHash(ref Vector3 in__wp)
        {
            int x = (int)(in__wp.x * _cacheBucketSizeRecip);
            int z = (int)(in__wp.z * _cacheBucketSizeRecip);
            return (uint)(x + 32768 + ((z + 32768) << 16));
        }

        public bool SampleDisplacement(ref Vector3 i_worldPos, out Vector3 o_displacement, float minSpatialLength)
        {
            return _collProvider.SampleDisplacement(ref i_worldPos, out o_displacement, minSpatialLength);
        }

        public void SampleDisplacementVel(ref Vector3 i_worldPos, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid, float minSpatialLength)
        {
            _collProvider.SampleDisplacementVel(ref i_worldPos, out o_displacement, out o_displacementValid, out o_displacementVel, out o_velValid, minSpatialLength);
        }

        public bool PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength)
        {
            return _collProvider.PrewarmForSamplingArea(areaXZ, minSpatialLength);
        }

        public bool SampleDisplacementInArea(ref Vector3 i_worldPos, out Vector3 o_displacement)
        {
            return _collProvider.SampleDisplacementInArea(ref i_worldPos, out o_displacement);
        }

        public bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal, float minSpatialLength)
        {
            return _collProvider.SampleNormal(ref in__undisplacedWorldPos, out o_normal, minSpatialLength);
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, out Vector3 undisplacedWorldPos, float minSpatialLength)
        {
            return _collProvider.ComputeUndisplacedPosition(ref i_worldPos, out undisplacedWorldPos, minSpatialLength);
        }

        public void SampleDisplacementVelInArea(ref Vector3 i_worldPos, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid)
        {
            _collProvider.SampleDisplacementVelInArea(ref i_worldPos, out o_displacement, out o_displacementValid, out o_displacementVel, out o_velValid);
        }

        /// <summary>
        /// Height is the only thing that is cached right now. We could cache disps and normals too, but the height queries are heaviest.
        /// </summary>
        public bool SampleHeight(ref Vector3 i_worldPos, out float height, float minSpatialLength)
        {
            var hash = CalcHash(ref i_worldPos);

            _cacheChecks++;
            if (_waterHeightCache.TryGetValue(hash, out height))
            {
                // got it from the cache!
                _cacheHits++;
                return true;
            }

            // compute the height
            bool success = _collProvider.SampleHeight(ref i_worldPos, out height, minSpatialLength);

            // populate cache (regardless of success for now)
            _waterHeightCache.Add(hash, height);

            return success;
        }

        public bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal)
        {
            return _collProvider.SampleNormalInArea(ref in__undisplacedWorldPos, out o_normal);
        }

        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, float minSpatialLength)
        {
            return _collProvider.CheckAvailability(ref i_worldPos, minSpatialLength);
        }

        public int CacheChecks { get { return _cacheChecksLastFrame; } }
        public int CacheHits { get { return _cacheHitsLastFrame; } }
    }
}
