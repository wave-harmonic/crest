// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Base class for data/behaviours created on each LOD.
    /// </summary>
    public abstract class LodData : MonoBehaviour
    {
        public string SimName { get { return LodDataType.ToString(); } }
        public abstract SimType LodDataType { get; }

        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }
        public abstract CameraClearFlags CamClearFlags { get; }
        public abstract RenderTexture DataTexture { get; }


        // shape texture resolution
        int _shapeRes = -1;

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
                DataTexture.Release();
                DataTexture.width = DataTexture.height = _shapeRes;
                DataTexture.Create();
            }
        }

        protected PropertyWrapperMaterial _pwMat = new PropertyWrapperMaterial();
        protected PropertyWrapperMPB _pwMPB = new PropertyWrapperMPB();

        public void BindResultData(int shapeSlot, Material properties)
        {
            _pwMat._target = properties;
            BindData(shapeSlot, _pwMat, DataTexture, true, ref LodTransform._renderData);
            _pwMat._target = null;
        }

        public void BindResultData(int shapeSlot, MaterialPropertyBlock properties)
        {
            _pwMPB._target = properties;
            BindData(shapeSlot, _pwMPB, DataTexture, true, ref LodTransform._renderData);
            _pwMPB._target = null;
        }

        public void BindResultData(int shapeSlot, Material properties, bool blendOut)
        {
            _pwMat._target = properties;
            BindData(shapeSlot, _pwMat, DataTexture, blendOut, ref LodTransform._renderData);
            _pwMat._target = null;
        }

        protected virtual void BindData(int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData)
        {
            if (applyData)
            {
                properties.SetTexture(_paramsLodDataSampler[shapeSlot], applyData);
            }

            properties.SetVector(_paramsPosScale[shapeSlot], new Vector3(renderData._posSnapped.x, renderData._posSnapped.z, transform.lossyScale.x));
            properties.SetFloat(_paramsLodIdx[shapeSlot], LodTransform.LodIndex);
            properties.SetVector(_paramsOceanParams[shapeSlot],
                new Vector4(renderData._texelWidth, renderData._textureRes, 1f, 1f / renderData._textureRes));
        }

        public enum SimType
        {
            DynamicWaves,
            Foam,
            AnimatedWaves,
            // this is currently not used as the sea floor depth is not created as a unique sim object
            SeaFloorDepth,
            Flow,
        }

        public static GameObject CreateLodData(int lodIdx, int lodCount, float baseVertDensity, SimType simType, Dictionary<System.Type, SimSettingsBase> cachedSettings)
        {
            var go = new GameObject(string.Format("{0}Cam{1}", simType.ToString(), lodIdx));

            go.AddComponent<LodTransform>().InitLODData(lodIdx, lodCount); ;

            LodData sim;
            switch (simType)
            {
                case SimType.AnimatedWaves:
                    sim = go.AddComponent<LodDataAnimatedWaves>();
                    go.AddComponent<LodDataSeaFloorDepth>();
                    go.AddComponent<ReadbackLodData>();
                    break;
                case SimType.DynamicWaves:
                    sim = go.AddComponent<LodDataDynamicWaves>();
                    break;
                case SimType.Foam:
                    sim = go.AddComponent<LodDataFoam>();
                    break;
                case SimType.Flow:
                    sim = go.AddComponent<LodDataFlow>();
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

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = sim.CamClearFlags;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 0;
            cam.orthographic = true;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 500f;
            cam.renderingPath = RenderingPath.Forward;
            cam.useOcclusionCulling = false;
            cam.allowHDR = true;
            cam.allowMSAA = false;
            cam.allowDynamicResolution = false;

            var cart = go.AddComponent<CreateAssignRenderTexture>();
            cart._targetName = go.name;
            cart._width = cart._height = (int)(4f * baseVertDensity);
            cart._depthBits = 0;
            cart._format = sim.TextureFormat;
            cart._wrapMode = TextureWrapMode.Clamp;
            cart._antiAliasing = 1;
            cart._filterMode = FilterMode.Bilinear;
            cart._anisoLevel = 0;
            cart._useMipMap = false;
            cart._createPingPongTargets = sim as LodDataPersistent != null;
            cart.Create();

            var apply = go.AddComponent<ApplyLayers>();
            apply._cullIncludeLayers = new string[] { string.Format("LodData{0}", simType.ToString()) };

            return go;
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

        Camera _camera; protected Camera Cam { get { return _camera ?? (_camera = GetComponent<Camera>()); } }
        LodTransform _lt; public LodTransform LodTransform { get { return _lt ?? (_lt = GetComponent<LodTransform>()); } }
        LodDataSeaFloorDepth _ldsd;
        public LodDataSeaFloorDepth LDSeaDepth { get {
                return _ldsd ?? (_ldsd = GetComponent<LodDataSeaFloorDepth>());
        } }
        LodDataFoam _ldf;
        public LodDataFoam LDFoam { get {
                return _ldf ?? (_ldf = OceanRenderer.Instance.Builder._camsFoam[LodTransform.LodIndex].GetComponent<LodDataFoam>());
        } }
        LodDataDynamicWaves _lddw;
        public LodDataDynamicWaves LDDynamicWaves { get {
                return _lddw ?? (_lddw = OceanRenderer.Instance.Builder._camsDynWaves[LodTransform.LodIndex].GetComponent<LodDataDynamicWaves>());
        } }
        LodDataFlow _ldfl;
        public LodDataFlow LDFlow { get {
                return _ldfl ?? (_ldfl = OceanRenderer.Instance.Builder._camsFlow[LodTransform.LodIndex].GetComponent<LodDataFlow>());
        } }
    }
}
