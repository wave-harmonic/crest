// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD. It
    /// </summary>
    public class LodDataMgrDynWaves : LodDataMgrPersistent
    {
        public override LodData.SimType LodDataType { get { return LodData.SimType.DynamicWaves; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/2D Wave Equation"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }

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

        bool[] _active;
        public bool SimActive(int lodIdx) { return _active[lodIdx]; }

        protected override void Start()
        {
            base.Start();

            _copySimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Wave Add To Disps"));
        }

        protected override void InitData()
        {
            base.InitData();

            _active = new bool[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _active.Length; i++) _active[i] = true;
        }

        public void HookCombinePass(Camera camera, CameraEvent onEvent)
        {
            _copySimResultCamera = camera;
            _copySimResultEvent = onEvent;
        }

        protected override bool BuildCommandBufferInternal(int lodIdx)
        {
            if (!base.BuildCommandBufferInternal(lodIdx))
                return false;

            // this sim copies its results into the animated waves

            if (_copySimResultCamera == null)
            {
                // the copy results hook is not configured yet
                return false;
            }

            // check if the sim should be running
            float texelWidth = OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this)._texelWidth;
            _active[lodIdx] = texelWidth >= Settings._minGridSize && (texelWidth <= Settings._maxGridSize || Settings._maxGridSize == 0f);
            if (!_active[lodIdx] && _copySimResultsCmdBuf != null)
            {
                // not running - remove command buffer to copy results in
                _copySimResultCamera.RemoveCommandBuffer(_copySimResultEvent, _copySimResultsCmdBuf);
                _copySimResultsCmdBuf = null;
            }
            else if (_active[lodIdx] && _copySimResultsCmdBuf == null)
            {
                // running - create command buffer
                _copySimResultsCmdBuf = new CommandBuffer();
                _copySimResultsCmdBuf.name = "CopySimResults_" + SimName;
                _copySimResultCamera.AddCommandBuffer(_copySimResultEvent, _copySimResultsCmdBuf);
            }
            // only run simulation if enabled
            if (!_active[lodIdx])
                return false;

            _copySimMaterial.SetFloat("_HorizDisplace", Settings._horizDisplace);
            _copySimMaterial.SetFloat("_DisplaceClamp", Settings._displaceClamp);
            _copySimMaterial.SetFloat("_TexelWidth", OceanRenderer.Instance._lods[lodIdx]._renderData._texelWidth);
            _copySimMaterial.mainTexture = _targets[lodIdx];

            if (_copySimResultsCmdBuf != null)
            {
                _copySimResultsCmdBuf.Clear();
                _copySimResultsCmdBuf.Blit(
                    _targets[lodIdx], OceanRenderer.Instance._lodDataAnimWaves[lodIdx].Cam.targetTexture, _copySimMaterial);
            }

            return true;
        }

        protected override void SetAdditionalSimParams(int lodIdx, Material simMaterial)
        {
            base.SetAdditionalSimParams(lodIdx, simMaterial);

            simMaterial.SetFloat("_Damping", Settings._damping);
            simMaterial.SetFloat("_Gravity", OceanRenderer.Instance.Gravity);

            float laplacianKernelAngle = _rotateLaplacian ? Mathf.PI * 2f * Random.value : 0f;
            simMaterial.SetVector("_LaplacianAxisX", new Vector2(Mathf.Cos(laplacianKernelAngle), Mathf.Sin(laplacianKernelAngle)));

            // assign sea floor depth - to slot 1 current frame data. minor bug here - this depth will actually be from the previous frame,
            // because the depth is scheduled to render just before the animated waves, and this sim happens before animated waves.
            if (OceanRenderer.Instance._createSeaFloorDepthData)
            {
                OceanRenderer.Instance._lodDataSeaDepths[lodIdx].BindResultData(1, simMaterial);
            }

            if (OceanRenderer.Instance._createFlowSim)
            {
                OceanRenderer.Instance._lodDataFlow.BindResultData(lodIdx, 1, simMaterial);
            }

        }

        private void OnDisable()
        {
            if (_copySimResultsCmdBuf != null)
            {
                _copySimResultCamera.RemoveCommandBuffer(_copySimResultEvent, _copySimResultsCmdBuf);
                _copySimResultsCmdBuf = null;
            }
        }

        public static void CountWaveSims(int countFrom, out int o_present, out int o_active)
        {
            o_present = OceanRenderer.Instance.CurrentLodCount;
            o_active = 0;
            for (int i = 0; i < o_present; i++)
            {
                if (i < countFrom) continue;
                if (!OceanRenderer.Instance._lodDataDynWaves.SimActive(i)) continue;

                o_active++;
            }
        }

        SimSettingsWave Settings { get { return _settings as SimSettingsWave; } }
    }
}
