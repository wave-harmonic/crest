// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;

namespace Crest
{
    public abstract class LodData : MonoBehaviour
    {
        public abstract SimSettingsBase CreateDefaultSettings();
        public abstract void UseSettings(SimSettingsBase settings);

        public abstract RenderTextureFormat TextureFormat { get; }
        public abstract int Depth { get; }
        public abstract CameraClearFlags CamClearFlags { get; }

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
        }
        public RenderData _renderData = new RenderData();

        // shape texture resolution
        int _shapeRes = -1;

        int _lodIndex = -1;
        int _lodCount = -1;
        public void InitLODData(int lodIndex, int lodCount) { _lodIndex = lodIndex; _lodCount = lodCount; }
        protected int LodIndex { get { return _lodIndex; } }
        protected int LodCount { get { return _lodCount; } }

        protected virtual void LateUpdateTransformData()
        {
            // ensure camera size matches geometry size - although the projection matrix is overridden, this is needed for unity shader uniforms
            Cam.orthographicSize = 2f * transform.lossyScale.x;

            // find snap period
            int width = Cam.targetTexture.width;
            // debug functionality to resize RT if different size was specified.
            if (_shapeRes == -1)
            {
                _shapeRes = width;
            }
            else if (width != _shapeRes)
            {
                Cam.targetTexture.Release();
                Cam.targetTexture.width = Cam.targetTexture.height = _shapeRes;
                Cam.targetTexture.Create();
            }
            _renderData._textureRes = (float)Cam.targetTexture.width;
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
        }

        public enum SimType
        {
            DynamicWaves,
            Foam,
            AnimatedWaves,
        }

        public static GameObject CreateLodData(int lodIdx, int lodCount, float baseVertDensity, SimType simType, Dictionary<System.Type, SimSettingsBase> cachedSettings)
        {
            var go = new GameObject(string.Format("{0}Cam{1}", simType.ToString(), lodIdx));

            LodData sim;
            switch (simType)
            {
                case SimType.AnimatedWaves:
                    sim = go.AddComponent<LodDataAnimatedWaves>();
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

        Camera _camera; protected Camera Cam { get { return _camera ?? (_camera = GetComponent<Camera>()); } }
    }
}
