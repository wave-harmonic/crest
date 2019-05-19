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
        public PropertyWrapperMaterial(Material target) { _target = target; }
        public PropertyWrapperMaterial(Shader shader) { _target = new Material(shader); }
        public void SetFloat(int param, float value) { _target.SetFloat(param, value); }
        public void SetTexture(int param, Texture value) { _target.SetTexture(param, value); }
        public void SetVector(int param, Vector4 value) { _target.SetVector(param, value); }
        public void SetVectorArray(int param, Vector4[] value) { _target.SetVectorArray(param, value); }
        public void SetMatrix(int param, Matrix4x4 value) { _target.SetMatrix(param, value); }
        public void SetInt(int param, int value) { _target.SetInt(param, value); }

        public Material material { get { return _target; }}
        private Material _target;
    }
    public class PropertyWrapperMPB : IPropertyWrapper
    {
        public PropertyWrapperMPB(MaterialPropertyBlock target) { _target = target; }
        public void SetFloat(int param, float value) { _target.SetFloat(param, value); }
        public void SetTexture(int param, Texture value) { _target.SetTexture(param, value); }
        public void SetVector(int param, Vector4 value) { _target.SetVector(param, value); }
        public void SetVectorArray(int param, Vector4[] value) { _target.SetVectorArray(param, value); }
        public void SetMatrix(int param, Matrix4x4 value) { _target.SetMatrix(param, value); }
        public void SetInt(int param, int value) { _target.SetInt(param, value); }

        public MaterialPropertyBlock materialPropertyBlock { get { return _target; }}
        private MaterialPropertyBlock _target;
    }
}
