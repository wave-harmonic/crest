// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Base class for data/behaviours created on each LOD.
    /// </summary>
    public abstract class LodDataMgr : MonoBehaviour
    {
        public abstract string SimName { get; }

        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }

        // NOTE: This MUST match the value in OceanLODDataShared.hlsl, as it
        // determines the size of the texture arrays in the shaders.
        public const int MAX_LOD_COUNT = 16;

        protected abstract int GetParamIdSampler(bool prevFrame = false);

        protected abstract bool NeedToReadWriteTextureData { get; }

        protected RenderTexture _targets;

        public RenderTexture DataTexture { get { return _targets; } }

        // shape texture resolution
        int _shapeRes = -1;

        // ocean scale last frame - used to detect scale changes
        float _oceanLocalScalePrev = -1f;

        int _scaleDifferencePow2 = 0;
        protected int ScaleDifferencePow2 { get { return _scaleDifferencePow2; } }

        protected List<RegisterLodDataInputBase> _drawList = new List<RegisterLodDataInputBase>();

        protected virtual void Start()
        {
            InitData();
        }

        protected virtual void InitData()
        {
            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(TextureFormat), "The graphics device does not support the render texture format " + TextureFormat.ToString());

            Debug.Assert(OceanRenderer.Instance.CurrentLodCount <= MAX_LOD_COUNT);

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);

            _targets = new RenderTexture(desc);
            _targets.wrapMode = TextureWrapMode.Clamp;
            _targets.antiAliasing = 1;
            _targets.filterMode = FilterMode.Bilinear;
            _targets.anisoLevel = 0;
            _targets.useMipMap = false;
            _targets.name = SimName;
            _targets.dimension = TextureDimension.Tex2DArray;
            _targets.volumeDepth = OceanRenderer.Instance.CurrentLodCount;
            _targets.enableRandomWrite = NeedToReadWriteTextureData;
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
                _targets.Release();
                _targets.width = _targets.height = _shapeRes;
                _targets.Create();
            }

            // determine if this LOD has changed scale and by how much (in exponent of 2)
            float oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            if (_oceanLocalScalePrev == -1f) _oceanLocalScalePrev = oceanLocalScale;
            float ratio = oceanLocalScale / _oceanLocalScalePrev;
            _oceanLocalScalePrev = oceanLocalScale;
            float ratio_l2 = Mathf.Log(ratio) / Mathf.Log(2f);
            _scaleDifferencePow2 = Mathf.RoundToInt(ratio_l2);
        }

        public void BindResultData(IPropertyWrapper properties, bool prevFrame = false)
        {
            // TODO(MRT): Use prev frame renderdata sometimes? Check
            BindData(properties, _targets, true, ref LodTransform._staticRenderData, prevFrame);
        }

        public void BindResultData(IPropertyWrapper properties, bool blendOut, bool prevFrame = false)
        {
            BindData(properties, _targets, blendOut, ref LodTransform._staticRenderData, prevFrame);
        }


        // TODO(MRT): This is a temporary hack to avoid a lot of array allocations which are then GCed.
        // this will need to be fixed by changing the BindData API to something more appropriate, and making
        // the LodTransform class SOA
        Vector4[] _paramIdPosScales = new Vector4[MAX_LOD_COUNT];
        Vector4[] _paramIdOceans = new Vector4[MAX_LOD_COUNT];
        protected virtual void BindData(IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData[] renderData, bool prevFrame = false)
        {
            if (applyData)
            {
                properties.SetTexture(GetParamIdSampler(prevFrame), applyData);
            }

            for(int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                var lt = OceanRenderer.Instance._lods[lodIdx];

                // NOTE: gets zeroed by unity, see https://www.alanzucconi.com/2016/10/24/arrays-shaders-unity-5-4/
                _paramIdPosScales[lodIdx] = new Vector4(renderData[lodIdx]._posSnapped.x, renderData[lodIdx]._posSnapped.z, lt.transform.lossyScale.x, 0);
                _paramIdOceans[lodIdx] = new Vector4(renderData[lodIdx]._texelWidth, renderData[lodIdx]._textureRes, 1f, 1f / renderData[lodIdx]._textureRes);
            }
            properties.SetVectorArray(LodTransform.ParamIdPosScale(prevFrame), _paramIdPosScales);
            properties.SetVectorArray(LodTransform.ParamIdOcean(prevFrame), _paramIdOceans);
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

        public void AddDraw(RegisterLodDataInputBase data)
        {
            if (OceanRenderer.Instance == null)
            {
                // Ocean has unloaded, clear out
                _drawList.Clear();
                return;
            }

            _drawList.Add(data);
        }

        public void RemoveDraw(RegisterLodDataInputBase data)
        {
            if (OceanRenderer.Instance == null)
            {
                // Ocean has unloaded, clear out
                _drawList.Clear();
                return;
            }

            _drawList.Remove(data);
        }

        protected void SwapRTs(ref RenderTexture o_a, ref RenderTexture o_b)
        {
            var temp = o_a;
            o_a = o_b;
            o_b = temp;
        }

        public interface IDrawFilter
        {
            bool Filter(RegisterLodDataInputBase data);
        }

        // TODO(MRT): Plan a way to encourage using compute shaders so that we
        // can run these asynchronously. Then use Graphics fences to manage when
        // these are run.
        protected void SubmitDraws(int lodIdx, CommandBuffer buf)
        {
            var lt = OceanRenderer.Instance._lods[lodIdx];
            lt._renderData.Validate(0, this);

            lt.SetViewProjectionMatrices(buf);

            foreach (var draw in _drawList)
            {
                buf.DrawRenderer(draw.RendererComponent, draw.RendererComponent.sharedMaterial);
            }
        }

        protected void SubmitDrawsFiltered(int lodIdx, CommandBuffer buf, IDrawFilter filter)
        {
            var lt = OceanRenderer.Instance._lods[lodIdx];
            lt._renderData.Validate(0, this);

            lt.SetViewProjectionMatrices(buf);

            foreach (var draw in _drawList)
            {
                if (filter.Filter(draw))
                {
                    buf.DrawRenderer(draw.RendererComponent, draw.RendererComponent.sharedMaterial);
                }
            }
        }
    }
}
