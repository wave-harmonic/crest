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
        public string SimName { get { return LodDataType.ToString(); } }
        public abstract LodData.SimType LodDataType { get; }

        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }
        public abstract CameraClearFlags CamClearFlags { get; }

        public const int MAX_LOD_COUNT = 16;

        public virtual RenderTexture DataTexture(int lodIdx)
        {
            return _targets[lodIdx];
        }
        public LodTransform GetLodTransform(int lodIdx)
        {
            return transform.GetChild(lodIdx).GetComponent<LodTransform>();
        }

        public RenderTexture[] _targets;

        // shape texture resolution
        int _shapeRes = -1;
        // ocean scale last frame - used to detect scale changes
        float _oceanLocalScalePrev = -1f;
        protected int _scaleDifferencePow2 = 0;

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

            {
                _worldToCameraMatrix = CalculateWorldToCameraMatrixRHS(transform.position, Quaternion.AngleAxis(90f, Vector3.right));

                _projectionMatrix = Matrix4x4.Ortho(-2f * transform.lossyScale.x, 2f * transform.lossyScale.x, -2f * transform.lossyScale.x, 2f * transform.lossyScale.x, 1f, 500f);
            }
        }

        // Borrowed from LWRP code: https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/2a68d8073c4eeef7af3be9e4811327a522434d5f/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs
        public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        }

        public Matrix4x4 _worldToCameraMatrix;
        public Matrix4x4 _projectionMatrix;

        protected PropertyWrapperMaterial _pwMat = new PropertyWrapperMaterial();
        protected PropertyWrapperMPB _pwMPB = new PropertyWrapperMPB();

        public void BindResultData(int lodIdx, int shapeSlot, Material properties)
        {
            _pwMat._target = properties;
            BindData(lodIdx, shapeSlot, _pwMat, DataTexture(lodIdx), true, ref GetLodTransform(lodIdx)._renderData);
            _pwMat._target = null;
        }

        public void BindResultData(int lodIdx, int shapeSlot, MaterialPropertyBlock properties)
        {
            _pwMPB._target = properties;
            BindData(lodIdx, shapeSlot, _pwMPB, DataTexture(lodIdx), true, ref GetLodTransform(lodIdx)._renderData);
            _pwMPB._target = null;
        }

        public void BindResultData(int lodIdx, int shapeSlot, Material properties, bool blendOut)
        {
            _pwMat._target = properties;
            BindData(lodIdx, shapeSlot, _pwMat, DataTexture(lodIdx), blendOut, ref GetLodTransform(lodIdx)._renderData);
            _pwMat._target = null;
        }

        protected virtual void BindData(int lodIdx, int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData)
        {
            if (applyData)
            {
                properties.SetTexture(_paramsLodDataSampler[shapeSlot], applyData);
            }

            properties.SetVector(_paramsPosScale[shapeSlot], new Vector3(renderData._posSnapped.x, renderData._posSnapped.z, transform.lossyScale.x));
            properties.SetFloat(_paramsLodIdx[shapeSlot], GetLodTransform(lodIdx).LodIndex);
            properties.SetVector(_paramsOceanParams[shapeSlot],
                new Vector4(renderData._texelWidth, renderData._textureRes, 1f, 1f / renderData._textureRes));
        }

        public static LodDataMgr Create(int lodCount, GameObject attachGO, float baseVertDensity, LodData.SimType simType, Dictionary<System.Type, SimSettingsBase> cachedSettings)
        {
            LodDataMgr sim;
            switch (simType)
            {
                case LodData.SimType.Flow:
                    sim = attachGO.AddComponent<LodDataMgrFlow>();
                    break;
                default:
                    Debug.LogError("Unknown sim type: " + simType.ToString());
                    return null;
            }

            // create a shared settings object if one doesnt already exist
            SimSettingsBase settings;
            if (!cachedSettings.TryGetValue(sim.GetType(), out settings))
            {
                settings = sim.CreateDefaultSettings();
                cachedSettings.Add(sim.GetType(), settings);
            }
            sim.UseSettings(settings);

            sim._targets = new RenderTexture[lodCount];
            var desc = new RenderTextureDescriptor((int)(4 * baseVertDensity), (int)(4 * baseVertDensity), sim.TextureFormat, 0);
            for (int i = 0; i < sim._targets.Length; i++)
            {
                sim._targets[i] = new RenderTexture(desc);
                sim._targets[i].wrapMode = TextureWrapMode.Clamp;
                sim._targets[i].antiAliasing = 1;
                sim._targets[i].filterMode = FilterMode.Bilinear;
                sim._targets[i].anisoLevel = 0;
                sim._targets[i].useMipMap = false;
                sim._targets[i].name = simType.ToString() + "_" + i;
            }

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
    }
}
