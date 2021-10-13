// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Unified interface for setting properties on both materials and material property blocks
    /// </summary>
    public interface IPropertyWrapper
    {
        void SetFloat(int param, float value);
        void SetFloatArray(int param, float[] value);
        void SetVector(int param, Vector4 value);
        void SetVectorArray(int param, Vector4[] value);
        void SetTexture(int param, Texture value);
        void SetMatrix(int param, Matrix4x4 matrix);
        void SetInt(int param, int value);
    }

    static class PropertyWrapperConstants
    {
        internal const string NO_SHADER_MESSAGE = "Cannot create required material because shader <i>{0}</i> could not be found or loaded."
            + " Try right clicking the Crest folder in the Project view and selecting Reimport, and checking for errors.";
    }

    [System.Serializable]
    public class PropertyWrapperMaterial : IPropertyWrapper
    {
        public PropertyWrapperMaterial(Material target) => material = target;
        public PropertyWrapperMaterial(Shader shader)
        {
            Debug.Assert(shader != null, "Crest: PropertyWrapperMaterial: Cannot create required material because shader is null");
            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
        public PropertyWrapperMaterial(string shaderPath)
        {
            Shader shader = Shader.Find(shaderPath);
            Debug.AssertFormat(shader != null, $"Crest.PropertyWrapperMaterial: {PropertyWrapperConstants.NO_SHADER_MESSAGE}", shaderPath);
            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        public void SetFloat(int param, float value) => material.SetFloat(param, value);
        public void SetFloatArray(int param, float[] value) => material.SetFloatArray(param, value);
        public void SetTexture(int param, Texture value) => material.SetTexture(param, value);
        public void SetBuffer(int param, ComputeBuffer value) => material.SetBuffer(param, value);
        public void SetVector(int param, Vector4 value) => material.SetVector(param, value);
        public void SetVectorArray(int param, Vector4[] value) => material.SetVectorArray(param, value);
        public void SetMatrix(int param, Matrix4x4 value) => material.SetMatrix(param, value);
        public void SetInt(int param, int value) => material.SetInt(param, value);

        public Material material { get; private set; }
    }

    public class PropertyWrapperMPB : IPropertyWrapper
    {
        public PropertyWrapperMPB() => materialPropertyBlock = new MaterialPropertyBlock();
        public void SetFloat(int param, float value) => materialPropertyBlock.SetFloat(param, value);
        public void SetFloatArray(int param, float[] value) => materialPropertyBlock.SetFloatArray(param, value);
        public void SetTexture(int param, Texture value) => materialPropertyBlock.SetTexture(param, value);
        public void SetVector(int param, Vector4 value) => materialPropertyBlock.SetVector(param, value);
        public void SetVectorArray(int param, Vector4[] value) => materialPropertyBlock.SetVectorArray(param, value);
        public void SetMatrix(int param, Matrix4x4 value) => materialPropertyBlock.SetMatrix(param, value);
        public void SetInt(int param, int value) => materialPropertyBlock.SetInt(param, value);

        public MaterialPropertyBlock materialPropertyBlock { get; private set; }
    }

    [System.Serializable]
    public class PropertyWrapperCompute : IPropertyWrapper
    {
        private CommandBuffer _commandBuffer = null;
        ComputeShader _computeShader = null;
        int _computeKernel = -1;

        public void Initialise(
            CommandBuffer commandBuffer,
            ComputeShader computeShader, int computeKernel
        )
        {
            _commandBuffer = commandBuffer;
            _computeShader = computeShader;
            _computeKernel = computeKernel;
        }

        public void SetFloat(int param, float value) => _commandBuffer.SetComputeFloatParam(_computeShader, param, value);
        public void SetFloatArray(int param, float[] value) => _commandBuffer.SetGlobalFloatArray(param, value);
        public void SetInt(int param, int value) => _commandBuffer.SetComputeIntParam(_computeShader, param, value);
        public void SetTexture(int param, Texture value) => _commandBuffer.SetComputeTextureParam(_computeShader, _computeKernel, param, value);
        public void SetBuffer(int param, ComputeBuffer value) => _commandBuffer.SetComputeBufferParam(_computeShader, _computeKernel, param, value);
        public void SetVector(int param, Vector4 value) => _commandBuffer.SetComputeVectorParam(_computeShader, param, value);
        public void SetVectorArray(int param, Vector4[] value) => _commandBuffer.SetComputeVectorArrayParam(_computeShader, param, value);
        public void SetMatrix(int param, Matrix4x4 value) => _commandBuffer.SetComputeMatrixParam(_computeShader, param, value);
    }

    [System.Serializable]
    public class PropertyWrapperComputeStandalone : IPropertyWrapper
    {
        ComputeShader _computeShader = null;
        int _computeKernel = -1;

        public PropertyWrapperComputeStandalone(
            ComputeShader computeShader, int computeKernel
        )
        {
            _computeShader = computeShader;
            _computeKernel = computeKernel;
        }

        public void SetFloat(int param, float value) => _computeShader.SetFloat(param, value);
        public void SetFloatArray(int param, float[] value) => _computeShader.SetFloats(param, value);
        public void SetInt(int param, int value) => _computeShader.SetInt(param, value);
        public void SetTexture(int param, Texture value) => _computeShader.SetTexture(_computeKernel, param, value);
        public void SetVector(int param, Vector4 value) => _computeShader.SetVector(param, value);
        public void SetVectorArray(int param, Vector4[] value) => _computeShader.SetVectorArray(param, value);
        public void SetMatrix(int param, Matrix4x4 value) => _computeShader.SetMatrix(param, value);
    }
}
