using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Stores shadowing data to use during ocean shading. Shadowing is persistent and supports sampling across
    /// many frames and jittered sampling for (very) soft shadows.
    /// </summary>
    public class LodDataShadow : LodData
    {
        public override SimType LodDataType { get { return SimType.Shadow; } }
        public override void UseSettings(SimSettingsBase settings) { _settings = settings; }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return _shadowData[_rtIndex]; } }

        int _rtIndex = 1;
        RenderTexture[] _shadowData = new RenderTexture[2];
        CommandBuffer _bufCopyShadowMap = null;
        Light _mainLight;
        Material _renderMaterial;
        Camera _cameraMain;

        [SerializeField]
        SimSettingsBase _settings;

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsShadow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void Start()
        {
            base.Start();

            int res = OceanRenderer.Instance.LodDataResolution;
            for(int i = 0; i < 2; i++)
            {
                _shadowData[i] = new RenderTexture(res, res, 0);
                _shadowData[i].name = gameObject.name + "_shadow_" + i;
                _shadowData[i].format = TextureFormat;
                _shadowData[i].useMipMap = false;
                _shadowData[i].anisoLevel = 0;
            }

            if (!StartInitLight())
            {
                enabled = false;
                return;
            }

            _renderMaterial = new Material(Shader.Find("Ocean/ShadowUpdate"));

            _cameraMain = Camera.main;
            if (_cameraMain == null)
            {
                var viewpoint = OceanRenderer.Instance._viewpoint;
                _cameraMain = viewpoint != null ? viewpoint.GetComponent<Camera>() : null;

                if(_cameraMain == null)
                {
                    Debug.LogError("Could not find main camera, disabling shadow data", this);
                    enabled = false;
                    return;
                }
            }
        }

        bool StartInitLight()
        {
            _mainLight = OceanRenderer.Instance._primaryLight;

            if (!_mainLight)
            {
                Debug.LogError("Primary light must be specified on OceanRenderer script to enable shadows.", this);
                return false;
            }

            if (_mainLight.type != LightType.Directional)
            {
                Debug.LogError("Primary light must be of type Directional.", this);
                return false;
            }

            if (_mainLight.shadows == LightShadows.None)
            {
                Debug.LogError("Shadows must be enabled on primary light to enable ocean shadowing (types Hard and Soft are equivalent for the ocean system).", this);
                return false;
            }

            return true;
        }

        private void Update()
        {
            _rtIndex = (_rtIndex + 1) % 2;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!_mainLight) return;

            if (_bufCopyShadowMap == null)
            {
                _bufCopyShadowMap = new CommandBuffer();
                _bufCopyShadowMap.name = "Shadow data " + LodTransform.LodIndex;
                _mainLight.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, _bufCopyShadowMap);
            }

            // clear the shadow collection. it will be overwritten with shadow values IF the shadows render,
            // which only happens if there are (nontransparent) shadow receivers around
            Graphics.Blit(Texture2D.blackTexture, _shadowData[_rtIndex]);

            _bufCopyShadowMap.Clear();
            _bufCopyShadowMap.SetRenderTarget(_shadowData[_rtIndex]);

            LodTransform._renderData.Validate(0, this);
            _renderMaterial.SetVector("_CenterPos", LodTransform._renderData._posSnapped);
            _renderMaterial.SetVector("_Scale", transform.lossyScale);
            _renderMaterial.SetVector("_CamPos", OceanRenderer.Instance._viewpoint.position);
            _renderMaterial.SetVector("_CamForward", OceanRenderer.Instance._viewpoint.forward);
            _renderMaterial.SetFloat("_JitterDiameter", Settings._jitterDiameter);
            _renderMaterial.SetFloat("_CurrentFrameWeight", Settings._currentFrameWeight);
            _renderMaterial.SetMatrix("_MainCameraProjectionMatrix", _cameraMain.projectionMatrix * _cameraMain.worldToCameraMatrix);
            _renderMaterial.SetFloat("_SimDeltaTime", Time.deltaTime);

            // compute which lod data we are sampling previous frame shadows from. if a scale change has happened this can be any lod up or down the chain.
            int srcDataIdx = LodTransform.LodIndex + _scaleDifferencePow2;
            srcDataIdx = Mathf.Clamp(srcDataIdx, 0, LodTransform.LodCount - 1);
            var lds = OceanRenderer.Instance._lodDataAnimWaves;
            // bind data to slot 0 - previous frame data
            lds[srcDataIdx].LDShadow.BindSourceData(0, _renderMaterial, false);
            _bufCopyShadowMap.Blit(Texture2D.blackTexture, _shadowData[_rtIndex], _renderMaterial);
        }

        public void BindSourceData(int slot, Material simMaterial, bool paramsOnly)
        {
            _pwMat._target = _renderMaterial;
            var rd = LodTransform._renderDataPrevFrame.Validate(-1, this);
            BindData(0, _pwMat, paramsOnly ? Texture2D.blackTexture : (_shadowData[(_rtIndex + 1) % 2] as Texture), true, ref rd);
            _pwMat._target = null;
        }

        void OnEnable()
        {
            RemoveCommandBuffers();
        }

        void OnDisable()
        {
            RemoveCommandBuffers();
        }

        void RemoveCommandBuffers()
        {
            if (_bufCopyShadowMap != null)
            {
                if (_mainLight)
                {
                    _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, _bufCopyShadowMap);
                }
                _bufCopyShadowMap = null;
            }
        }

        SimSettingsShadow Settings { get { return _settings as SimSettingsShadow; } }
    }
}
