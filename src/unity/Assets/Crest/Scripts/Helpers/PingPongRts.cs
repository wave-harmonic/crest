// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Flips between the two render textures each frame, and assigns one of them as the camera target texture.
    /// </summary>
    public class PingPongRts : MonoBehaviour
    {
        RenderTexture _rtSource, _rtTarget;

        public void InitRTs(RenderTexture rtA, RenderTexture rtB)
        {
            _rtSource = rtA;
            _rtTarget = rtB;
        }

        void Update()
        {
            Flip(ref _rtSource, ref _rtTarget);

            Cam.targetTexture = _rtTarget;
        }

        void Flip(ref RenderTexture rtA, ref RenderTexture rtB)
        {
            var temp = rtA;
            rtA = rtB;
            rtB = temp;
        }

        Camera _cam; Camera Cam { get { return _cam ?? (_cam = GetComponent<Camera>()); } }
    }
}
