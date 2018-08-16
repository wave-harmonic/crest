// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD.
    /// </summary>
    public class SimWave : SimBase
    {
        public override string SimName { get { return "Wave"; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/2D Wave Equation"; } }
        protected override string ShaderRenderResultsIntoDispTexture { get { return "Ocean/Shape/Sim/Wave Add To Disps"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        // simulate before foam, because foam sim will generate from the waves (if there are matching resolutions)
        public override int Depth { get { return SimFoam.SIM_RENDER_DEPTH - 1; } }
        public override SimSettingsBase CreateDefaultSettings()
        {
            return ScriptableObject.CreateInstance<SimSettingsWave>();
        }

        public bool _rotateLaplacian = true;

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat("_Damping", Settings._damping);
            simMaterial.SetFloat("_Gravity", OceanRenderer.Instance.Gravity);

            float laplacianKernelAngle = _rotateLaplacian ? Mathf.PI * 2f * Random.value : 0f;
            simMaterial.SetVector("_LaplacianAxisX", new Vector2(Mathf.Cos(laplacianKernelAngle), Mathf.Sin(laplacianKernelAngle)));
        }

        protected override void SetAdditionalCopySimParams(Material copySimMaterial)
        {
            base.SetAdditionalCopySimParams(copySimMaterial);

            copySimMaterial.SetFloat("_HorizDisplace", Settings._horizDisplace);
            copySimMaterial.SetFloat("_DisplaceClamp", Settings._displaceClamp);
            copySimMaterial.SetFloat("_TexelWidth", (2f * Cam.orthographicSize) / PPRTs.Target.width);
        }

        SimSettingsWave Settings { get { return _settings as SimSettingsWave; } }
    }
}
