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

        public static int SuggestDataLOD(Rect sampleAreaXZ, float minSpatialLength)
        {
            var ldaws = OceanRenderer.Instance._lodDataAnimWaves;
            for (int lod = 0; lod < ldaws.Length; lod++)
            {
                // shape texture needs to completely contain sample area
                var ldaw = ldaws[lod].LDFlow;
                if (ldaw.DataReadback == null) return -1;
                var wdcRect = ldaw.DataReadback.DataRectXZ;
                // shrink rect by 1 texel border - this is to make finite differences fit as well
                wdcRect.x += ldaw.LodTransform._renderData._texelWidth; wdcRect.y += ldaw.LodTransform._renderData._texelWidth;
                wdcRect.width -= 2f * ldaw.LodTransform._renderData._texelWidth; wdcRect.height -= 2f * ldaw.LodTransform._renderData._texelWidth;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                    continue;

                return lod;
            }

            return -1;
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
            var idx = 2 * (y * (int)_dataReadback._dataRenderData._textureRes + x);
            flow.x = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 0]);
            flow.y = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 1]);

            return true;
        }

        ReadbackLodData _dataReadback; public ReadbackLodData DataReadback { get { return _dataReadback; } }
    }
}
