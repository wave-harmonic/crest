// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class PingPongRts : MonoBehaviour
    {
        public RenderTexture _targetThisFrame;
        public RenderTexture _sourceThisFrame;

        RenderTexture _rtA, _rtB;

        public void InitRTs( RenderTexture rtA, RenderTexture rtB )
        {
            _rtA = rtA;
            _rtB = rtB;
        }

        void Update()
        {
            UpdatePingPong( out _sourceThisFrame );

            Cam.targetTexture = _targetThisFrame;
        }

        void UpdatePingPong( out RenderTexture sourceThisFrame )
        {
            // switch RTs
            sourceThisFrame = _targetThisFrame;
            _targetThisFrame = _targetThisFrame == _rtA ? _rtB : _rtA;
        }

        Camera _cam; Camera Cam { get { return _cam != null ? _cam : (_cam = GetComponent<Camera>()); } }
    }
}
