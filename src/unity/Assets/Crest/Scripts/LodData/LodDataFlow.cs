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

        protected override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_FLOW_ON"))
            {
                Debug.LogWarning("Flow is not enabled on the current ocean material and will not be visible.", this);
            }
#endif
        }
    }
}
