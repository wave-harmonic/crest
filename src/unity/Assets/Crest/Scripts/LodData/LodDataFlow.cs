// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

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

        static readonly string SHADER_KEYWORD = "_FLOW_ON";

        protected override void Start() {
            base.Start();

            _dataReadback = GetComponent<ReadbackLodData>();
            _dataReadback.SetTextureFormat(TextureFormat);
            _dataReadback._active = true;
        }

        void OnEnable()
        {
            OceanRenderer.Instance.OceanMaterial.EnableKeyword(SHADER_KEYWORD);
        }

        void OnDisable()
        {
            OceanRenderer.Instance.OceanMaterial.DisableKeyword(SHADER_KEYWORD);

            // free native array when component removed or destroyed
            if (_dataReadback._result._data.IsCreated)
            {
                _dataReadback._result._data.Dispose();
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
            return _dataReadback._result.SampleRG16(ref in__worldPos, out flow);
        }

        ReadbackLodData _dataReadback; public ReadbackLodData DataReadback { get { return _dataReadback; } }
    }
}
