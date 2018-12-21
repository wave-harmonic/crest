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

        uint CalcHash(ref Vector3 i_wp)
        {
            int x = (int)(i_wp.x * _cacheBucketSizeRecip);
            int z = (int)(i_wp.z * _cacheBucketSizeRecip);
            return (uint)(x + 32768 + ((z + 32768) << 16));
        }

        /// <summary>
        /// Height is the only thing that is cached right now. We could cache disps and normals too, but the height queries are heaviest.
        /// </summary>
        public bool SampleHeight(ref Vector3 i_worldPos, SamplingData i_samplingData, out float o_height)
        {
            var hash = CalcHash(ref i_worldPos);

            _cacheChecks++;
            if (_waterHeightCache.TryGetValue(hash, out o_height))
            {
                // got it from the cache!
                _cacheHits++;
                return true;
            }

            // compute the height
            bool success = _collProvider.SampleHeight(ref i_worldPos, i_samplingData, out o_height);

            // populate cache (regardless of success for now)
            _waterHeightCache.Add(hash, o_height);

            return success;
        }

        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData)
        {
            return _collProvider.CheckAvailability(ref i_worldPos, i_samplingData);
        }

        public bool GetSamplingData(ref Rect i_displacedSamplingArea, float i_minSpatialLength, SamplingData o_samplingData)
        {
            return _collProvider.GetSamplingData(ref i_displacedSamplingArea, i_minSpatialLength, o_samplingData);
        }

        public void ReturnSamplingData(SamplingData i_data)
        {
            _collProvider.ReturnSamplingData(i_data);
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 undisplacedWorldPos)
        {
            return _collProvider.ComputeUndisplacedPosition(ref i_worldPos, i_samplingData, out undisplacedWorldPos);
        }

        public void SampleDisplacementVel(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid)
        {
            _collProvider.SampleDisplacementVel(ref i_worldPos, i_samplingData, out o_displacement, out o_displacementValid, out o_displacementVel, out o_velValid);
        }

        public bool SampleNormal(ref Vector3 i_undisplacedWorldPos, SamplingData i_samplingData, out Vector3 o_normal)
        {
            return _collProvider.SampleNormal(ref i_undisplacedWorldPos, i_samplingData, out o_normal);
        }

        public bool SampleDisplacement(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement)
        {
            return _collProvider.SampleDisplacement(ref i_worldPos, i_samplingData, out o_displacement);
        }

        public int CacheChecks { get { return _cacheChecksLastFrame; } }
        public int CacheHits { get { return _cacheHitsLastFrame; } }
    }
}
