// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// TODO: This has some lag for deferred because the chosen event happens after opaque.

namespace Crest.Examples
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    [ExecuteDuringEditMode]
    public class MaskFill : CustomMonoBehaviour
    {
        [SerializeField]
        [Tooltip("Masked meshes that need filling. Can provide non masked meshes to build a fill mask for open meshes as it uses the back faces.")]
        List<MeshFilter> _meshes = new List<MeshFilter>();

        static class ShaderIDs
        {
            public static readonly int s_FillTexture = Shader.PropertyToID("_FillTexture");
        }

        Material _material;
        RenderTexture _texture;
        RenderTargetIdentifier _target;

        void OnEnable()
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find("Hidden/Crest/Examples/Mask Fill"));
            }

            Helpers.CreateRenderTargetTextureReference(ref _texture, ref _target);
            _texture.name = "_FillTexture";
        }

        void OnDisable()
        {
            if (_texture != null)
            {
                _texture.Release();
            }
        }

        public void ExecuteFillMask(CommandBuffer buffer, Camera camera, RenderTextureDescriptor descriptor)
        {
            if (_meshes.Count == 0)
            {
                return;
            }

            if (Helpers.RenderTargetTextureNeedsUpdating(_texture, descriptor))
            {
                // RFloat is supported much better than RHalf.
                descriptor.colorFormat = RenderTextureFormat.RFloat;
                descriptor.depthBufferBits = 24;
                _texture.Release();
                _texture.descriptor = descriptor;
            }

            buffer.SetRenderTarget(_target);
            buffer.ClearRenderTarget(true, true, Color.black);
            buffer.SetGlobalTexture(ShaderIDs.s_FillTexture, _target);

            foreach (var meshFilter in _meshes)
            {
                if (meshFilter == null)
                {
                    continue;
                }

                buffer.DrawMesh(meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix, _material, 0, 0, null);
            }
        }

        public void EnableShadowPassKeyword(CommandBuffer buffer, Camera camera, RenderTextureDescriptor descriptor)
        {
            // Shader Caster pass is used for both depth and shadows, but there is no way to determine which one.
            buffer.EnableShaderKeyword("_SHADOW_PASS");
        }

        public void DisableShadowPassKeyword(CommandBuffer buffer, Camera camera, RenderTextureDescriptor descriptor)
        {
            buffer.DisableShaderKeyword("_SHADOW_PASS");
        }

        public void Clear(CommandBuffer buffer, Camera camera)
        {
            if (_texture != null && _texture.IsCreated())
            {
                buffer.SetRenderTarget(_target);
                buffer.ClearRenderTarget(true, true, Color.black);
            }

            buffer.DisableShaderKeyword("_SHADOW_PASS");
        }
    }
}

