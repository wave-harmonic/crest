// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
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

        public T Current => _buffers[_currentFrameIndex];

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
    public abstract class LodDataMgr : MonoBehaviour
    {
        public abstract string SimName { get; }

        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }

        // NOTE: This MUST match the value in OceanLODData.hlsl, as it
        // determines the size of the texture arrays in the shaders.
        public const int MAX_LOD_COUNT = 15;

        protected abstract int GetParamIdSampler(bool sourceLod = false);

        protected abstract bool NeedToReadWriteTextureData { get; }

        protected BufferedData<RenderTexture> _targets;

        public RenderTexture DataTexture => _targets.Current;
        public RenderTexture GetDataTexture(int frameDelta) => _targets.Previous(frameDelta);

        public virtual int BufferCount => 1;
        public virtual void FlipBuffers() => _targets.Flip();

        public static int sp_LD_SliceIndex = Shader.PropertyToID("_LD_SliceIndex");
        protected static int sp_LODChange = Shader.PropertyToID("_LODChange");

        // shape texture resolution
        int _shapeRes = -1;

        // ocean scale last frame - used to detect scale changes
        float _oceanLocalScalePrev = -1f;

        int _scaleDifferencePow2 = 0;
        protected int ScaleDifferencePow2 { get { return _scaleDifferencePow2; } }

        protected virtual void Start()
        {
            InitData();
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
            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(TextureFormat), "The graphics device does not support the render texture format " + TextureFormat.ToString());

            Debug.Assert(OceanRenderer.Instance.CurrentLodCount <= MAX_LOD_COUNT);

            var resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);
            _targets = new BufferedData<RenderTexture>(BufferCount, () => CreateLodDataTextures(desc, SimName, NeedToReadWriteTextureData));
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

            // determine if this LOD has changed scale and by how much (in exponent of 2)
            float oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            if (_oceanLocalScalePrev == -1f) _oceanLocalScalePrev = oceanLocalScale;
            float ratio = oceanLocalScale / _oceanLocalScalePrev;
            _oceanLocalScalePrev = oceanLocalScale;
            float ratio_l2 = Mathf.Log(ratio) / Mathf.Log(2f);
            _scaleDifferencePow2 = Mathf.RoundToInt(ratio_l2);
        }

        public void BindResultData(IPropertyWrapper properties, bool blendOut = true, int framesBack = 0)
        {
            BindData(properties, _targets.Previous(framesBack), blendOut, OceanRenderer.Instance._lodTransform._renderData, framesBack);
        }

        // Avoid heap allocations instead BindData
        private Vector4[] _BindData_paramIdPosScales = new Vector4[MAX_LOD_COUNT];
        // Used in child
        protected Vector4[] _BindData_paramIdOceans = new Vector4[MAX_LOD_COUNT];
        protected virtual void BindData(IPropertyWrapper properties, Texture applyData, bool blendOut, BufferedData<LodTransform.RenderData>[] renderData, int framesBack = 0, bool sourceLod = false)
        {
            if (applyData)
            {
                properties.SetTexture(GetParamIdSampler(sourceLod), applyData);
            }

            var lt = OceanRenderer.Instance._lodTransform;
            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                // NOTE: gets zeroed by unity, see https://www.alanzucconi.com/2016/10/24/arrays-shaders-unity-5-4/
                _BindData_paramIdPosScales[lodIdx] = new Vector4(
                    renderData[lodIdx].Previous(framesBack)._posSnapped.x, renderData[lodIdx].Previous(framesBack)._posSnapped.z,
                    OceanRenderer.Instance.CalcLodScale(lodIdx), 0f);
                _BindData_paramIdOceans[lodIdx] = new Vector4(renderData[lodIdx].Previous(framesBack)._texelWidth, renderData[lodIdx].Previous(framesBack)._textureRes, 1f, 1f / renderData[lodIdx].Previous(framesBack)._textureRes);
            }

            // Duplicate the last element as the shader accesses element {slice index + 1] in a few situations. This way going
            // off the end of this parameter is the same as going off the end of the texture array with our clamped sampler.
            _BindData_paramIdPosScales[OceanRenderer.Instance.CurrentLodCount] = _BindData_paramIdPosScales[OceanRenderer.Instance.CurrentLodCount - 1];
            _BindData_paramIdOceans[OceanRenderer.Instance.CurrentLodCount] = _BindData_paramIdOceans[OceanRenderer.Instance.CurrentLodCount - 1];

            properties.SetVectorArray(LodTransform.ParamIdPosScale(sourceLod), _BindData_paramIdPosScales);
            properties.SetVectorArray(LodTransform.ParamIdOcean(sourceLod), _BindData_paramIdOceans);
        }

        public static LodDataType Create<LodDataType, LodDataSettings>(GameObject attachGO, ref LodDataSettings settings)
            where LodDataType : LodDataMgr where LodDataSettings : SimSettingsBase
        {
            var sim = attachGO.AddComponent<LodDataType>();

            if (settings == null)
            {
                settings = sim.CreateDefaultSettings() as LodDataSettings;
            }
            sim.UseSettings(settings);

            return sim;
        }

        public virtual void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = b;
            b = a;
            a = temp;
        }

        public interface IDrawFilter
        {
            float Filter(ILodDataInput data, out int isTransition);
        }

        protected void SubmitDraws(int lodIdx, CommandBuffer buf)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            lt._renderData[lodIdx].Current.Validate(0, this);

            lt.SetViewProjectionMatrices(lodIdx, buf);

            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            foreach (var draw in drawList)
            {
                draw.Draw(buf, 1f, 0);
            }
        }

        protected void SubmitDrawsFiltered(int lodIdx, CommandBuffer buf, IDrawFilter filter)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            lt._renderData[lodIdx].Current.Validate(0, this);

            lt.SetViewProjectionMatrices(lodIdx, buf);

            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            foreach (var draw in drawList)
            {
                if (!draw.Enabled)
                {
                    continue;
                }

                int isTransition;
                float weight = filter.Filter(draw, out isTransition);
                if (weight > 0f)
                {
                    draw.Draw(buf, weight, isTransition);
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
            public int GetId(bool sourceLod) { return sourceLod ? _paramId_Source : _paramId; }
        }
    }
}
