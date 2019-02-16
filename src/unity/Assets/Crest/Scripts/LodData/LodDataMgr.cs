// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DrawFilter = System.Func<Crest.RegisterLodDataInputBase, bool>;

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

        public virtual RenderTexture DataTexture(int lodIdx)
        {
            return _targets[lodIdx];
        }

        protected abstract int GetParamIdSampler(int slot);

        protected RenderTexture[] _targets;

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
            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);

            _targets = new RenderTexture[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _targets.Length; i++)
            {
                _targets[i] = new RenderTexture(desc);
                _targets[i].wrapMode = TextureWrapMode.Clamp;
                _targets[i].antiAliasing = 1;
                _targets[i].filterMode = FilterMode.Bilinear;
                _targets[i].anisoLevel = 0;
                _targets[i].useMipMap = false;
                _targets[i].name = SimName + "_" + i + "_0";
            }
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
                for(int i = 0; i < OceanRenderer.Instance.CurrentLodCount; i++)
                {
                    var tex = DataTexture(i);
                    tex.Release();
                    tex.width = tex.height = _shapeRes;
                    tex.Create();
                }
            }

            // determine if this LOD has changed scale and by how much (in exponent of 2)
            float oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            if (_oceanLocalScalePrev == -1f) _oceanLocalScalePrev = oceanLocalScale;
            float ratio = oceanLocalScale / _oceanLocalScalePrev;
            _oceanLocalScalePrev = oceanLocalScale;
            float ratio_l2 = Mathf.Log(ratio) / Mathf.Log(2f);
            _scaleDifferencePow2 = Mathf.RoundToInt(ratio_l2);
        }

        protected PropertyWrapperMaterial _pwMat = new PropertyWrapperMaterial();
        protected PropertyWrapperMPB _pwMPB = new PropertyWrapperMPB();

        public void BindResultData(int lodIdx, int shapeSlot, Material properties)
        {
            _pwMat._target = properties;
            BindData(lodIdx, shapeSlot, _pwMat, DataTexture(lodIdx), true, ref OceanRenderer.Instance._lods[lodIdx]._renderData);
            _pwMat._target = null;
        }

        public void BindResultData(int lodIdx, int shapeSlot, MaterialPropertyBlock properties)
        {
            _pwMPB._target = properties;
            BindData(lodIdx, shapeSlot, _pwMPB, DataTexture(lodIdx), true, ref OceanRenderer.Instance._lods[lodIdx]._renderData);
            _pwMPB._target = null;
        }

        public void BindResultData(int lodIdx, int shapeSlot, Material properties, bool blendOut)
        {
            _pwMat._target = properties;
            BindData(lodIdx, shapeSlot, _pwMat, DataTexture(lodIdx), blendOut, ref OceanRenderer.Instance._lods[lodIdx]._renderData);
            _pwMat._target = null;
        }

        protected virtual void BindData(int lodIdx, int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData)
        {
            if (applyData)
            {
                properties.SetTexture(GetParamIdSampler(shapeSlot), applyData);
            }

            var lt = OceanRenderer.Instance._lods[lodIdx];
            properties.SetVector(LodTransform.ParamIdPosScale(shapeSlot), new Vector3(renderData._posSnapped.x, renderData._posSnapped.z, lt.transform.lossyScale.x));
            properties.SetVector(LodTransform.ParamIdOcean(shapeSlot),
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

        protected void SubmitDraws(int lodIdx, CommandBuffer buf)
        {
            var lt = OceanRenderer.Instance._lods[lodIdx];
            lt._renderData.Validate(0, this);

            lt.SetViewProjectionMatrices(buf);

            foreach (var draw in _drawList)
            {
                buf.DrawRenderer(draw.RendererComponent, draw.RendererComponent.material);
            }
        }

        protected void SubmitDrawsFiltered(int lodIdx, CommandBuffer buf, DrawFilter filter)
        {
            var lt = OceanRenderer.Instance._lods[lodIdx];
            lt._renderData.Validate(0, this);

            lt.SetViewProjectionMatrices(buf);

            foreach (var draw in _drawList)
            {
                if (filter(draw))
                {
                    buf.DrawRenderer(draw.RendererComponent, draw.RendererComponent.material);
                }
            }
        }
    }
}
