// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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
        protected override bool NeedToReadWriteTextureData => TextureArrayHelpers.s_UseLegacyClear;
        static Texture2DArray s_nullTexture => TextureArrayHelpers.BlackTextureArray;
        protected override Texture2DArray NullTexture => s_nullTexture;
        public override int BufferCount => 2;

        internal static readonly string MATERIAL_KEYWORD_PROPERTY = "_Shadows";
        internal static readonly string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_SHADOWS_ON";
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
        static readonly int sp_ShadowMapTexture = Shader.PropertyToID("_ShadowMapTexture");

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._simSettingsShadow != null ? _ocean._simSettingsShadow : GetDefaultSettings<SettingsType>();

        public enum Error
        {
            None,
            NoLight,
            NoShadows,
            IncorrectLightType,
        }

        Error _error;

        public LodDataMgrShadow(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void Start()
        {
            base.Start();

            {
                _renderMaterial = new PropertyWrapperMaterial[OceanRenderer.Instance.CurrentLodCount];
                var shaderPath = "Hidden/Crest/Simulation/Update Shadow";

                var shader = Shader.Find(shaderPath);
                for (int i = 0; i < _renderMaterial.Length; i++)
                {
                    _renderMaterial[i] = new PropertyWrapperMaterial(shader);
                    _renderMaterial[i].SetInt(sp_LD_SliceIndex, i);
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

            {
                if (QualitySettings.shadows == ShadowQuality.Disable)
                {
                    isShadowsDisabled = true;
                }
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

            Enable();
        }

        internal override void Enable()
        {
            base.Enable();

            {
                Camera.onPreCull -= OnPreCullCamera;
                Camera.onPreCull += OnPreCullCamera;
                Camera.onPostRender -= OnPostRenderCamera;
                Camera.onPostRender += OnPostRenderCamera;
            }

            CleanUpShadowCommandBuffers();
        }

        internal override void Disable()
        {
            base.Disable();

            CleanUpShadowCommandBuffers();

            {
                Camera.onPreCull -= OnPreCullCamera;
                Camera.onPostRender -= OnPostRenderCamera;
            }
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            Disable();

            for (var index = 0; index < _renderMaterial.Length; index++)
            {
                Helpers.Destroy(_renderMaterial[index].material);
            }
        }

        protected override void InitData()
        {
            base.InitData();
            _targets.RunLambda(buffer => TextureArrayHelpers.ClearToBlack(buffer));
        }

        public override void ClearLodData()
        {
            base.ClearLodData();
            _targets.RunLambda(buffer => TextureArrayHelpers.ClearToBlack(buffer));
        }

        void OnPreCullCamera(Camera camera)
        {
#if UNITY_EDITOR
            // Do not execute when editor is not active to conserve power and prevent possible leaks.
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                BufCopyShadowMap?.Clear();
                return;
            }

            if (!OceanRenderer.IsWithinEditorUpdate)
            {
                BufCopyShadowMap?.Clear();
                return;
            }
#endif

            var ocean = OceanRenderer.Instance;

            if (ocean == null)
            {
                return;
            }

            if (!Helpers.MaskIncludesLayer(camera.cullingMask, ocean.Layer))
            {
                return;
            }

            if (camera == ocean.ViewCamera && BufCopyShadowMap != null)
            {
                // Calling this in OnPreRender was too late to be executed in the same frame.
                AddCommandBufferToPrimaryLight();

                // Disable for XR SPI otherwise input will not have correct world position.
                if (camera.stereoEnabled && XRHelpers.IsSinglePass)
                {
                    BufCopyShadowMap.DisableShaderKeyword("STEREO_INSTANCING_ON");
                }

                BuildCommandBuffer(ocean, BufCopyShadowMap);

                // Restore XR SPI as we cannot rely on remaining pipeline to do it for us.
                if (camera.stereoEnabled && XRHelpers.IsSinglePass)
                {
                    BufCopyShadowMap.EnableShaderKeyword("STEREO_INSTANCING_ON");
                }
            }
        }

        void OnPostRenderCamera(Camera camera)
        {
#if UNITY_EDITOR
            // Do not execute when editor is not active to conserve power and prevent possible leaks.
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                BufCopyShadowMap?.Clear();
                return;
            }

            if (!OceanRenderer.IsWithinEditorUpdate)
            {
                BufCopyShadowMap?.Clear();
                return;
            }
#endif

            var ocean = OceanRenderer.Instance;

            if (ocean == null)
            {
                return;
            }

            if (!Helpers.MaskIncludesLayer(camera.cullingMask, ocean.Layer))
            {
                return;
            }

            if (camera == ocean.ViewCamera)
            {
                // CBs added to a light are executed for every camera, but the LOD data is only supports a single
                // camera. Removing the CB after the camera renders restricts the CB to one camera.
                RemoveCommandBufferFromPrimaryLight();
            }
        }

        internal void AddCommandBufferToPrimaryLight()
        {
            if (_mainLight == null || BufCopyShadowMap == null) return;
            _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
            _mainLight.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
        }

        internal void RemoveCommandBufferFromPrimaryLight()
        {
            if (_mainLight == null || BufCopyShadowMap == null) return;
            _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
        }

        /// <summary>
        /// Validates the primary light.
        /// </summary>
        /// <returns>
        /// Whether the light is valid. An invalid light should be treated as a developer error and not recoverable.
        /// </returns>
        bool ValidateLight()
        {
            if (_mainLight == null)
            {
                if (!Settings._allowNullLight)
                {
                    if (_error != Error.NoLight)
                    {
                        Debug.LogWarning("Crest: Primary light must be specified on OceanRenderer script to enable shadows.", OceanRenderer.Instance);
                        _error = Error.NoLight;
                    }
                    return false;
                }

                return true;
            }

            if (_mainLight.shadows == LightShadows.None)
            {
                if (!Settings._allowNoShadows)
                {
                    if (_error != Error.NoShadows)
                    {
                        Debug.LogWarning("Crest: Shadows must be enabled on primary light to enable ocean shadowing (types Hard and Soft are equivalent for the ocean system).", _mainLight);
                        _error = Error.NoShadows;
                    }
                    return false;
                }
            }

            if (_mainLight.type != LightType.Directional)
            {
                if (_error != Error.IncorrectLightType)
                {
                    Debug.LogError("Crest: Primary light must be of type Directional.", _mainLight);
                    _error = Error.IncorrectLightType;
                }
                return false;
            }

            _error = Error.None;
            return true;
        }

        /// <summary>
        /// Stores the primary light.
        /// </summary>
        /// <returns>
        /// Whether there is a light that casts shadows.
        /// </returns>
        bool StartInitLight()
        {
            if (_mainLight == null)
            {
                _mainLight = OceanRenderer.Instance._primaryLight;

                if (_mainLight == null)
                {
                    return false;
                }
            }

            if (_mainLight.shadows == LightShadows.None)
            {
                return false;
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
                _targets.RunLambda(buffer => TextureArrayHelpers.ClearToBlack(buffer));
                CleanUpShadowCommandBuffers();
                _mainLight = null;
            }
        }

        void SetUpShadowCommandBuffers()
        {
            BufCopyShadowMap = new CommandBuffer();
            BufCopyShadowMap.name = "Crest Shadow Data";

            {
                // Call this regardless of rendering path as it has no negative consequences for forward.
                SetUpDeferredShadows();
                SetUpScreenSpaceShadows();
            }
        }

        void CleanUpShadowCommandBuffers()
        {
            if (BufCopyShadowMap != null)
            {
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
            if (_mainLight != null) _mainLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _screenSpaceShadowMapCommandBuffer);
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
            _deferredShadowMapCommandBuffer.SetGlobalTexture(sp_ShadowMapTexture, BuiltinRenderTextureType.CurrentActive);
            _mainLight.AddCommandBuffer(LightEvent.AfterShadowMap, _deferredShadowMapCommandBuffer);
        }

        void CleanUpDeferredShadows()
        {
            if (_deferredShadowMapCommandBuffer == null) return;
            if (_mainLight != null) _mainLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, _deferredShadowMapCommandBuffer);
            _deferredShadowMapCommandBuffer.Release();
            _deferredShadowMapCommandBuffer = null;
        }

        public override void UpdateLodData()
        {
            // If disabled then we hit a failure state. Try and recover in edit mode by proceeding.
            if (!enabled && Application.isPlaying)
            {
                return;
            }

            base.UpdateLodData();

            ClearBufferIfLightChanged();

            var hasShadowCastingLight = StartInitLight();
            // If in play mode, and this becomes false, then we hit a failed state and will not recover.
            enabled = ValidateLight();

            if (!s_processData || !enabled || !hasShadowCastingLight)
            {
                if (BufCopyShadowMap != null)
                {
                    // If we have a command buffer, then there is likely shadow data so we need to clear it.
                    _targets.RunLambda(buffer => TextureArrayHelpers.ClearToBlack(buffer));
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
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buffer)
        {
            // NOTE: Base call will flip buffers which is done elsewhere for this simulation.

            // Cache the camera for further down.
            var camera = ocean.ViewCamera;

#if CREST_SRP
#pragma warning disable 618
            using (new ProfilingSample(buffer, "CrestSampleShadows"))
#pragma warning restore 618
#endif
            {
                var lt = ocean._lodTransform;
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
#if UNITY_EDITOR
                    lt._renderData[lodIdx].Current.Validate(0, SimName);
#endif

                    _renderMaterial[lodIdx].SetVector(sp_CenterPos, lt._renderData[lodIdx].Current._posSnapped);
                    var scale = ocean.CalcLodScale(lodIdx);
                    _renderMaterial[lodIdx].SetVector(sp_Scale, new Vector3(scale, 1f, scale));
                    _renderMaterial[lodIdx].SetVector(sp_JitterDiameters_CurrentFrameWeights, new Vector4(Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard));
                    _renderMaterial[lodIdx].SetMatrix(sp_MainCameraProjectionMatrix, GL.GetGPUProjectionMatrix(camera.projectionMatrix, renderIntoTexture: true) * camera.worldToCameraMatrix);
                    _renderMaterial[lodIdx].SetFloat(sp_SimDeltaTime, Time.deltaTime);

                    _renderMaterial[lodIdx].SetTexture(GetParamIdSampler(true), _targets.Previous(1));

#if UNITY_EDITOR
                    // On recompiles this becomes unset even though we run over the code path to set it again...
                    _renderMaterial[lodIdx].material.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
#endif

                    LodDataMgrSeaFloorDepth.Bind(_renderMaterial[lodIdx]);

                    Helpers.Blit(buffer, _targets.Current, _renderMaterial[lodIdx].material, depthSlice: lodIdx);
                }

                // Process registered inputs.
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    buffer.SetGlobalInt(sp_LD_SliceIndex, lodIdx);
                    buffer.SetRenderTarget(_targets.Current, _targets.Current.depthBuffer, 0, CubemapFace.Unknown, lodIdx);
                    // BUG: These draw calls will "leak" and be duplicated before the above blit. They are executed at
                    // the beginning of this CB before any commands are applied.
                    SubmitDraws(lodIdx, buffer);
                }

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

        public static void BindNullToGraphicsShaders()
        {
            Shader.SetGlobalTexture(ParamIdSampler(), s_nullTexture);
        }

        public static void BindScreenSpaceNullToGraphicsShaders(Camera camera)
        {
            // Black for shadowed. White for unshadowed. Built-in RP only.
            if (camera.stereoEnabled && XRHelpers.IsSinglePass)
            {
                Shader.SetGlobalTexture(sp_CrestScreenSpaceShadowTexture, XRHelpers.WhiteTexture);
            }
            else
            {
                Shader.SetGlobalTexture(sp_CrestScreenSpaceShadowTexture, Texture2D.whiteTexture);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
