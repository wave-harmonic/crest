// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataFlow : LodDataPersistent
    {
        public override SimType LodDataType { get { return SimType.Flow; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Flow"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance._camsFlow; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFlow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void Start() {
            base.Start();

            _dataReadback = GetComponent<ReadbackLodData>();
            _dataReadback._textureFormat = ReadbackLodData.TexFormat.RGHalf;
        }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);
            // assign animated waves - to slot 1 current frame data
            OceanRenderer.Instance._lodDataAnimWaves[LodTransform.LodIndex].BindResultData(1, simMaterial);
            // assign sea floor depth - to slot 1 current frame data
            OceanRenderer.Instance._lodDataAnimWaves[LodTransform.LodIndex].LDSeaDepth.BindResultData(1, simMaterial);
        }

        SimSettingsFlow Settings { get { return _settings as SimSettingsFlow; } }



        public bool SampleFlow(Vector3 worldPos, out Vector2 flow)
        {
            float xOffset = worldPos.x - _dataReadback._dataRenderData._posSnapped.x;
            float zOffset = worldPos.z - _dataReadback._dataRenderData._posSnapped.z;
            float r = _dataReadback._dataRenderData._texelWidth * _dataReadback._dataRenderData._textureRes / 2f;
            if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
            {
                // outside of this collision data
                flow = Vector2.zero;
                return false;
            }

            var u = 0.5f + 0.5f * xOffset / r;
            var v = 0.5f + 0.5f * zOffset / r;
            var x = Mathf.FloorToInt(u * _dataReadback._dataRenderData._textureRes);
            var y = Mathf.FloorToInt(v * _dataReadback._dataRenderData._textureRes);
            var idx = 4 * (y * (int)_dataReadback._dataRenderData._textureRes + x);

            flow.x = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 0]);
            flow.y = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 1]);

            return true;
        }

        ReadbackLodData _dataReadback; public ReadbackLodData DataReadback { get { return _dataReadback; } }
    }
}
