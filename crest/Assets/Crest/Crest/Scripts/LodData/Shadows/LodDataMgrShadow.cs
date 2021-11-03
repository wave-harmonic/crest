// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if ENABLE_VR && ENABLE_VR_MODULE
using UnityEngine.XR;
#endif

namespace Crest
{
    using SettingsType = SimSettingsShadow;

    /// <summary>
    /// Stores shadowing data to use during ocean shading. Shadowing is persistent and supports sampling across
    /// many frames and jittered sampling for (very) soft shadows.
    /// </summary>
    public class LodDataMgrShadow : LodDataMgr
    {
        public override string SimName => "Shadow";
        protected override GraphicsFormat RequestedTextureFormat => GraphicsFormat.R8G8_UNorm;
        protected override bool NeedToReadWriteTextureData => true;
        static Texture2DArray s_nullTexture => TextureArrayHelpers.BlackTextureArray;
        protected override Texture2DArray NullTexture => s_nullTexture;
        public override int BufferCount => 2;

        internal const string MATERIAL_KEYWORD_PROPERTY = "_Shadows";
        internal const string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_SHADOWS_ON";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING = "Shadowing is not enabled on the ocean material and will not be visible.";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING_FIX = "Tick the <i>Shadowing</i> option in the <i>Scattering<i> parameter section on the material currently assigned to the <i>OceanRenderer</i> component.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF = "The shadow feature is disabled on this component but is enabled on the ocean material.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF_FIX = "If this is not intentional, either enable the <i>Create Shadow Data</i> option on this component to turn it on, or disable the <i>Shadowing</i> feature on the ocean material to save performance.";

        public static bool s_processData = true;

        Light _mainLight;

        // SRP version needs access to this externally, hence public get
        public CommandBuffer BufCopyShadowMap { get; private set; }
        CommandBuffer _screenSpaceShadowMapCommandBuffer;
        CommandBuffer _deferredShadowMapCommandBuffer;

        PropertyWrapperMaterial[] _renderMaterial;

        readonly int sp_CenterPos = Shader.PropertyToID("_CenterPos");
        readonly int sp_Scale = Shader.PropertyToID("_Scale");
        readonly int sp_JitterDiameters_CurrentFrameWeights = Shader.PropertyToID("_JitterDiameters_CurrentFrameWeights");
        readonly int sp_MainCameraProjectionMatrix = Shader.PropertyToID("_MainCameraProjectionMatrix");
        readonly int sp_SimDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        static readonly int sp_CrestScreenSpaceShadowTexture = Shader.PropertyToID("_CrestScreenSpaceShadowTexture");

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._simSettingsShadow != null ? _ocean._simSettingsShadow : GetDefaultSettings<SettingsType>();

