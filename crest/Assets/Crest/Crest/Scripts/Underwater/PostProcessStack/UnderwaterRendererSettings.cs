

using System;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Crest
{
    [Serializable]
    [PostProcess(typeof(UnderwaterRenderer), PostProcessEvent.BeforeStack, "Crest/Underwater", false)]
    public sealed class UnderwaterRendererSettings : PostProcessEffectSettings
    {
        public Material _underwaterPostProcessMaterial;

        public BoolParameter _enable = new BoolParameter() { value = true };
        public BoolParameter _copyOceanMaterialParamsEachFrame = new BoolParameter() { value = false };

        [Range(UnderwaterPostProcessUtils.MinFilterOceanDataValue, UnderwaterPostProcessUtils.MaxFilterOceanDataValue), Tooltip(UnderwaterPostProcessUtils.tooltipFilterOceanData)]
        public IntParameter _filterOceanData = new IntParameter() { value = UnderwaterPostProcessUtils.DefaultFilterOceanDataValue };

        [Header("Debug Options")]
        public BoolParameter _viewOceanMask = new BoolParameter() { value = false };
        public BoolParameter _disableOceanMask = new BoolParameter() { value = false };
        [Range(0f, 1f), Tooltip(UnderwaterPostProcessUtils.tooltipHorizonSafetyMarginMultiplier)]
        public FloatParameter _horizonSafetyMarginMultiplier = new FloatParameter() { value = UnderwaterPostProcessUtils.DefaultHorizonSafetyMarginMultiplier };
        public BoolParameter _scaleSafetyMarginWithDynamicResolution = new BoolParameter() { value = true };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (!Application.isPlaying)
            {
                return false;
            }
            if (OceanRenderer.Instance != null)
            {
                return OceanRenderer.Instance.ViewerHeightAboveWater < 2f && _enable.value;
            }
            else
            {
                return false;
            }
        }
    }
}
