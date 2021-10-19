// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Circular buffer to store a multiple sets of data
    /// </summary>
    public class BufferedData<T>
    {
        public BufferedData(int bufferSize, Func<T> initFunc)
        {
            _buffers = new T[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                _buffers[i] = initFunc();
            }
        }

        public T Current { get => _buffers[_currentFrameIndex]; set => _buffers[_currentFrameIndex] = value; }

        public int Size => _buffers.Length;

        public T Previous(int framesBack)
        {
            Debug.Assert(framesBack >= 0 && framesBack < _buffers.Length);

            int index = (_currentFrameIndex - framesBack + _buffers.Length) % _buffers.Length;

            return _buffers[index];
        }

        public void Flip()
        {
            _currentFrameIndex = (_currentFrameIndex + 1) % _buffers.Length;
        }

        public void RunLambda(Action<T> lambda)
        {
            foreach (var buffer in _buffers)
            {
                lambda(buffer);
            }
        }

        T[] _buffers = null;
        int _currentFrameIndex = 0;
    }

    /// <summary>
    /// Base class for data/behaviours created on each LOD.
    /// </summary>
    public abstract class LodDataMgr
    {
        public abstract string SimName { get; }

        // This is the texture format we want to use.
        protected abstract GraphicsFormat RequestedTextureFormat { get; }

        // This is the platform compatible texture format we will use.
        public GraphicsFormat CompatibleTextureFormat { get; private set; }

        // NOTE: This MUST match the value in OceanConstants.hlsl, as it
        // determines the size of the texture arrays in the shaders.
        public const int MAX_LOD_COUNT = 15;

        // NOTE: these MUST match the values in OceanConstants.hlsl
        // 64 recommended as a good common minimum: https://www.reddit.com/r/GraphicsProgramming/comments/aeyfkh/for_compute_shaders_is_there_an_ideal_numthreads/
        public const int THREAD_GROUP_SIZE_X = 8;
        public const int THREAD_GROUP_SIZE_Y = 8;

        // NOTE: This is a temporary solution to keywords having prefixes downstream.
        internal const string MATERIAL_KEYWORD_PREFIX = "";

        protected abstract int GetParamIdSampler(bool sourceLod = false);

        protected abstract bool NeedToReadWriteTextureData { get; }

        protected BufferedData<RenderTexture> _targets;

        public RenderTexture DataTexture => _targets.Current;
        public RenderTexture GetDataTexture(int frameDelta) => _targets.Previous(frameDelta);

        public virtual int BufferCount => 1;
        public virtual void FlipBuffers() => _targets.Flip();

        protected virtual Texture2DArray NullTexture => TextureArrayHelpers.BlackTextureArray;

        public static int sp_LD_SliceIndex = Shader.PropertyToID("_LD_SliceIndex");
        protected static int sp_LODChange = Shader.PropertyToID("_LODChange");

        // shape texture resolution
        int _shapeRes = -1;

        public bool enabled { get; protected set; }

        protected OceanRenderer _ocean;

        // Implement in any sub-class which supports having an asset file for settings. This is used for polymorphic
        // operations. A sub-class will also implement an alternative for the specialised type called Settings.
        public virtual SimSettingsBase SettingsBase => null;
        SimSettingsBase _defaultSettings;

        /// <summary>
        /// Returns the default value of the settings asset for the provided type.
        /// </summary>
        protected SettingsType GetDefaultSettings<SettingsType>() where SettingsType : SimSettingsBase
        {
            if (_defaultSettings == null)
            {
                _defaultSettings = ScriptableObject.CreateInstance<SettingsType>();
                _defaultSettings.name = SimName + " Auto-generated Settings";
            }

            return (SettingsType)_defaultSettings;
        }

        public LodDataMgr(OceanRenderer ocean)
        {
            _ocean = ocean;
        }

        public virtual void Start()
        {
            InitData();
            enabled = true;
        }

        public static RenderTexture CreateLodDataTextures(RenderTextureDescriptor desc, string name, bool needToReadWriteTextureData)
        {
            RenderTexture result = new RenderTexture(desc);
            result.wrapMode = TextureWrapMode.Clamp;
            result.antiAliasing = 1;
            result.filterMode = FilterMode.Bilinear;
            result.anisoLevel = 0;
            result.useMipMap = false;
            result.name = name;
            result.dimension = TextureDimension.Tex2DArray;
            result.volumeDepth = OceanRenderer.Instance.CurrentLodCount;
            result.enableRandomWrite = needToReadWriteTextureData;
            result.Create();
            return result;
        }

        protected virtual void InitData()
        {
            // Find a compatible texture format.
            var formatUsage = NeedToReadWriteTextureData ? FormatUsage.LoadStore : FormatUsage.Sample;
            CompatibleTextureFormat = SystemInfo.GetCompatibleFormat(RequestedTextureFormat, formatUsage);
            if (CompatibleTextureFormat != RequestedTextureFormat)
            {
                Debug.Log($"Crest: Using render texture format {CompatibleTextureFormat} instead of {RequestedTextureFormat}");
            }
            Debug.Assert(CompatibleTextureFormat != GraphicsFormat.None, $"Crest: The graphics device does not support the render texture format {RequestedTextureFormat}");

            Debug.Assert(OceanRenderer.Instance.CurrentLodCount <= MAX_LOD_COUNT);

            var resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, CompatibleTextureFormat, 0);
            _targets = new BufferedData<RenderTexture>(BufferCount, () => CreateLodDataTextures(desc, SimName, NeedToReadWriteTextureData));

            // Bind globally once here on init, which will bind to all graphics shaders (not compute)
            Shader.SetGlobalTexture(GetParamIdSampler(), _targets.Current);
        }

        public virtual void UpdateLodData()
        {
            int width = OceanRenderer.Instance.LodDataResolution;
            // debug functionality to resize RT if different size was specified.
            if (_shapeRes == -1)
            {
                _shapeRes = width;
            }
            else if (width != _shapeRes)
            {
                _shapeRes = width;

                _targets.RunLambda(buffer =>
                {
                    buffer.Release();
                    buffer.width = buffer.height = _shapeRes;
                    buffer.Create();
                });
            }
        }


        public virtual void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            FlipBuffers();
        }

        public interface IDrawFilter
        {
            float Filter(ILodDataInput data, out int isTransition);
        }

        protected void SubmitDraws(int lodIdx, CommandBuffer buf)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            lt._renderData[lodIdx].Current.Validate(0, SimName);

            lt.SetViewProjectionMatrices(lodIdx, buf);

            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            foreach (var draw in drawList)
            {
                if (!draw.Value.Enabled)
                {
                    continue;
                }

                draw.Value.Draw(buf, 1f, 0, lodIdx);
            }
        }

        protected void SubmitDrawsFiltered(int lodIdx, CommandBuffer buf, IDrawFilter filter)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            lt._renderData[lodIdx].Current.Validate(0, SimName);

            lt.SetViewProjectionMatrices(lodIdx, buf);

            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            foreach (var draw in drawList)
            {
                if (!draw.Value.Enabled)
                {
                    continue;
                }

                float weight = filter.Filter(draw.Value, out var isTransition);
                if (weight > 0f)
                {
                    draw.Value.Draw(buf, weight, isTransition, lodIdx);
                }
            }
        }

        protected struct TextureArrayParamIds
        {
            private int _paramId;
            private int _paramId_Source;
            public TextureArrayParamIds(string textureArrayName)
            {
                _paramId = Shader.PropertyToID(textureArrayName);
                // Note: string concatenation does generate a small amount of
                // garbage. However, this is called on initialisation so should
                // be ok for now? Something worth considering for the future if
                // we want to go garbage-free.
                _paramId_Source = Shader.PropertyToID(textureArrayName + "_Source");
            }
            public int GetId(bool sourceLod) => sourceLod ? _paramId_Source : _paramId;
        }

        internal virtual void OnEnable()
        {
        }
        internal virtual void OnDisable()
        {
            // Unbind from all graphics shaders (not compute)
            Shader.SetGlobalTexture(GetParamIdSampler(), NullTexture);
        }
    }
}
