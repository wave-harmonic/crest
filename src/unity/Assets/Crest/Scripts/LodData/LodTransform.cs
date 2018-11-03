using UnityEngine;

namespace Crest
{
    public class LodTransform : MonoBehaviour
    {
        protected int _transformUpdateFrame = -1;

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
            public int _frame;

            public RenderData Validate(int frameOffset, Object context)
            {
                // ignore first frame - this patches errors when using edit & continue in editor
                if (_frame > 0 && _frame != Time.frameCount + frameOffset)
                {
                    Debug.LogError(string.Format("RenderData validation failed: _frame of data ({0}) != expected ({1}). Perhaps a script execution order has not been set for a LodData script?", _frame, Time.frameCount + frameOffset), context);
                }

                return this;
            }

            public Rect RectXZ
            {
                get
                {
                    float w = _texelWidth * _textureRes;
                    return new Rect(_posSnapped.x - w / 2f, _posSnapped.z - w / 2f, w, w);
                }
            }
        }

        public RenderData _renderData = new RenderData();
        public RenderData _renderDataPrevFrame = new RenderData();

        int _lodIndex = -1;
        int _lodCount = -1;
        public void InitLODData(int lodIndex, int lodCount) { _lodIndex = lodIndex; _lodCount = lodCount; }
        public int LodIndex { get { return _lodIndex; } }
        public int LodCount { get { return _lodCount; } }

        public void LateUpdate()
        {
            if (_transformUpdateFrame == Time.frameCount)
                return;

            _transformUpdateFrame = Time.frameCount;

            _renderDataPrevFrame = _renderData;

            float camOrthSize = 2f * transform.lossyScale.x;

            // find snap period
            _renderData._textureRes = OceanRenderer.Instance.LodDataResolution;
            _renderData._texelWidth = 2f * camOrthSize / _renderData._textureRes;
            // snap so that shape texels are stationary
            _renderData._posSnapped = transform.position
                - new Vector3(Mathf.Repeat(transform.position.x, _renderData._texelWidth), 0f, Mathf.Repeat(transform.position.z, _renderData._texelWidth));

            _renderData._frame = Time.frameCount;

            // detect first update and populate the render data if so - otherwise it can give divide by 0s and other nastiness
            if (_renderDataPrevFrame._textureRes == 0f)
            {
                _renderDataPrevFrame._posSnapped = _renderData._posSnapped;
                _renderDataPrevFrame._texelWidth = _renderData._texelWidth;
                _renderDataPrevFrame._textureRes = _renderData._textureRes;
            }
        }
    }
}
