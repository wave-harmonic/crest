// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

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

    public struct PropertyWrapperMaterial : IPropertyWrapper
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
    public struct PropertyWrapperMPB : IPropertyWrapper
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



    public class PropertyWrapperCompute : IPropertyWrapper
    {
        private Dictionary<int, float> _floats = new Dictionary<int, float>();
        private Dictionary<int, Texture> _textures = new Dictionary<int, Texture>();
        private Dictionary<int, Vector4> _vectors = new Dictionary<int, Vector4>();
        private Dictionary<int, Vector4[]> _vectorArrays = new Dictionary<int, Vector4[]>();
        private Dictionary<int, int> _ints = new Dictionary<int, int>();

        public void SetFloat(int param, float value)
        {
            if(_floats.ContainsKey(param))
            {
                _floats[param] = value;
            }
            else
            {
                _floats.Add(param, value);
            }
        }

        public void SetInt(int param, int value)
        {
            if(_ints.ContainsKey(param))
            {
                _ints[param] = value;
            }
            else
            {
                _ints.Add(param, value);
            }
        }

        public void SetTexture(int param, Texture value)
        {
            if(_textures.ContainsKey(param))
            {
                _textures[param] = value;
            }
            else
            {
                _textures.Add(param, value);
            }
        }

        public void SetVector(int param, Vector4 value)
        {
            if(_vectors.ContainsKey(param))
            {
                _vectors[param] = value;
            }
            else
            {
                _vectors.Add(param, value);
            }
        }

        public void SetVectorArray(int param, Vector4[] value)
        {
            if(_vectorArrays.ContainsKey(param))
            {
                System.Array.Copy(value, _vectorArrays[param], value.Length);
            }
            else
            {
                Vector4[] newValue = new Vector4[value.Length];
                System.Array.Copy(value, newValue, value.Length);
                _vectorArrays.Add(param, newValue);
            }
        }

        public void InitialiseAndDispatchShader(
            CommandBuffer commandBuffer, ComputeShader computeShader,
            int computeKernel, RenderTexture renderTarget
        )
        {
            foreach(KeyValuePair<int, float> pair in _floats)
            {
                commandBuffer.SetComputeFloatParam(
                    computeShader,
                    pair.Key,
                    pair.Value
                );
            }
            foreach(KeyValuePair<int, int> pair in _ints)
            {
                commandBuffer.SetComputeIntParam(
                    computeShader,
                    pair.Key,
                    pair.Value
                );
            }
            foreach(KeyValuePair<int, Texture> pair in _textures)
            {
                commandBuffer.SetComputeTextureParam(
                    computeShader,
                    computeKernel,
                    pair.Key,
                    pair.Value
                );
            }
            foreach(KeyValuePair<int, Vector4> pair in _vectors)
            {
                commandBuffer.SetComputeVectorParam(
                    computeShader,
                    pair.Key,
                    pair.Value
                );
            }
            foreach(KeyValuePair<int, Vector4[]> pair in _vectorArrays)
            {
                commandBuffer.SetComputeVectorArrayParam(
                    computeShader,
                    pair.Key,
                    pair.Value
                );
            }

            commandBuffer.SetComputeTextureParam(
                computeShader,
                computeKernel,
                "Result",
                renderTarget
            );

            // TODO(Tom): enforce that this matches thread group size in shader
            commandBuffer.DispatchCompute(
                computeShader, computeKernel,
                OceanRenderer.Instance.LodDataResolution / 8,
                OceanRenderer.Instance.LodDataResolution / 8,
                1
            );
        }

        public void SetMatrix(int param, Matrix4x4 matrix)
        {
            // Not called anywhere for Compute Shaders anywhere,
            // so why waste the data?
            throw new System.NotImplementedException();
        }
    }
}
