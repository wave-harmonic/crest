// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// This script is attached to the parent GameObject of each LOD. It provides helper functionality related to each LOD.
    /// </summary>
    public class LodTransform : IFloatingOrigin
    {
        protected int[] _transformUpdateFrame;

        [System.Serializable]
        public class RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
            public int _frame;
            public float _maxWavelength;

            public RenderData Validate(int frameOffset, string context)
            {
                // ignore first frame - this patches errors when using edit & continue in editor
                if (_frame > 0 && _frame != OceanRenderer.FrameCount + frameOffset)
                {
                    Debug.LogWarning($"RenderData validation failed - {context} - _frame of data ({_frame}) != expected ({OceanRenderer.FrameCount + frameOffset}), which may indicate some update functions are being called out of order, or script execution order is broken.", OceanRenderer.Instance);
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

        public BufferedData<RenderData>[] _renderData;

        public int LodCount { get; private set; }

        Matrix4x4[] _worldToCameraMatrix;
        Matrix4x4[] _projectionMatrix;
        public Matrix4x4 GetWorldToCameraMatrix(int lodIdx) { return _worldToCameraMatrix[lodIdx]; }
        public Matrix4x4 GetProjectionMatrix(int lodIdx) { return _projectionMatrix[lodIdx]; }

        public void InitLODData(int lodCount)
        {
            LodCount = lodCount;

            _renderData = new BufferedData<RenderData>[lodCount];
            for (var i = 0; i < lodCount; i++)
            {
                _renderData[i] = new BufferedData<RenderData>(3, () => new RenderData());
            }

            _worldToCameraMatrix = new Matrix4x4[lodCount];
            _projectionMatrix = new Matrix4x4[lodCount];

            _transformUpdateFrame = new int[lodCount];
            for (int i = 0; i < _transformUpdateFrame.Length; i++)
            {
                _transformUpdateFrame[i] = -1;
            }
        }

        public void UpdateTransforms()
        {
            for (int lodIdx = 0; lodIdx < LodCount; lodIdx++)
            {
                if (_transformUpdateFrame[lodIdx] == OceanRenderer.FrameCount) continue;

                _transformUpdateFrame[lodIdx] = OceanRenderer.FrameCount;

                var lodScale = OceanRenderer.Instance.CalcLodScale(lodIdx);
                var camOrthSize = 2f * lodScale;

                // find snap period
                _renderData[lodIdx].Current._textureRes = OceanRenderer.Instance.LodDataResolution;
                _renderData[lodIdx].Current._texelWidth = 2f * camOrthSize / _renderData[lodIdx].Current._textureRes;
                // snap so that shape texels are stationary
                _renderData[lodIdx].Current._posSnapped = OceanRenderer.Instance.Root.position
                    - new Vector3(Mathf.Repeat(OceanRenderer.Instance.Root.position.x, _renderData[lodIdx].Current._texelWidth), 0f, Mathf.Repeat(OceanRenderer.Instance.Root.position.z, _renderData[lodIdx].Current._texelWidth));

                _renderData[lodIdx].Current._frame = OceanRenderer.FrameCount;

                _renderData[lodIdx].Current._maxWavelength = MaxWavelength(lodIdx);

                // TODO?
                // // detect first update and populate the render data if so - otherwise it can give divide by 0s and other nastiness
                // if (_renderDataSource[lodIdx].Current._textureRes == 0f)
                // {
                //     _renderDataSource[lodIdx].Current._posSnapped = _renderData[lodIdx].Current._posSnapped;
                //     _renderDataSource[lodIdx].Current._texelWidth = _renderData[lodIdx].Current._texelWidth;
                //     _renderDataSource[lodIdx].Current._textureRes = _renderData[lodIdx].Current._textureRes;
                //     _renderDataSource[lodIdx].Current._maxWavelength = _renderData[lodIdx].Current._maxWavelength;
                // }

                _worldToCameraMatrix[lodIdx] = CalculateWorldToCameraMatrixRHS(_renderData[lodIdx].Current._posSnapped + Vector3.up * 100f, Quaternion.AngleAxis(90f, Vector3.right));

                _projectionMatrix[lodIdx] = Matrix4x4.Ortho(-2f * lodScale, 2f * lodScale, -2f * lodScale, 2f * lodScale, 1f, 500f);
            }
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
            float oceanBaseScale = OceanRenderer.Instance.Scale;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, lodIdx);
            float maxTexelSize = maxDiameter / OceanRenderer.Instance.LodDataResolution;
            return 2f * maxTexelSize * OceanRenderer.Instance.MinTexelsPerWave;
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            for (var lodIdx = 0; lodIdx < LodCount; lodIdx++)
            {
                _renderData[lodIdx].RunLambda(renderData => renderData._posSnapped -= newOrigin);
            }
        }

        public void WriteCascadeParams(OceanRenderer.CascadeParams[] cascadeParamsTgt, OceanRenderer.CascadeParams[] cascadeParamsSrc)
        {
            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                cascadeParamsTgt[lodIdx]._posSnapped[0] = _renderData[lodIdx].Current._posSnapped[0];
                cascadeParamsTgt[lodIdx]._posSnapped[1] = _renderData[lodIdx].Current._posSnapped[2];
                cascadeParamsSrc[lodIdx]._posSnapped[0] = _renderData[lodIdx].Previous(1)._posSnapped[0];
                cascadeParamsSrc[lodIdx]._posSnapped[1] = _renderData[lodIdx].Previous(1)._posSnapped[2];

                cascadeParamsTgt[lodIdx]._scale = cascadeParamsSrc[lodIdx]._scale = OceanRenderer.Instance.CalcLodScale(lodIdx);

                cascadeParamsTgt[lodIdx]._textureRes = _renderData[lodIdx].Current._textureRes;
                cascadeParamsSrc[lodIdx]._textureRes = _renderData[lodIdx].Previous(1)._textureRes;

                cascadeParamsTgt[lodIdx]._oneOverTextureRes = 1f / cascadeParamsTgt[lodIdx]._textureRes;
                cascadeParamsSrc[lodIdx]._oneOverTextureRes = 1f / cascadeParamsSrc[lodIdx]._textureRes;

                cascadeParamsTgt[lodIdx]._texelWidth = _renderData[lodIdx].Current._texelWidth;
                cascadeParamsSrc[lodIdx]._texelWidth = _renderData[lodIdx].Previous(1)._texelWidth;

                cascadeParamsTgt[lodIdx]._weight = cascadeParamsSrc[lodIdx]._weight = 1f;

                cascadeParamsTgt[lodIdx]._maxWavelength = _renderData[lodIdx].Current._maxWavelength;
                // TODO?
                // cascadeParamsSrc[lodIdx]._maxWavelength = _renderDataSource[lodIdx].Current._maxWavelength;
            }

            // Duplicate last element so that things can safely read off the end of the cascades
            cascadeParamsTgt[OceanRenderer.Instance.CurrentLodCount] = cascadeParamsTgt[OceanRenderer.Instance.CurrentLodCount - 1];
            cascadeParamsSrc[OceanRenderer.Instance.CurrentLodCount] = cascadeParamsSrc[OceanRenderer.Instance.CurrentLodCount - 1];
            cascadeParamsTgt[OceanRenderer.Instance.CurrentLodCount]._weight = cascadeParamsSrc[OceanRenderer.Instance.CurrentLodCount]._weight = 0f;
        }
    }
}
