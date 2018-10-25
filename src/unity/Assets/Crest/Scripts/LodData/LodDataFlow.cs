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

        static readonly string SHADER_KEYWORD = "_FLOW_ON";

        [SerializeField]
        protected SimSettingsFlow _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings as SimSettingsFlow; }
        public SimSettingsFlow Settings { get { return _settings as SimSettingsFlow; } }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFlow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        void OnEnable()
        {
            OceanRenderer.Instance.OceanMaterial.EnableKeyword(SHADER_KEYWORD);
        }

        void OnDisable()
        {
            OceanRenderer.Instance.OceanMaterial.DisableKeyword(SHADER_KEYWORD);
        }
    }
}
