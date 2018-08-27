// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class LodDataSSS : LodDataPersistent
    {
        public override SimType LodDataType { get { return SimType.SubSurfaceScattering; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Sub-Surface Scattering"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._camsSSS; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }
    }
}
