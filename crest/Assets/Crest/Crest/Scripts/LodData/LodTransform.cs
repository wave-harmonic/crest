// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// This script is attached to the parent GameObject of each LOD. It provides helper functionality related to each LOD.
    /// </summary>
    // TODO(MRT): LodTransformSOA See if we can make the is a struct of arrays or something similar.
    // (NOTE: This affects a lot of TODOs, these are start with "TODO(MRT): LodTransformSOA" (including this one!)
    public class LodTransform : MonoBehaviour, IFloatingOrigin
    {
        protected int[] _transformUpdateFrame;

        static int s_paramsPosScaleThisFrame = Shader.PropertyToID("_LD_Pos_Scale");
        static int s_paramsPosScalePrevFrame = Shader.PropertyToID("_LD_Pos_Scale_PrevFrame");
        static int s_paramsOceanThisFrame = Shader.PropertyToID("_LD_Params");
        static int s_paramsOceanPrevFrame = Shader.PropertyToID("_LD_Params_PrevFrame");

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
            public int _frame;

            // TODO(MRT): LodTransformSOA Rewrite this function for SOA (a lot of places cannot use it atm)
            public RenderData Validate(int frameOffset, Object context)
            {
                // ignore first frame - this patches errors when using edit & continue in editor
                if (_frame > 0 && _frame != Time.frameCount + frameOffset)
                {
                    Debug.LogWarning(string.Format("RenderData validation failed: _frame of data ({0}) != expected ({1}), which may indicate some update functions are being called out of order, or script execution order is broken.", _frame, Time.frameCount + frameOffset), context);
                }
                return this;
            }

            public Rect RectXZ
            {
                get
                {
                    float w = _texelWidth * _textureRes;
                    return new Rect(_posSnapped.x - w / 2f, _posSnapped.z - w / 2f, w, w);
                }
            }
        }

        public RenderData[] _renderData = null;
        public RenderData[] _renderDataPrevFrame = null;

        public int LodCount { get; private set; }

        Matrix4x4[] _worldToCameraMatrix;
        Matrix4x4[] _projectionMatrix;
        public Matrix4x4 GetWorldToCameraMatrix(int lodIdx) { return _worldToCameraMatrix[lodIdx]; }
        public Matrix4x4 GetProjectionMatrix(int lodIdx) { return _projectionMatrix[lodIdx]; }

        public void InitLODData(int lodCount)
        {
            LodCount = lodCount;

            _renderData = new RenderData[lodCount];
            _renderDataPrevFrame = new RenderData[lodCount];
            _worldToCameraMatrix = new Matrix4x4[lodCount];
            _projectionMatrix = new Matrix4x4[lodCount];

            _transformUpdateFrame = new int[lodCount];
            for (int i = 0; i < _transformUpdateFrame.Length; i++)
            {
                _transformUpdateFrame[i] = -1;
            }
        }

        public void UpdateTransform(int lodIdx)
        {
            if (_transformUpdateFrame[lodIdx] == Time.frameCount)
                return;

            _transformUpdateFrame[lodIdx] = Time.frameCount;

            _renderDataPrevFrame[lodIdx] = _renderData[lodIdx];

            var lodTransform = GetLodTransform(lodIdx);

            float camOrthSize = 2f * lodTransform.lossyScale.x;

            // find snap period
            _renderData[lodIdx]._textureRes = OceanRenderer.Instance.LodDataResolution;
            _renderData[lodIdx]._texelWidth = 2f * camOrthSize / _renderData[lodIdx]._textureRes;
            // snap so that shape texels are stationary
            _renderData[lodIdx]._posSnapped = lodTransform.position
                - new Vector3(Mathf.Repeat(lodTransform.position.x, _renderData[lodIdx]._texelWidth), 0f, Mathf.Repeat(lodTransform.position.z, _renderData[lodIdx]._texelWidth));

            _renderData[lodIdx]._frame = Time.frameCount;

            // detect first update and populate the render data if so - otherwise it can give divide by 0s and other nastiness
            if (_renderDataPrevFrame[lodIdx]._textureRes == 0f)
            {
                _renderDataPrevFrame[lodIdx]._posSnapped = _renderData[lodIdx]._posSnapped;
                _renderDataPrevFrame[lodIdx]._texelWidth = _renderData[lodIdx]._texelWidth;
                _renderDataPrevFrame[lodIdx]._textureRes = _renderData[lodIdx]._textureRes;
            }

            _worldToCameraMatrix[lodIdx] = CalculateWorldToCameraMatrixRHS(_renderData[lodIdx]._posSnapped + Vector3.up * 100f, Quaternion.AngleAxis(90f, Vector3.right));

            _projectionMatrix[lodIdx] = Matrix4x4.Ortho(-2f * lodTransform.lossyScale.x, 2f * lodTransform.lossyScale.x, -2f * lodTransform.lossyScale.z, 2f * lodTransform.lossyScale.z, 1f, 500f);
        }

        // Borrowed from LWRP code: https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/2a68d8073c4eeef7af3be9e4811327a522434d5f/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs
        public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        }

        public void SetViewProjectionMatrices(int lodIdx, CommandBuffer buf)
        {
            buf.SetViewProjectionMatrices(GetWorldToCameraMatrix(lodIdx), GetProjectionMatrix(lodIdx));
        }

        public float MaxWavelength(int lodIdx)
        {
            float oceanBaseScale = OceanRenderer.Instance.transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, lodIdx);
            float maxTexelSize = maxDiameter / OceanRenderer.Instance.LodDataResolution;
            return 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;
        }

        public static int ParamIdPosScale(bool prevFrame = false)
        {
            if(prevFrame)
            {
                return s_paramsPosScalePrevFrame;
            }
            else
            {
                return s_paramsPosScaleThisFrame;
            }
        }

        public static int ParamIdOcean(bool prevFrame = false)
        {
            if(prevFrame)
            {
                return s_paramsOceanPrevFrame;
            }
            else
            {
                return s_paramsOceanThisFrame;
            }
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            for(int lodIdx = 0; lodIdx < LodCount; lodIdx++)
            {
                _renderData[lodIdx]._posSnapped -= newOrigin;
                _renderDataPrevFrame[lodIdx]._posSnapped -= newOrigin;
            }
        }

        public Transform GetLodTransform(int lodIdx)
        {
            return transform.GetChild(lodIdx);
        }
    }
}
