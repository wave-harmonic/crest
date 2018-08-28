// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD. It 
    /// </summary>
    public class LodDataDynamicWaves : LodDataPersistent
    {
        public override SimType LodDataType { get { return SimType.DynamicWaves; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/2D Wave Equation"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._camsDynWaves; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsWave>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        public bool _rotateLaplacian = true;

        CameraEvent _copySimResultEvent = 0;

        Material _copySimMaterial = null;
        CommandBuffer _copySimResultsCmdBuf;
        Camera _copySimResultCamera = null;

        bool _active = true;
        public bool Active { get { return _active; } }

        protected override void Start()
        {
            base.Start();

            _copySimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Wave Add To Disps"));
        }

        public void HookCombinePass(Camera camera, CameraEvent onEvent)
        {
            _copySimResultCamera = camera;
            _copySimResultEvent = onEvent;
        }

        protected override void LateUpdateInternal()
        {
            base.LateUpdateInternal();

            // this sim copies its results into the animated waves

            if (_copySimResultCamera == null)
            {
                // the copy results hook is not configured yet
                return;
            }

            // check if the sim should be running
            float texelWidth = LodTransform._renderData.Validate(0, this)._texelWidth;
            _active = texelWidth >= Settings._minGridSize && (texelWidth <= Settings._maxGridSize || Settings._maxGridSize == 0f);
            if (!_active && _copySimResultsCmdBuf != null)
            {
                // not running - remove command buffer to copy results in
                _copySimResultCamera.RemoveCommandBuffer(_copySimResultEvent, _copySimResultsCmdBuf);
                _copySimResultsCmdBuf = null;
            }
            else if (_active && _copySimResultsCmdBuf == null)
            {
                // running - create command buffer
                _copySimResultsCmdBuf = new CommandBuffer();
                _copySimResultsCmdBuf.name = "CopySimResults_" + SimName;
                _copySimResultCamera.AddCommandBuffer(_copySimResultEvent, _copySimResultsCmdBuf);
            }
            // only run simulation if enabled
            Cam.enabled = _active;

            _copySimMaterial.SetFloat("_HorizDisplace", Settings._horizDisplace);
            _copySimMaterial.SetFloat("_DisplaceClamp", Settings._displaceClamp);
            _copySimMaterial.SetFloat("_TexelWidth", (2f * Cam.orthographicSize) / PPRTs.Target.width);

            _copySimMaterial.mainTexture = PPRTs.Target;

            if (_copySimResultsCmdBuf != null)
            {
                _copySimResultsCmdBuf.Clear();
                _copySimResultsCmdBuf.Blit(
                    PPRTs.Target, OceanRenderer.Instance.Builder._camsAnimWaves[LodTransform.LodIndex].targetTexture, _copySimMaterial);
            }
        }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat("_Damping", Settings._damping);
            simMaterial.SetFloat("_Gravity", OceanRenderer.Instance.Gravity);

            float laplacianKernelAngle = _rotateLaplacian ? Mathf.PI * 2f * Random.value : 0f;
            simMaterial.SetVector("_LaplacianAxisX", new Vector2(Mathf.Cos(laplacianKernelAngle), Mathf.Sin(laplacianKernelAngle)));

            // assign sea floor depth - to slot 1 current frame data. minor bug here - this depth will actually be from the previous frame,
            // because the depth is scheduled to render just before the animated waves, and this sim happens before animated waves.
            OceanRenderer.Instance.Builder._lodDataAnimWaves[LodTransform.LodIndex].LDSeaDepth.BindResultData(1, simMaterial);
        }

        private void OnDisable()
        {
            if (_copySimResultsCmdBuf != null)
            {
                _copySimResultCamera.RemoveCommandBuffer(_copySimResultEvent, _copySimResultsCmdBuf);
                _copySimResultsCmdBuf = null;
            }
        }

        public static void CountWaveSims(int countFrom, out int present, out int active)
        {
            present = active = 0;
            foreach (var ldaw in OceanRenderer.Instance.Builder._lodDataAnimWaves)
            {
                if (ldaw.LDDynamicWaves == null)
                    continue;
                present++;

                if (ldaw.LodTransform.LodIndex < countFrom)
                    continue;

                if (!ldaw.LDDynamicWaves.Active)
                    continue;
                active++;
            }
        }

        SimSettingsWave Settings { get { return _settings as SimSettingsWave; } }
    }
}
