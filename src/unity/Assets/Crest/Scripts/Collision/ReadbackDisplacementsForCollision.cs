using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering; // asyn readback experimental prior to 2018.2
using Unity.Collections;
using UnityEngine.Profiling;

namespace Crest
{
    /// <summary>
    /// Class that handles copying shape back from the GPU to use for CPU collision.
    /// </summary>
    public class ReadbackDisplacementsForCollision : MonoBehaviour
    {
        [Tooltip("Read shape textures back to the CPU for collision purposes.")]
        public bool _readbackShapeForCollision = true;

        struct CollisionRequest
        {
            public AsyncGPUReadbackRequest _request;
            public LodData.RenderData _renderData;
        }

        Queue<CollisionRequest> _requests = new Queue<CollisionRequest>();
        const int MAX_REQUESTS = 8;

        // collision data
        NativeArray<ushort> _collDataNative;
        LodData.RenderData _collRenderData;

        private void Update()
        {
            if (_readbackShapeForCollision)
            {
                UpdateShapeReadback(Cam, LDAW._renderData);
            }
        }

        /// <summary>
        /// Request current contents of cameras shape texture.
        /// </summary>
        public void UpdateShapeReadback(Camera cam, LodData.RenderData renderData)
        {
            // shape textures are read back to the CPU for collision purposes. this uses an experimental API which
            // will hopefully be settled in future unity versions.
            // queue pattern inspired by: https://github.com/keijiro/AsyncCaptureTest

            // beginning of update turns out to be a good time to sample the displacement textures. i had
            // issues doing this in post render because there is a follow up pass for the lod0 camera which
            // combines shape textures, and this pass was not included.
            EnqueueReadbackRequest(cam.targetTexture, renderData);

            // remove any failed readback requests
            for (int i = 0; i < MAX_REQUESTS && _requests.Count > 0; i++)
            {
                var request = _requests.Peek();
                if (request._request.hasError)
                {
                    _requests.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // create array to hold collision data if we don't have one already
            var num = 4 * cam.targetTexture.width * cam.targetTexture.height;
            if (!_collDataNative.IsCreated || _collDataNative.Length != num)
            {
                _collDataNative = new NativeArray<ushort>(num, Allocator.Persistent);
            }

            // process current request queue
            if (_requests.Count > 0)
            {
                var request = _requests.Peek();
                if (request._request.done)
                {
                    _requests.Dequeue();

                    // eat up any more completed requests to squeeze out latency wherever possible
                    CollisionRequest nextRequest;
                    while (_requests.Count > 0 && (nextRequest = _requests.Peek())._request.done)
                    {
                        request = nextRequest;
                        _requests.Dequeue();
                    }

                    Profiler.BeginSample("Copy out collision data");

                    var data = request._request.GetData<ushort>();
                    data.CopyTo(_collDataNative);
                    _collRenderData = request._renderData;

                    Profiler.EndSample();
                }
            }
        }

        public void EnqueueReadbackRequest(RenderTexture target, LodData.RenderData renderData)
        {
            if (_requests.Count < MAX_REQUESTS)
            {
                _requests.Enqueue(
                    new CollisionRequest
                    {
                        _request = AsyncGPUReadback.Request(target),
                        _renderData = renderData
                    }
                );
            }
        }

        public Rect CollisionDataRectXZ
        {
            get
            {
                float w = _collRenderData._texelWidth * _collRenderData._textureRes;
                return new Rect(_collRenderData._posSnapped.x - w / 2f, _collRenderData._posSnapped.z - w / 2f, w, w);
            }
        }

        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            float xOffset = worldPos.x - _collRenderData._posSnapped.x;
            float zOffset = worldPos.z - _collRenderData._posSnapped.z;
            float r = _collRenderData._texelWidth * _collRenderData._textureRes / 2f;
            if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
            {
                // outside of this collision data
                return false;
            }

            var u = 0.5f + 0.5f * xOffset / r;
            var v = 0.5f + 0.5f * zOffset / r;
            var x = Mathf.FloorToInt(u * _collRenderData._textureRes);
            var y = Mathf.FloorToInt(v * _collRenderData._textureRes);
            var idx = 4 * (y * (int)_collRenderData._textureRes + x);

            displacement.x = Mathf.HalfToFloat(_collDataNative[idx + 0]);
            displacement.y = Mathf.HalfToFloat(_collDataNative[idx + 1]);
            displacement.z = Mathf.HalfToFloat(_collDataNative[idx + 2]);

            return true;
        }

        /// <summary>
        /// Get position on ocean plane that displaces horizontally to the given position.
        /// </summary>
        public Vector3 GetPositionDisplacedToPosition(ref Vector3 displacedWorldPos)
        {
            // fixed point iteration - guess should converge to location that displaces to the target position

            var guess = displacedWorldPos;

            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            for (int i = 0; i < 4; i++)
            {
                var disp = Vector3.zero;
                SampleDisplacement(ref guess, ref disp);
                var error = guess + disp - displacedWorldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }
            guess.y = OceanRenderer.Instance.SeaLevel;
            return guess;
        }

        public float GetHeight(ref Vector3 worldPos)
        {
            var posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            var undisplacedPos = GetPositionDisplacedToPosition(ref posFlatland);

            var disp = Vector3.zero;
            SampleDisplacement(ref undisplacedPos, ref disp);

            return posFlatland.y + disp.y;
        }

        public int CollReadbackRequestsQueued { get { return _requests.Count; } }

        void OnDisable()
        {
            // free native array when component removed or destroyed
            if (_collDataNative.IsCreated)
            {
                _collDataNative.Dispose();
            }
        }

        /// <summary>
        /// Returns index of lod that completely covers the sample area, and contains wavelengths that repeat no more than twice across the smaller
        /// spatial length. If no such lod available, returns -1. This means high frequency wavelengths are filtered out, and the lod index can
        /// be used for each sample in the sample area.
        /// </summary>
        public static int SuggestCollisionLOD(Rect sampleAreaXZ)
        {
            return SuggestCollisionLOD(sampleAreaXZ, Mathf.Min(sampleAreaXZ.width, sampleAreaXZ.height));
        }
        public static int SuggestCollisionLOD(Rect sampleAreaXZ, float minSpatialLength)
        {
            var ldaws = OceanRenderer.Instance.Builder._lodDataAnimWaves;
            for (int lod = 0; lod < ldaws.Length; lod++)
            {
                // shape texture needs to completely contain sample area
                var ldaw = ldaws[lod];
                var wdcRect = ldaw.CollReadback.CollisionDataRectXZ;
                // shrink rect by 1 texel border - this is to make finite differences fit as well
                wdcRect.x += ldaw._renderData._texelWidth; wdcRect.y += ldaw._renderData._texelWidth;
                wdcRect.width -= 2f * ldaw._renderData._texelWidth; wdcRect.height -= 2f * ldaw._renderData._texelWidth;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                    continue;

                // the smallest wavelengths should repeat no more than twice across the smaller spatial length
                var minWL = ldaw.MaxWavelength() / 2f;
                if (minWL < minSpatialLength / 2f)
                    continue;

                return lod;
            }

            return -1;
        }

        LodDataAnimatedWaves _ldaw; LodDataAnimatedWaves LDAW { get { return _ldaw ?? (_ldaw = GetComponent<LodDataAnimatedWaves>()); } }
        Camera _cam; Camera Cam { get { return _cam ?? (_cam = GetComponent<Camera>()); } }
    }
}
