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
        public enum SimType
        {
            DynamicWaves,
            Foam,
            AnimatedWaves,
            SeaFloorDepth,
            Flow,
            Shadow,
        }

        public string SimName { get { return LodDataType.ToString(); } }
        public abstract SimType LodDataType { get; }

        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }

        public const int MAX_LOD_COUNT = 16;

        public virtual RenderTexture DataTexture(int lodIdx)
        {
            return _targets[lodIdx];
        }

        protected RenderTexture[] _targets;

        // shape texture resolution
        int _shapeRes = -1;
        // ocean scale last frame - used to detect scale changes
        float _oceanLocalScalePrev = -1f;

        int _scaleDifferencePow2 = 0;
        protected int ScaleDifferencePow2 { get { return _scaleDifferencePow2; } }

        protected List<Renderer> _drawList = new List<Renderer>();

        // these would ideally be static but then they get cleared when editing-and-continuing in the editor.
        int[] _paramsPosScale;
        int[] _paramsLodIdx;
        protected int[] _paramsOceanParams;
        int[] _paramsLodDataSampler;

        protected virtual void Start()
        {
            // create shader param IDs for each LOD once on start to avoid creating garbage each frame.
            if (_paramsLodDataSampler == null)
            {
                CreateParamIDs(ref _paramsLodDataSampler, "_LD_Sampler_" + SimName + "_");
                CreateParamIDs(ref _paramsOceanParams, "_LD_Params_");
                CreateParamIDs(ref _paramsPosScale, "_LD_Pos_Scale_");
                CreateParamIDs(ref _paramsLodIdx, "_LD_LodIdx_");
            }

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

        protected virtual void LateUpdate()
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

            // determine if this lod has changed scale and by how much (in exponent of 2)
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
                properties.SetTexture(_paramsLodDataSampler[shapeSlot], applyData);
            }

            var lt = OceanRenderer.Instance._lods[lodIdx];
            properties.SetVector(_paramsPosScale[shapeSlot], new Vector3(renderData._posSnapped.x, renderData._posSnapped.z, lt.transform.lossyScale.x));
            properties.SetFloat(_paramsLodIdx[shapeSlot], lt.LodIndex);
            properties.SetVector(_paramsOceanParams[shapeSlot],
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

        protected void CreateParamIDs(ref int[] ids, string prefix)
        {
            int count = 2;
            ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = Shader.PropertyToID(string.Format("{0}{1}", prefix, i));
            }
        }

        public virtual void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
        }

        public void AddDraw(Renderer rend)
        {
            if (OceanRenderer.Instance == null)
            {
                // Ocean has unloaded, clear out
                _drawList.Clear();
                return;
            }

            _drawList.Add(rend);
        }

        public void RemoveDraw(Renderer rend)
        {
            if (OceanRenderer.Instance == null)
            {
                // Ocean has unloaded, clear out
                _drawList.Clear();
                return;
            }

            _drawList.Remove(rend);
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
                buf.DrawRenderer(draw, draw.material);
            }
        }
    }
}
