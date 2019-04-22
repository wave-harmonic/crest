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

            commandBuffer.DispatchCompute(
                computeShader, computeKernel,
                OceanRenderer.Instance.LodDataResolution,
                OceanRenderer.Instance.LodDataResolution,
                1
            );
        }
    }
}
