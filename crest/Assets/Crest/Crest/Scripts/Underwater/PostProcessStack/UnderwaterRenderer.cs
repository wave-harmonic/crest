

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

        public PropertySheet propertySheet { get; private set; }
    }

    public sealed class UnderwaterRenderer : PostProcessEffectRenderer<UnderwaterRendererSettings>
    {
        private Material _underwaterPostProcessMaterial;
        private Material _oceanMaskMaterial;
        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";
        UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();
        bool _firstRender = true;

        public override void Init()
        {

        }

        public override void Release()
        {

        }

        public override void Render(PostProcessRenderContext context)
        {
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
                settings._filterOceanData.value
            );
            _firstRender = false;


            context.command.BlitFullscreenTriangle(context.source, context.destination, underWaterPostProcessEffect, 0);
        }
    }
}