        public LodDataMgrShadow(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void Start()
        {
            base.Start();

            {
                _renderMaterial = new PropertyWrapperMaterial[OceanRenderer.Instance.CurrentLodCount];
                var shader = Shader.Find("Hidden/Crest/Simulation/Update Shadow");
                for (int i = 0; i < _renderMaterial.Length; i++)
                {
                    _renderMaterial[i] = new PropertyWrapperMaterial(shader);
                }
            }

#if UNITY_EDITOR
            if (OceanRenderer.Instance.OceanMaterial != null
                && OceanRenderer.Instance.OceanMaterial.HasProperty(MATERIAL_KEYWORD_PROPERTY)
                && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(MATERIAL_KEYWORD))
            {
                Debug.LogWarning("Crest: " + ERROR_MATERIAL_KEYWORD_MISSING + " " + ERROR_MATERIAL_KEYWORD_MISSING_FIX, _ocean);
            }
#endif

            // Define here so we can override check per pipeline downstream.
            var isShadowsDisabled = false;

            if (QualitySettings.shadows == ShadowQuality.Disable)
            {
                isShadowsDisabled = true;
            }

            if (isShadowsDisabled)
            {
                Debug.LogError("Crest: Shadows must be enabled in the quality settings to enable ocean shadowing.", OceanRenderer.Instance);
                return;
            }
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            CleanUpShadowCommandBuffers();
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            CleanUpShadowCommandBuffers();
        }

        protected override void InitData()
        {
            base.InitData();
            _targets.RunLambda(buffer => TextureArrayHelpers.ClearToBlack(buffer));
        }

        bool StartInitLight()
        {
            if (_mainLight == null)
            {
                _mainLight = OceanRenderer.Instance._primaryLight;

                if (_mainLight == null)
                {
                    if (!Settings._allowNullLight)
                    {
                        Debug.LogWarning("Crest: Primary light must be specified on OceanRenderer script to enable shadows.", OceanRenderer.Instance);
                    }
                    return false;
                }

                if (_mainLight.type != LightType.Directional)
                {
                    Debug.LogError("Crest: Primary light must be of type Directional.", OceanRenderer.Instance);
                    return false;
                }

                if (_mainLight.shadows == LightShadows.None)
                {
                    Debug.LogError("Crest: Shadows must be enabled on primary light to enable ocean shadowing (types Hard and Soft are equivalent for the ocean system).", OceanRenderer.Instance);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// May happen if scenes change etc
        /// </summary>
        void ClearBufferIfLightChanged()
        {
            if (_mainLight != OceanRenderer.Instance._primaryLight)
            {
                if (_mainLight)
                {
                    CleanUpShadowCommandBuffers();
                    _targets.RunLambda(buffer => TextureArrayHelpers.ClearToBlack(buffer));
                }
                _mainLight = null;
            }
        }

        void SetUpShadowCommandBuffers()
        {
            BufCopyShadowMap = new CommandBuffer();
            BufCopyShadowMap.name = "Shadow data";
            _mainLight.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);

            // Call this regardless of rendering path as it has no negative consequences for forward.
            SetUpDeferredShadows();
            SetUpScreenSpaceShadows();
        }

        void CleanUpShadowCommandBuffers()
        {
            if (BufCopyShadowMap != null)
            {
                _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
                BufCopyShadowMap.Release();
                BufCopyShadowMap = null;
            }

            CleanUpDeferredShadows();
            CleanUpScreenSpaceShadows();
        }

        void SetUpScreenSpaceShadows()
        {
            // Make the screen-space shadow texture available for the ocean shader for caustic occlusion.
            _screenSpaceShadowMapCommandBuffer = new CommandBuffer()
            {
                name = "Screen-Space Shadow Data"
            };
            _screenSpaceShadowMapCommandBuffer.SetGlobalTexture(sp_CrestScreenSpaceShadowTexture, BuiltinRenderTextureType.CurrentActive);
            _mainLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _screenSpaceShadowMapCommandBuffer);
        }

        void CleanUpScreenSpaceShadows()
        {
            if (_screenSpaceShadowMapCommandBuffer == null) return;
            _mainLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _screenSpaceShadowMapCommandBuffer);
            _screenSpaceShadowMapCommandBuffer.Release();
            _screenSpaceShadowMapCommandBuffer = null;
        }

        void SetUpDeferredShadows()
        {
            // Make the screen-space shadow texture available for the ocean shader for caustic occlusion.
            _deferredShadowMapCommandBuffer = new CommandBuffer()
            {
                name = "Deferred Shadow Data"
            };
            _deferredShadowMapCommandBuffer.SetGlobalTexture("_ShadowMapTexture", BuiltinRenderTextureType.CurrentActive);
            _mainLight.AddCommandBuffer(LightEvent.AfterShadowMap, _deferredShadowMapCommandBuffer);
        }

        void CleanUpDeferredShadows()
        {
            if (_deferredShadowMapCommandBuffer == null) return;
            _mainLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, _deferredShadowMapCommandBuffer);
            _deferredShadowMapCommandBuffer.Release();
            _deferredShadowMapCommandBuffer = null;
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            // Intentionally blank to not flip buffers.
        }

