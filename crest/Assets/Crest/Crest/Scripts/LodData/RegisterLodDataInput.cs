// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public interface ILodDataInput
    {
        void Draw(CommandBuffer buf, int lodIdx, float weight);
        float Wavelength { get; }
        bool Enabled { get; }
    }

    /// <summary>
    /// Base class for scripts that register input to the various LOD data types.
    /// </summary>
    public abstract class RegisterLodDataInputBase : MonoBehaviour, ILodDataInput
    {
        public abstract float Wavelength { get; }

        public bool Enabled => true;

        public static int sp_Weight = Shader.PropertyToID("_Weight");

        Renderer _renderer;
        Material[] _materials = new Material[2];

        protected virtual void Start()
        {
            _renderer = GetComponent<Renderer>();

            if(_renderer)
            {
                _materials[0] = new Material(_renderer.sharedMaterial);
                _materials[1] = new Material(_renderer.sharedMaterial);
            }
        }

        public void Draw(CommandBuffer buf, int lodIdx, float weight)
        {
            if (_renderer && weight > 0f)
            {
                _materials[lodIdx % 2].SetFloat(sp_Weight, weight);
                
                buf.DrawRenderer(_renderer, _materials[lodIdx % 2]);
            }
        }
    }

    /// <summary>
    /// Registers input to a particular LOD data.
    /// </summary>
    public abstract class RegisterLodDataInput<LodDataType> : RegisterLodDataInputBase
        where LodDataType : LodDataMgr
    {
        [SerializeField] bool _disableRenderer = true;

        protected virtual void OnEnable()
        {
            var rend = GetComponent<Renderer>();
            var ocean = OceanRenderer.Instance;
            if (rend && ocean)
            {
                if (_disableRenderer)
                {
                    rend.enabled = false;
                }

                var ld = ocean.GetComponent<LodDataType>();
                if (ld)
                {
                    ld.AddDraw(this);
                }
            }
        }

        protected virtual void OnDisable()
        {
            var rend = GetComponent<Renderer>();
            var ocean = OceanRenderer.Instance;
            if (rend && ocean)
            {
                var ld = ocean.GetComponent<LodDataType>();
                if (ld)
                {
                    ld.RemoveDraw(this);
                }
            }
        }
    }
}
