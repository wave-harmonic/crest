// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Unified interface for setting properties on both materials and material property blocks
    /// </summary>
    public interface IPropertyWrapper
    {
        void SetFloat(string name, float value);
        void SetVector(string name, Vector4 value);
        void SetTexture(string name, Texture value);
    }
    public struct PropertyWrapperMaterial : IPropertyWrapper
    {
        public PropertyWrapperMaterial(Material mat) { _mat = mat; }
        public void SetFloat(string name, float value) { _mat.SetFloat(name, value); }
        public void SetTexture(string name, Texture value) { _mat.SetTexture(name, value); }
        public void SetVector(string name, Vector4 value) { _mat.SetVector(name, value); }
        Material _mat;
    }
    public struct PropertyWrapperMPB : IPropertyWrapper
    {
        public PropertyWrapperMPB(MaterialPropertyBlock mpb) { _mpb = mpb; }
        public void SetFloat(string name, float value) { _mpb.SetFloat(name, value); }
        public void SetTexture(string name, Texture value) { _mpb.SetTexture(name, value); }
        public void SetVector(string name, Vector4 value) { _mpb.SetVector(name, value); }
        MaterialPropertyBlock _mpb;
    }
}
