using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
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

            var lightGO = GameObject.Find("Directional light");
            if (lightGO)
            {
                _mainLight = lightGO.GetComponent<Light>();
            }

            _renderMaterial = new Material(Shader.Find("Ocean/ShadowUpdate"));
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

            // TODO transfer shadows up/down lod chain
            bool paramsOnly = false; // true if prev data didnt exist
            _pwMat._target = _renderMaterial;
            var rd = LodTransform._renderDataPrevFrame.Validate(-1, this);
            BindData(0, _pwMat, paramsOnly ? Texture2D.blackTexture : (_shadowData[(_rtIndex + 1) % 2] as Texture), true, ref rd);
            _pwMat._target = null;


            _bufCopyShadowMap.Blit(Texture2D.blackTexture, _shadowData[_rtIndex], _renderMaterial);

            _rtIndex = (_rtIndex + 1) % 2;
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
                _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, _bufCopyShadowMap);
                _bufCopyShadowMap = null;
            }
        }

        SimSettingsShadow Settings { get { return _settings as SimSettingsShadow; } }
    }
}
