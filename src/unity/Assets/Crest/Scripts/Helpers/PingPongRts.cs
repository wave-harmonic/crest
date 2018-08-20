// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Flips between the two render textures each frame, and assigns one of them as the camera target texture.
    /// </summary>
    public class PingPongRts : MonoBehaviour
    {
        /// <summary>
        /// Track frame when updated to make sure we are always using latest and greatest state.
        /// </summary>
        int _frame = -1;

        RenderTexture _rtSource, _rtTarget;

        public void InitRTs(RenderTexture rtA, RenderTexture rtB)
        {
            _rtSource = rtA;
            _rtTarget = rtB;
        }

        void Update()
        {
            if (_frame != Time.frameCount)
            {
                Flip(ref _rtSource, ref _rtTarget);

                Cam.targetTexture = _rtTarget;

                _frame = Time.frameCount;
            }
        }

        void Flip(ref RenderTexture rtA, ref RenderTexture rtB)
        {
            var temp = rtA;
            rtA = rtB;
            rtB = temp;
        }

        public RenderTexture Source { get { if (_frame != Time.frameCount) Update(); return _rtSource; } }
        public RenderTexture Target { get { if (_frame != Time.frameCount) Update(); return _rtTarget; } }

        Camera _cam; Camera Cam { get { return _cam ?? (_cam = GetComponent<Camera>()); } }
    }
}
