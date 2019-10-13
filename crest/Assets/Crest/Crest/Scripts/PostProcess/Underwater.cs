// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Crest
{
    /// <summary>
    /// This class holds settings for the Underwater effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(UnderwaterRenderer), PostProcessEvent.BeforeStack, "Crest/Underwater")]
    public sealed class Underwater : PostProcessEffectSettings
    {
    }

    internal sealed class UnderwaterRenderer : PostProcessEffectRenderer<Underwater>
    {
        static readonly int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static readonly int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static readonly int sp_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static readonly int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_InstanceData = Shader.PropertyToID("_InstanceData");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");

        private const string SHADER_UNDERWATER = "Crest/Underwater/Post Process New";
        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";

        Shader _shader;
        Shader _shaderMask;

        public override void Init()
        {
            base.Init();

            InitialisedCorrectly();
        }

        private bool InitialisedCorrectly()
        {
            _shader = Shader.Find(SHADER_UNDERWATER);
            //if (_underwaterPostProcessMaterial == null)
            //{
            //    Debug.LogError("UnderwaterPostProcess must have a post processing material assigned", this);
            //    return false;
            //}

            _shaderMask = Shader.Find(SHADER_OCEAN_MASK);
            //_oceanMaskMaterial = maskShader ? new Material(maskShader) : null;
            //if (_oceanMaskMaterial == null)
            //{
            //    Debug.LogError($"Could not create a material with shader {SHADER_OCEAN_MASK}", this);
            //    return false;
            //}

            if (OceanRenderer.Instance && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_UNDERWATER_ON"))
            {
                Debug.LogError("Underwater must be enabled on the ocean material for UnderwaterPostProcess to work", OceanRenderer.Instance);
                return false;
            }

            //return CheckMaterial();
            return true;
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;

            var sheet = context.propertySheets.Get(_shader);

            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
