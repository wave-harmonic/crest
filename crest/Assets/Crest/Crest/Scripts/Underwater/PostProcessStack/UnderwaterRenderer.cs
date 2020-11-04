

using System;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

using static Crest.UnderwaterPostProcessUtils;


namespace Crest
{
    public class PropertyWrapperPropertySheet : IPropertyWrapper
    {
        public PropertyWrapperPropertySheet(PropertySheet propertySheet) => this.propertySheet = propertySheet;
        public void SetFloat(int param, float value) => propertySheet.properties.SetFloat(param, value);
        public void SetFloatArray(int param, float[] value) => propertySheet.properties.SetFloatArray(param, value);
        public void SetTexture(int param, Texture value) => propertySheet.properties.SetTexture(param, value);
        public void SetVector(int param, Vector4 value) => propertySheet.properties.SetVector(param, value);
        public void SetVectorArray(int param, Vector4[] value) => propertySheet.properties.SetVectorArray(param, value);
        public void SetMatrix(int param, Matrix4x4 value) => propertySheet.properties.SetMatrix(param, value);
        public void SetInt(int param, int value) => propertySheet.properties.SetInt(param, value);
        public void EnableKeyword(string keyword) => propertySheet.EnableKeyword(keyword);
        public void DisableKeyword(string keyword) => propertySheet.DisableKeyword(keyword);
        public void CopyPropertiesFromMaterial(Material material) => propertySheet.CopyPropertiesFromMaterial(material);

        public PropertySheet propertySheet { get; private set; }
    }

    public sealed class UnderwaterRenderer : PostProcessEffectRenderer<UnderwaterRendererSettings>
    {
        private Material _underwaterPostProcessMaterial;
        private Material _oceanMaskMaterial;
        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";
        UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();
        bool _firstRender = true;

        // We need to track the stereo eye index for XR SPI for usage in scripts and shaders.
        int _stereoEyeIndex = -1;

        public override void Init()
        {

        }

        public override void Release()
        {

        }

        public override void Render(PostProcessRenderContext context)
        {
            // Render is called per eye for XR SPI. We are keeping track of the eye index to determine which eye is
            // being processed for both scripts and shaders. It is possible that Unity might change this behaviour.
            if (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
            {
                _stereoEyeIndex = (_stereoEyeIndex + 1) % 2;
            }
            else
            {
                _stereoEyeIndex = 0;
            }

            PropertySheet underWaterPostProcessEffect = context.propertySheets.Get(Shader.Find("Crest/Underwater/Post Process Stack"));
            PropertyWrapperPropertySheet underwaterPostProcessMaterialWrapper = new PropertyWrapperPropertySheet(underWaterPostProcessEffect);

            if (OceanRenderer.Instance == null)
            {
                context.command.BuiltinBlit(context.source, context.destination);
            }

            if (_oceanMaskMaterial == null)
            {
                var maskShader = Shader.Find(SHADER_OCEAN_MASK);
                _oceanMaskMaterial = maskShader ? new Material(maskShader) : null;
            }

            UnderwaterPostProcessMaskRenderer perCameraData = context.camera.GetComponent<UnderwaterPostProcessMaskRenderer>();
            if (perCameraData == null)
            {
                perCameraData = context.camera.gameObject.AddComponent<UnderwaterPostProcessMaskRenderer>();
                perCameraData.Initialise(_oceanMaskMaterial, settings._disableOceanMask);
                context.command.BuiltinBlit(context.source, context.destination);
                return;
            }

            var horizonSafetyMarginMultiplier = settings._horizonSafetyMarginMultiplier.value;

            UpdatePostProcessMaterial(
                context.source,
                context.camera,
                underwaterPostProcessMaterialWrapper,
                _sphericalHarmonicsData,
                perCameraData._sampleHeightHelper,
                _firstRender || settings._copyOceanMaterialParamsEachFrame.value,
                settings._viewOceanMask.value,
                horizonSafetyMarginMultiplier,
                settings._filterOceanData.value,
                _stereoEyeIndex
            );
            _firstRender = false;

            // We are currently using CG shaders which means we cannot use the HLSL includes from the post-processing
            // stack package. This leaves some variables not set like unity_StereoEyeIndex. We could setup each eye
            // correctly so we do not need the eye index, but this might diverge from downstream too much.
            if (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
            {
                context.command.SetGlobalInt("_StereoEyeIndex", _stereoEyeIndex);
            }

            context.command.BlitFullscreenTriangle(context.source, context.destination, underWaterPostProcessEffect, 0);
        }
    }
}
