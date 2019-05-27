// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Unified interface for setting properties on both materials and material property blocks
    /// </summary>
    public interface IPropertyWrapper
    {
        void SetFloat(int param, float value);
        void SetVector(int param, Vector4 value);
        void SetVectorArray(int param, Vector4[] value);
        void SetTexture(int param, Texture value);
        void SetMatrix(int param, Matrix4x4 matrix);
        void SetInt(int param, int value);
    }

    public class PropertyWrapperMaterial : IPropertyWrapper
    {
        public PropertyWrapperMaterial(Material target) { material = target; }
        public PropertyWrapperMaterial(Shader shader) { material = new Material(shader); }
        public void SetFloat(int param, float value) { material.SetFloat(param, value); }
        public void SetTexture(int param, Texture value) { material.SetTexture(param, value); }
        public void SetVector(int param, Vector4 value) { material.SetVector(param, value); }
        public void SetVectorArray(int param, Vector4[] value) { material.SetVectorArray(param, value); }
        public void SetMatrix(int param, Matrix4x4 value) { material.SetMatrix(param, value); }
        public void SetInt(int param, int value) { material.SetInt(param, value); }

        public Material material { get; private set; }
    }
    public class PropertyWrapperMPB : IPropertyWrapper
    {
        public PropertyWrapperMPB() { materialPropertyBlock = new MaterialPropertyBlock(); }
        public void SetFloat(int param, float value) { materialPropertyBlock.SetFloat(param, value); }
        public void SetTexture(int param, Texture value) { materialPropertyBlock.SetTexture(param, value); }
        public void SetVector(int param, Vector4 value) { materialPropertyBlock.SetVector(param, value); }
        public void SetVectorArray(int param, Vector4[] value) { materialPropertyBlock.SetVectorArray(param, value); }
        public void SetMatrix(int param, Matrix4x4 value) { materialPropertyBlock.SetMatrix(param, value); }
        public void SetInt(int param, int value) { materialPropertyBlock.SetInt(param, value); }

        public MaterialPropertyBlock materialPropertyBlock { get; private set; }
    }
}
