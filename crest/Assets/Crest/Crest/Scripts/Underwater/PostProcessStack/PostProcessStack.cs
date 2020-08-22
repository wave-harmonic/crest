

using System;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;


namespace Crest
{
    public sealed class PostProcessStack : PostProcessEffectRenderer<PostProcessStackSettings>
    {
        private Material _underwaterPostProcessMaterial;
        private PropertyWrapperMaterial _underwaterPostProcessMaterialWrapper;

        public override void Init()
        {

        }

        public override void Release()
        {

        }

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet underWaterPostProcessEffect = context.propertySheets.Get(Shader.Find("Crest/Underwater/Post Process"));
            PropertyWrapperMPB underwaterPostProcessMaterialWrapper = new PropertyWrapperMPB(underWaterPostProcessEffect.properties);

            context.command.BlitFullscreenTriangle(context.source, context.destination, underWaterPostProcessEffect, 0);
        }
    }
}
