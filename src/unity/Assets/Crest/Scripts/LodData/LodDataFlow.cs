// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataFlow : LodData
    {
        public override SimType LodDataType { get { return SimType.Flow; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return Cam.targetTexture; } }
        public override void UseSettings(SimSettingsBase settings) {}
        public override SimSettingsBase CreateDefaultSettings() { return null;}

        protected override void Start() {
            base.Start();

            _dataReadback = GetComponent<ReadbackLodData>();
            _dataReadback._textureFormat = ReadbackLodData.TexFormat.RGHalf;
            _dataReadback._active = true;
        }

        void OnDisable() {
            // free native array when component removed or destroyed
            if (_dataReadback._dataNative.IsCreated)
            {
                _dataReadback._dataNative.Dispose();
            }
        }

        public bool SampleFlow(ref Vector3 in__worldPos, out Vector2 flow)
        {
            float xOffset = in__worldPos.x - _dataReadback._dataRenderData._posSnapped.x;
            float zOffset = in__worldPos.z - _dataReadback._dataRenderData._posSnapped.z;
            float r = _dataReadback._dataRenderData._texelWidth * _dataReadback._dataRenderData._textureRes / 2f;
            if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
            {
                // outside of this collision data
                flow = Vector3.zero;
                return false;
            }

            var u = 0.5f + 0.5f * xOffset / r;
            var v = 0.5f + 0.5f * zOffset / r;
            var x = Mathf.FloorToInt(u * _dataReadback._dataRenderData._textureRes);
            var y = Mathf.FloorToInt(v * _dataReadback._dataRenderData._textureRes);
            var idx = 4 * (y * (int)_dataReadback._dataRenderData._textureRes + x);
            // TODO: A hack added to ensure _dataNative so we don't get array out of
            // bounds error, but this shouldn't be happening anyway.
            if(idx + 1 >= _dataReadback._dataNative.Length) {
                flow = Vector3.zero;
                return false;
            }
            flow.x = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 0]);
            flow.y = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 1]);

            return true;
        }

        ReadbackLodData _dataReadback; public ReadbackLodData DataReadback { get { return _dataReadback; } }
    }
}