        public override void UpdateLodData()
        {
            if (!enabled)
            {
                return;
            }

            base.UpdateLodData();

            ClearBufferIfLightChanged();

            if (!StartInitLight())
            {
                enabled = false;
            }

            if (!s_processData || !enabled)
            {
                if (BufCopyShadowMap != null)
                {
                    CleanUpShadowCommandBuffers();
                }

                return;
            }

            if (BufCopyShadowMap == null)
            {
                SetUpShadowCommandBuffers();
            }

            FlipBuffers();

            BufCopyShadowMap.Clear();

            ValidateSourceData();

            // clear the shadow collection. it will be overwritten with shadow values IF the shadows render,
            // which only happens if there are (nontransparent) shadow receivers around. this is only reliable
            // in play mode, so don't do it in edit mode.
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                TextureArrayHelpers.ClearToBlack(_targets.Current);
            }

            // Cache the camera for further down.
            var camera = OceanRenderer.Instance.ViewCamera;
            if (camera == null)
            {
                // We want to return early after clear.
                return;
            }

#if CREST_SRP
#pragma warning disable 618
            using (new ProfilingSample(BufCopyShadowMap, "CrestSampleShadows"))
#pragma warning restore 618
#endif
            {
                var lt = OceanRenderer.Instance._lodTransform;
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
#if UNITY_EDITOR
                    lt._renderData[lodIdx].Current.Validate(0, SimName);
#endif

                    _renderMaterial[lodIdx].SetVector(sp_CenterPos, lt._renderData[lodIdx].Current._posSnapped);
                    var scale = OceanRenderer.Instance.CalcLodScale(lodIdx);
                    _renderMaterial[lodIdx].SetVector(sp_Scale, new Vector3(scale, 1f, scale));
                    _renderMaterial[lodIdx].SetVector(sp_JitterDiameters_CurrentFrameWeights, new Vector4(Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard));
                    _renderMaterial[lodIdx].SetMatrix(sp_MainCameraProjectionMatrix, GL.GetGPUProjectionMatrix(camera.projectionMatrix, renderIntoTexture: true) * camera.worldToCameraMatrix);
                    _renderMaterial[lodIdx].SetFloat(sp_SimDeltaTime, Time.deltaTime);

                    _renderMaterial[lodIdx].SetInt(sp_LD_SliceIndex, lodIdx);
                    _renderMaterial[lodIdx].SetTexture(GetParamIdSampler(true), _targets.Previous(1));

                    LodDataMgrSeaFloorDepth.Bind(_renderMaterial[lodIdx]);

                    BufCopyShadowMap.Blit(Texture2D.blackTexture, _targets.Current, _renderMaterial[lodIdx].material, -1, lodIdx);
                }

#if ENABLE_VR && ENABLE_VR_MODULE
                // Disable for XR SPI otherwise input will not have correct world position.
                if (XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    BufCopyShadowMap.DisableShaderKeyword("STEREO_INSTANCING_ON");
                }
#endif

                // Process registered inputs.
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    BufCopyShadowMap.SetRenderTarget(_targets.Current, _targets.Current.depthBuffer, 0, CubemapFace.Unknown, lodIdx);
                    SubmitDraws(lodIdx, BufCopyShadowMap);
                }

#if ENABLE_VR && ENABLE_VR_MODULE
                // Restore XR SPI as we cannot rely on remaining pipeline to do it for us.
                if (XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    BufCopyShadowMap.EnableShaderKeyword("STEREO_INSTANCING_ON");
                }
#endif

                // Set the target texture as to make sure we catch the 'pong' each frame
                Shader.SetGlobalTexture(GetParamIdSampler(), _targets.Current);
            }
        }

        public void ValidateSourceData()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // Don't validate when not in play mode in editor as shadows won't be updating.
                return;
            }
#endif

            foreach (var renderData in OceanRenderer.Instance._lodTransform._renderData)
            {
                renderData.Previous(1).Validate(BuildCommandBufferBase._lastUpdateFrame - OceanRenderer.FrameCount, SimName);
            }
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Shadow";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) => s_textureArrayParamIds.GetId(sourceLod);
        protected override int GetParamIdSampler(bool sourceLod = false) => ParamIdSampler(sourceLod);

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataShadow != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataShadow.GetParamIdSampler(), OceanRenderer.Instance._lodDataShadow.DataTexture);
            }
            else
            {
                properties.SetTexture(ParamIdSampler(), s_nullTexture);
            }
        }

        public static void BindNullToGraphicsShaders() => Shader.SetGlobalTexture(ParamIdSampler(), s_nullTexture);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
