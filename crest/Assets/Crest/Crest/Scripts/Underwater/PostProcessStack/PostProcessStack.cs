

using System;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

using static Crest.UnderwaterPostProcessUtils;


namespace Crest
{
    public sealed class PostProcessStack : PostProcessEffectRenderer<PostProcessStackSettings>
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
            PropertyWrapperMPB underwaterPostProcessMaterialWrapper = new PropertyWrapperMPB(underWaterPostProcessEffect.properties);

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
