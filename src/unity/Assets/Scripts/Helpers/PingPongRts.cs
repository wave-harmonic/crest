// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class PingPongRts : MonoBehaviour
    {
        public RenderTexture _targetThisFrame;
        public RenderTexture _sourceThisFrame;

        RenderTexture _rtA, _rtB;

        // tried making it public here so i could hook it up to a RT asset which has access mode set. didnt make a difference
        public RenderTexture _lastFrameSource;

        public void InitRTs( RenderTexture rtA, RenderTexture rtB )
        {
            _rtA = rtA;
            _rtB = rtB;

            //_lastFrameSource = new RenderTexture( _rtA.descriptor );
        }

        void Update()
        {
            UpdatePingPong( out _sourceThisFrame );

            Cam.targetTexture = _targetThisFrame;
        }

        void UpdatePingPong( out RenderTexture sourceThisFrame )
        {
            // capture shape allows the render texture to stop rendering, so that the data being read is not
            // changing. this doesnt make a difference, suprisingly..

            //// only for lod 0 just a hack for experimenting, and for when the RT is taken from a texture
            //if( ShapeWaveSim._captureShape && GetComponent<WaveDataCam>()._lodIndex == 0 && _sourceThisFrame != null )
            //{
            //    // doesnt make a difference
            //    //_lastFrameSource = RenderTexture.GetTemporary( _sourceThisFrame.width, _sourceThisFrame.height, 0, _sourceThisFrame.format, RenderTextureReadWrite.Linear, 1 );

            //    // this approach copies the source out into a new RT to further remove the readpixels target from the rendering. this doesn't make
            //    // a difference - directly accessing _sourceThisFrame or _targetThisFrame has same stall.
            //    Graphics.Blit( _sourceThisFrame, _lastFrameSource );
            //}

            // switch RTs
            sourceThisFrame = _targetThisFrame;
            _targetThisFrame = _targetThisFrame == _rtA ? _rtB : _rtA;
        }

        Camera _cam; Camera Cam { get { return _cam != null ? _cam : (_cam = GetComponent<Camera>()); } }
    }
}
