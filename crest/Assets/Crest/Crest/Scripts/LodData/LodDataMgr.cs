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

        public const int MAX_LOD_COUNT = 16;

        protected abstract int GetParamIdSampler(bool prevFrame = false);

        protected RenderTexture _targets;

        public RenderTexture DataTexture { get { return _targets; } }

        public const int SLICE_COUNT = 16; // MUST match the value in OceanLODData.hlsl

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
            Debug.Assert(_targets.depth <= SLICE_COUNT);
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

        public void BindResultData(int lodIdx, IPropertyWrapper properties, bool prevFrame = false)
        {
            BindData(lodIdx, properties, _targets, true, ref OceanRenderer.Instance._lods[lodIdx]._renderData, prevFrame);
        }

        public void BindResultData(int lodIdx, IPropertyWrapper properties, bool blendOut, bool prevFrame = false)
        {
            BindData(lodIdx, properties, _targets, blendOut, ref OceanRenderer.Instance._lods[lodIdx]._renderData, prevFrame);
        }

        protected virtual void BindData(int lodIdx, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData, bool prevFrame = false)
        {
            if (applyData)
            {
                properties.SetTexture(GetParamIdSampler(prevFrame), applyData);
                properties.SetFloat(Shader.PropertyToID("_LD_SLICE_Index_ThisLod"), lodIdx);
            }

            var lt = OceanRenderer.Instance._lods[lodIdx];
            properties.SetVector(LodTransform.ParamIdPosScale(prevFrame), new Vector3(renderData._posSnapped.x, renderData._posSnapped.z, lt.transform.lossyScale.x));
            properties.SetVector(LodTransform.ParamIdOcean(prevFrame),
                new Vector4(renderData._texelWidth, renderData._textureRes, 1f, 1f / renderData._textureRes));
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
