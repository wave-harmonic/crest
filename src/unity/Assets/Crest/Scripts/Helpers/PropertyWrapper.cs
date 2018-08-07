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
        void SetTexture(int param, Texture value);
    }
    public class PropertyWrapperMaterial : IPropertyWrapper
    {
        public void SetFloat(int param, float value) { _target.SetFloat(param, value); }
        public void SetTexture(int param, Texture value) { _target.SetTexture(param, value); }
        public void SetVector(int param, Vector4 value) { _target.SetVector(param, value); }
        public Material _target;
    }
    public class PropertyWrapperMPB : IPropertyWrapper
    {
        public void SetFloat(int param, float value) { _target.SetFloat(param, value); }
        public void SetTexture(int param, Texture value) { _target.SetTexture(param, value); }
        public void SetVector(int param, Vector4 value) { _target.SetVector(param, value); }
        public MaterialPropertyBlock _target;
    }
}
