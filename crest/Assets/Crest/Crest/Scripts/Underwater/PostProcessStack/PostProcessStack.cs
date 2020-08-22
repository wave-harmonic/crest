

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

        public override void Render(PostProcessRenderContext context)
        {


            // Stop the material from being saved on-edits at runtime
            _underwaterPostProcessMaterial = new Material(settings._underwaterPostProcessMaterial);
            _underwaterPostProcessMaterialWrapper = new PropertyWrapperMaterial(_underwaterPostProcessMaterial);
        }
    }
}
