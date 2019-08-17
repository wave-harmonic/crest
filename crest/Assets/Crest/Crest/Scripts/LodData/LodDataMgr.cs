// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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

        // NOTE: This MUST match the value in OceanLODData.hlsl, as it
        // determines the size of the texture arrays in the shaders.
        public const int MAX_LOD_COUNT = 16;

        protected abstract int GetParamIdSampler(LodIdType lodIdType = LodIdType.SmallerLod);

        protected abstract bool NeedToReadWriteTextureData { get; }

        protected RenderTexture[] _targets;

        public RenderTexture DataTexture(int index) { return _targets[index]; }

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

        public static RenderTexture[] CreateLodDataTextures(RenderTextureDescriptor desc, string name, bool needToReadWriteTextureData)
        {
            RenderTexture[] result = new RenderTexture[OceanRenderer.Instance.CurrentLodCount];
            for(int index = 0; index < result.Length; index++)
            {
                result[index] = new RenderTexture(desc);
                result[index].wrapMode = TextureWrapMode.Clamp;
                result[index].antiAliasing = 1;
                result[index].filterMode = FilterMode.Bilinear;
                result[index].anisoLevel = 0;
                result[index].useMipMap = false;
                result[index].name = name;
                result[index].enableRandomWrite = needToReadWriteTextureData;
                result[index].Create();
            }
            return result;
        }

        public static void RefreshLodDataTextures(RenderTexture[] textures, int shapeRes)
        {
            for(int index = 0; index < textures.Length; index++)
            {
                textures[index].Release();
                textures[index].width = textures[index].height = shapeRes;
                textures[index].Create();
            }
        }

        protected virtual void InitData()
        {
            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(TextureFormat), "The graphics device does not support the render texture format " + TextureFormat.ToString());

            Debug.Assert(OceanRenderer.Instance.CurrentLodCount <= MAX_LOD_COUNT);

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);
            _targets = CreateLodDataTextures(desc, SimName, NeedToReadWriteTextureData);
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
                RefreshLodDataTextures(_targets, _shapeRes);
            }

            // determine if this LOD has changed scale and by how much (in exponent of 2)
            float oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            if (_oceanLocalScalePrev == -1f) _oceanLocalScalePrev = oceanLocalScale;
            float ratio = oceanLocalScale / _oceanLocalScalePrev;
            _oceanLocalScalePrev = oceanLocalScale;
            float ratio_l2 = Mathf.Log(ratio) / Mathf.Log(2f);
            _scaleDifferencePow2 = Mathf.RoundToInt(ratio_l2);
        }

        public void BindResultTexture(IPropertyWrapper properties, int lodIndex, LodIdType lodIdType = LodIdType.SmallerLod)
        {
            BindLodTexture(properties, lodIndex < _targets.Length && lodIndex >= 0 ? (Texture) _targets[lodIndex] : Texture2D.blackTexture, lodIdType);
        }

        public void BindResultTexturesToBothLods(IPropertyWrapper properties, int lodIndex)
        {
            BindResultTexture(properties, lodIndex);
            BindResultTexture(properties, lodIndex + 1, LodIdType.BiggerLod);
        }

        public void BindOceanParams(IPropertyWrapper properties, bool blendOut = true, bool source = false)
        {
            BindData(properties, ref OceanRenderer.Instance._lodTransform._renderData, blendOut, source);
        }

        // Avoid heap allocations instead BindData
        private Vector4[] _BindData_paramIdPosScales = new Vector4[MAX_LOD_COUNT];
        // Used in child
        protected Vector4[] _BindData_paramIdOceans = new Vector4[MAX_LOD_COUNT];

        protected virtual void BindData(IPropertyWrapper properties, ref LodTransform.RenderData[] renderData, bool blendOut = true, bool source = false)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                // NOTE: gets zeroed by unity, see https://www.alanzucconi.com/2016/10/24/arrays-shaders-unity-5-4/
                _BindData_paramIdPosScales[lodIdx] = new Vector4(
                    renderData[lodIdx]._posSnapped.x, renderData[lodIdx]._posSnapped.z,
                    OceanRenderer.Instance.CalcLodScale(lodIdx), 0f);
                _BindData_paramIdOceans[lodIdx] = new Vector4(renderData[lodIdx]._texelWidth, renderData[lodIdx]._textureRes, 1f, 1f / renderData[lodIdx]._textureRes);
            }
            properties.SetVectorArray(LodTransform.ParamIdPosScale(source), _BindData_paramIdPosScales);
            properties.SetVectorArray(LodTransform.ParamIdOcean(source), _BindData_paramIdOceans);
        }

        protected virtual void BindLodTexture(IPropertyWrapper properties, Texture applyData, LodIdType lodIdType = LodIdType.SmallerLod)
        {
            properties.SetTexture(GetParamIdSampler(lodIdType), applyData);
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

        protected void SwapRTs(ref RenderTexture[] o_a, ref RenderTexture[] o_b)
        {
            var temp = o_a;
            o_a = o_b;
            o_b = temp;
        }

        public interface IDrawFilter
        {
            float Filter(ILodDataInput data, out int isTransition);
        }

        protected void SubmitDraws(int lodIdx, CommandBuffer buf)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            lt._renderData[lodIdx].Validate(0, this);

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
            lt._renderData[lodIdx].Validate(0, this);

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

        public enum LodIdType
        {
            SmallerLod,
            BiggerLod,
            SourceLod
        }

        protected struct TextureArrayParamIds
        {
            private int _paramId;
            private int _paramId_Bigger;
            private int _paramId_Source;
            public TextureArrayParamIds(string textureArrayName)
            {
                _paramId = Shader.PropertyToID(textureArrayName);
                // Note: string concatonation does generate a small amount of
                // garbage. However, this is called on initialisation so should
                // be ok for now? Something worth considering for the future if
                // we want to go garbage-free.
                _paramId_Bigger = Shader.PropertyToID(textureArrayName + "_BiggerLod");
                _paramId_Source = Shader.PropertyToID(textureArrayName + "_Source");
            }
            public int GetId(LodIdType lodIdType) {
                switch(lodIdType)
                {
                    case LodIdType.SmallerLod: return _paramId;
                    case LodIdType.BiggerLod: return _paramId_Bigger;
                    case LodIdType.SourceLod: return _paramId_Source;
                    default: Debug.Assert(false, this); return -1;
                }
            }
        }
    }
}
