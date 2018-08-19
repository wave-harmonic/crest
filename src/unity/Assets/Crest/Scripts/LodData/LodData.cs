// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;

namespace Crest
{
    public abstract class LodData : MonoBehaviour
    {
        public string SimName { get { return LodDataType.ToString(); } }
        public abstract SimType LodDataType { get; }

        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }
        public abstract int Depth { get; }
        public abstract CameraClearFlags CamClearFlags { get; }
        public abstract RenderTexture DataTexture { get; }

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
            public int _frame;
            public RenderData Validate(int frameOffset, Object context)
            {
                if (_frame != Time.frameCount + frameOffset)
                    Debug.LogError(string.Format("Validation failed: _frame of data ({0}) != expected ({1})", _frame, Time.frameCount + frameOffset), context);

                return this;
            }
        }
        public RenderData _renderData = new RenderData();
        public RenderData _renderDataPrevFrame = new RenderData();

        // shape texture resolution
        int _shapeRes = -1;

        int _lodIndex = -1;
        int _lodCount = -1;
        public void InitLODData(int lodIndex, int lodCount) { _lodIndex = lodIndex; _lodCount = lodCount; }
        protected int LodIndex { get { return _lodIndex; } }
        protected int LodCount { get { return _lodCount; } }

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

        protected int _transformUpdateFrame = -1;

        protected virtual void LateUpdateTransformData()
        {
            if (_transformUpdateFrame == Time.frameCount)
                return;

            _transformUpdateFrame = Time.frameCount;

            _renderDataPrevFrame = _renderData;

            // ensure camera size matches geometry size - although the projection matrix is overridden, this is needed for unity shader uniforms
            Cam.orthographicSize = 2f * transform.lossyScale.x;

            // find snap period
            int width = DataTexture.width;
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
            _renderData._textureRes = DataTexture.width;
            _renderData._texelWidth = 2f * Cam.orthographicSize / _renderData._textureRes;
            // snap so that shape texels are stationary
            _renderData._posSnapped = transform.position
                - new Vector3(Mathf.Repeat(transform.position.x, _renderData._texelWidth), 0f, Mathf.Repeat(transform.position.z, _renderData._texelWidth));

            // set projection matrix to snap to texels
            Cam.ResetProjectionMatrix();
            Matrix4x4 P = Cam.projectionMatrix, T = new Matrix4x4();
            T.SetTRS(new Vector3(transform.position.x - _renderData._posSnapped.x, transform.position.z - _renderData._posSnapped.z), Quaternion.identity, Vector3.one);
            P = P * T;
            Cam.projectionMatrix = P;

            _renderData._frame = Time.frameCount;

            // detect first update and populate the render data if so - otherwise it can give divide by 0s and other nastiness
            if (_renderDataPrevFrame._textureRes == 0f)
            {
                _renderDataPrevFrame._posSnapped = _renderData._posSnapped;
                _renderDataPrevFrame._texelWidth = _renderData._texelWidth;
                _renderDataPrevFrame._textureRes = _renderData._textureRes;
            }
        }

        protected PropertyWrapperMaterial _pwMat = new PropertyWrapperMaterial();
        protected PropertyWrapperMPB _pwMPB = new PropertyWrapperMPB();

        public void BindResultData(int shapeSlot, Material properties)
        {
            _pwMat._target = properties;
            BindData(shapeSlot, _pwMat, DataTexture, true, ref _renderData);
            _pwMat._target = null;
        }

        public void BindResultData(int shapeSlot, MaterialPropertyBlock properties)
        {
            _pwMPB._target = properties;
            BindData(shapeSlot, _pwMPB, DataTexture, true, ref _renderData);
            _pwMPB._target = null;
        }

        public void BindResultData(int shapeSlot, Material properties, bool blendOut)
        {
            _pwMat._target = properties;
            BindData(shapeSlot, _pwMat, DataTexture, blendOut, ref _renderData);
            _pwMat._target = null;
        }

        protected virtual void BindData(int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref RenderData renderData)
        {
            if (applyData)
            {
                properties.SetTexture(_paramsLodDataSampler[shapeSlot], applyData);
            }

            properties.SetVector(_paramsPosScale[shapeSlot], new Vector3(renderData._posSnapped.x, renderData._posSnapped.z, transform.lossyScale.x));
            properties.SetFloat(_paramsLodIdx[shapeSlot], LodIndex);
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
        }

        public static GameObject CreateLodData(int lodIdx, int lodCount, float baseVertDensity, SimType simType, Dictionary<System.Type, SimSettingsBase> cachedSettings)
        {
            var go = new GameObject(string.Format("{0}Cam{1}", simType.ToString(), lodIdx));

            LodData sim;
            switch (simType)
            {
                case SimType.AnimatedWaves:
                    sim = go.AddComponent<LodDataAnimatedWaves>();
                    go.AddComponent<LodDataSeaFloorDepth>();
                    go.AddComponent<ReadbackDisplacementsForCollision>();
                    break;
                case SimType.DynamicWaves:
                    sim = go.AddComponent<LodDataDynamicWaves>();
                    break;
                case SimType.Foam:
                    sim = go.AddComponent<LodDataFoam>();
                    break;
                default:
                    Debug.LogError("Unknown sim type: " + simType.ToString());
                    return null;
            }
            sim.InitLODData(lodIdx, lodCount);

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
            cam.depth = sim.Depth - lodIdx;
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
        LodDataSeaFloorDepth _ldsd;
        public LodDataSeaFloorDepth LDSeaDepth { get { return _ldsd ?? (_ldsd = GetComponent<LodDataSeaFloorDepth>()); } }
        LodDataFoam _ldf; public LodDataFoam LDFoam { get {
                return _ldf ?? (_ldf = OceanRenderer.Instance.Builder._camsFoam[LodIndex].GetComponent<LodDataFoam>());
        } }
    }
}
