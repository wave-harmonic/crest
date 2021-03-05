// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Crest
{
    using SettingsType = SimSettingsShadow;

    /// <summary>
    /// Stores shadowing data to use during ocean shading. Shadowing is persistent and supports sampling across
    /// many frames and jittered sampling for (very) soft shadows.
    /// </summary>
    public class LodDataMgrShadow : LodDataMgr
    {
        public override string SimName { get { return "Shadow"; } }
        protected override GraphicsFormat RequestedTextureFormat => GraphicsFormat.R8G8_UNorm;
        protected override bool NeedToReadWriteTextureData { get { return true; } }

        internal const string MATERIAL_KEYWORD_PROPERTY = "_Shadows";
        internal const string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_SHADOWS_ON";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING = "Shadowing must be enabled on the ocean material. Tick the <i>Shadowing</i> option in the <i>Scattering</i> parameter section on the material currently assigned to the OceanRenderer component.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF = "The shadow feature is disabled on this component but is enabled on the ocean material. If this is not intentional, either enable the <i>Create Shadow Data</i> option on this component to turn it on, or disable the Shadowing feature on the ocean material to save performance.";

        public static bool s_processData = true;

        Light _mainLight;

        // SRP version needs access to this externally, hence public get
        public CommandBuffer BufCopyShadowMap { get; private set; }

        RenderTexture _sources;
        PropertyWrapperCompute _renderProperties;
        ComputeShader _updateShadowShader;
        private int krnl_UpdateShadow;
        public const string UpdateShadow = "UpdateShadow";

        readonly int sp_CenterPos = Shader.PropertyToID("_CenterPos");
        readonly int sp_Scale = Shader.PropertyToID("_Scale");
        readonly int sp_CamPos = Shader.PropertyToID("_CamPos");
        readonly int sp_CamForward = Shader.PropertyToID("_CamForward");
        readonly int sp_JitterDiameters_CurrentFrameWeights = Shader.PropertyToID("_JitterDiameters_CurrentFrameWeights");
        readonly int sp_MainCameraProjectionMatrix = Shader.PropertyToID("_MainCameraProjectionMatrix");
        readonly int sp_SimDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        readonly int sp_LD_SliceIndex_Source = Shader.PropertyToID("_LD_SliceIndex_Source");
        readonly int sp_LD_TexArray_Target = Shader.PropertyToID("_LD_TexArray_Target");
        readonly int sp_cascadeDataSrc = Shader.PropertyToID("_CascadeDataSrc");

        SettingsType _defaultSettings;
        public SettingsType Settings
        {
            get
            {
                if (_ocean._simSettingsShadow != null) return _ocean._simSettingsShadow;

                if (_defaultSettings == null)
                {
                    _defaultSettings = ScriptableObject.CreateInstance<SettingsType>();
                    _defaultSettings.name = SimName + " Auto-generated Settings";
                }
                return _defaultSettings;
            }
        }

        public LodDataMgrShadow(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void Start()
        {
            base.Start();

            _renderProperties = new PropertyWrapperCompute();
            _updateShadowShader = ComputeShaderHelpers.LoadShader(UpdateShadow);
            if (_updateShadowShader == null)
            {
                enabled = false;
                return;
            }

            try
            {
                krnl_UpdateShadow = _updateShadowShader.FindKernel(UpdateShadow);
            }
            catch (Exception)
            {
                Debug.LogError("Could not load shadow update kernel. Disabling shadows.", _ocean);
                enabled = false;
                return;
            }

#if UNITY_EDITOR
            if (OceanRenderer.Instance.OceanMaterial != null
                && OceanRenderer.Instance.OceanMaterial.HasProperty(MATERIAL_KEYWORD_PROPERTY)
                && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(MATERIAL_KEYWORD))
            {
                Debug.LogWarning(ERROR_MATERIAL_KEYWORD_MISSING, _ocean);
            }
#endif
        }

        protected override void InitData()
        {
            base.InitData();

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, CompatibleTextureFormat, 0);
            _sources = CreateLodDataTextures(desc, SimName + "_1", NeedToReadWriteTextureData);

            TextureArrayHelpers.ClearToBlack(_sources);
            TextureArrayHelpers.ClearToBlack(_targets);
        }

        bool StartInitLight()
        {
            _mainLight = OceanRenderer.Instance._primaryLight;

            if (_mainLight.type != LightType.Directional)
            {
                Debug.LogError("Primary light must be of type Directional.", OceanRenderer.Instance);
                return false;
            }

            if (_mainLight.shadows == LightShadows.None)
            {
                Debug.LogError("Shadows must be enabled on primary light to enable ocean shadowing (types Hard and Soft are equivalent for the ocean system).", OceanRenderer.Instance);
                return false;
            }

            return true;
        }

        public override void UpdateLodData()
        {
            if (!enabled)
            {
                return;
            }

            base.UpdateLodData();

            if (_mainLight != OceanRenderer.Instance._primaryLight)
            {
                if (_mainLight)
                {
                    _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
                    BufCopyShadowMap = null;
                    TextureArrayHelpers.ClearToBlack(_sources);
                    TextureArrayHelpers.ClearToBlack(_targets);
                }
                _mainLight = null;
            }

            if (!OceanRenderer.Instance._primaryLight)
            {
                if (!Settings._allowNullLight)
                {
                    Debug.LogWarning("Primary light must be specified on OceanRenderer script to enable shadows.", OceanRenderer.Instance);
                }
                return;
            }

            if (!_mainLight)
            {
                if (!StartInitLight())
                {
                    enabled = false;
                    return;
                }
            }

            if (BufCopyShadowMap == null && s_processData)
            {
                BufCopyShadowMap = new CommandBuffer();
                BufCopyShadowMap.name = "Shadow data";
                _mainLight.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
            }
            else if (!s_processData && BufCopyShadowMap != null)
            {
                _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
                BufCopyShadowMap = null;
            }

            if (!s_processData)
            {
                return;
            }

            Swap(ref _sources, ref _targets);

            BufCopyShadowMap.Clear();

            ValidateSourceData();

            // clear the shadow collection. it will be overwritten with shadow values IF the shadows render,
            // which only happens if there are (nontransparent) shadow receivers around. this is only reliable
            // in play mode, so don't do it in edit mode.
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                TextureArrayHelpers.ClearToBlack(_targets);
            }

            // Cache the camera for further down.
            var camera = OceanRenderer.Instance.ViewCamera;
            if (camera == null)
            {
                // We want to return early after clear.
                return;
            }

            {
                // Run shadow update

                // It feels like quite a lot could be optimized out of the below. I think the same params are written repeatedly, and probably
                // a bunch of them are already available in existing ocean globals.

                _renderProperties.Initialise(BufCopyShadowMap, _updateShadowShader, krnl_UpdateShadow);

                _renderProperties.SetVector(sp_CamPos, camera.transform.position);
                _renderProperties.SetVector(sp_CamForward, camera.transform.forward);

                _renderProperties.SetVector(sp_JitterDiameters_CurrentFrameWeights, new Vector4(Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard));
                _renderProperties.SetMatrix(sp_MainCameraProjectionMatrix, camera.projectionMatrix * camera.worldToCameraMatrix);
                _renderProperties.SetFloat(sp_SimDeltaTime, OceanRenderer.Instance.DeltaTimeDynamics);

                _renderProperties.SetTexture(GetParamIdSampler(true), (Texture)_sources);

                _renderProperties.SetTexture(sp_LD_TexArray_Target, _targets);

                _renderProperties.SetBuffer(sp_cascadeDataSrc, OceanRenderer.Instance._bufCascadeDataSrc);

                var lt = OceanRenderer.Instance._lodTransform;
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
#if UNITY_EDITOR
                    lt._renderData[lodIdx].Validate(0, SimName);
#endif

                    _renderProperties.SetVector(sp_CenterPos, lt._renderData[lodIdx]._posSnapped);
                    var scale = OceanRenderer.Instance.CalcLodScale(lodIdx);
                    _renderProperties.SetVector(sp_Scale, new Vector3(scale, 1f, scale));

                    // compute which lod data we are sampling previous frame shadows from. if a scale change has happened this can be any lod up or down the chain.
                    var srcDataIdx = lodIdx + ScaleDifferencePow2;
                    srcDataIdx = Mathf.Clamp(srcDataIdx, 0, lt.LodCount - 1);
                    _renderProperties.SetInt(sp_LD_SliceIndex, lodIdx);
                    _renderProperties.SetInt(sp_LD_SliceIndex_Source, srcDataIdx);

                    BufCopyShadowMap.DispatchCompute(_updateShadowShader, krnl_UpdateShadow,
                        OceanRenderer.Instance.LodDataResolution / THREAD_GROUP_SIZE_X,
                        OceanRenderer.Instance.LodDataResolution / THREAD_GROUP_SIZE_Y,
                        1);
                }

                // Disable single pass double-wide stereo rendering for these commands since we are rendering to
                // rendering texture. Otherwise, it will render double. Single pass instanced is broken here, but that
                // appears to be a Unity bug only for the legacy VR system.
                if (camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
                {
                    BufCopyShadowMap.SetSinglePassStereo(SinglePassStereoMode.None);
                    BufCopyShadowMap.DisableShaderKeyword("UNITY_SINGLE_PASS_STEREO");
                }

                // Process registered inputs.
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    BufCopyShadowMap.SetRenderTarget(_targets, _targets.depthBuffer, 0, CubemapFace.Unknown, lodIdx);
                    SubmitDraws(lodIdx, BufCopyShadowMap);
                }

                // Restore single pass double-wide as we cannot rely on remaining pipeline to do it for us.
                if (camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
                {
                    BufCopyShadowMap.SetSinglePassStereo(SinglePassStereoMode.SideBySide);
                    BufCopyShadowMap.EnableShaderKeyword("UNITY_SINGLE_PASS_STEREO");
                }
            }

            // Set the target texture as to make sure we catch the 'pong' each frame
            Shader.SetGlobalTexture(GetParamIdSampler(), _targets);
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

            foreach (var renderData in OceanRenderer.Instance._lodTransform._renderDataSource)
            {
                renderData.Validate(BuildCommandBufferBase._lastUpdateFrame - OceanRenderer.FrameCount, SimName);
            }
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            RemoveCommandBuffers();
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            RemoveCommandBuffers();
        }

        void RemoveCommandBuffers()
        {
            if (BufCopyShadowMap != null)
            {
                if (_mainLight)
                {
                    _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
                }
                BufCopyShadowMap = null;
            }
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Shadow";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) { return s_textureArrayParamIds.GetId(sourceLod); }
        protected override int GetParamIdSampler(bool sourceLod = false)
        {
            return ParamIdSampler(sourceLod);
        }

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataShadow != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataShadow.GetParamIdSampler(), OceanRenderer.Instance._lodDataShadow.DataTexture);
            }
            else
            {
                properties.SetTexture(ParamIdSampler(), TextureArrayHelpers.BlackTextureArray);
            }
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
