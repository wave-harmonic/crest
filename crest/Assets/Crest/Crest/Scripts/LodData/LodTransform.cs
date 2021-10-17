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
        // Anything higher (minus 1 for near plane) will be clipped.
        const float k_RenderAboveSeaLevel = 10000f;
        // Anything lower will be clipped.
        const float k_RenderBelowSeaLevel = 10000f;

        protected int[] _transformUpdateFrame;

        // ocean scale last frame - used to detect scale changes
        float _oceanLocalScalePrev = -1f;
        int _scaleDifferencePow2 = 0;
        public int ScaleDifferencePow2 => _scaleDifferencePow2;

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
                    Debug.LogWarning($"Crest: RenderData validation failed - {context} - _frame of data ({_frame}) != expected ({OceanRenderer.FrameCount + frameOffset}), which may indicate some update functions are being called out of order, or script execution order is broken.", OceanRenderer.Instance);
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
        public Matrix4x4 GetWorldToCameraMatrix(int lodIdx) => _worldToCameraMatrix[lodIdx];
        public Matrix4x4 GetProjectionMatrix(int lodIdx) => _projectionMatrix[lodIdx];

        public void InitLODData(int lodCount, int bufferSize)
        {
            LodCount = lodCount;

            _renderData = new BufferedData<RenderData>[lodCount];
            for (var i = 0; i < lodCount; i++)
            {
                _renderData[i] = new BufferedData<RenderData>(bufferSize, () => new RenderData());
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

                var isFirstUpdate = _transformUpdateFrame[lodIdx] == -1;

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

                // Detect first update and populate the render data if so - otherwise it can give divide by 0s and other nastiness.
                if (isFirstUpdate && _renderData[lodIdx].Size > 1)
                {
                    // We are writing to "Current" again. But it is okay since only once.
                    _renderData[lodIdx].RunLambda(buffer =>
                    {
                        buffer._posSnapped = _renderData[lodIdx].Current._posSnapped;
                        buffer._texelWidth = _renderData[lodIdx].Current._texelWidth;
                        buffer._textureRes = _renderData[lodIdx].Current._textureRes;
                        buffer._maxWavelength = _renderData[lodIdx].Current._maxWavelength;
                    });
                }

                _worldToCameraMatrix[lodIdx] = CalculateWorldToCameraMatrixRHS(_renderData[lodIdx].Current._posSnapped + Vector3.up * k_RenderAboveSeaLevel, Quaternion.AngleAxis(90f, Vector3.right));

                _projectionMatrix[lodIdx] = Matrix4x4.Ortho(-2f * lodScale, 2f * lodScale, -2f * lodScale, 2f * lodScale, 1f, k_RenderAboveSeaLevel + k_RenderBelowSeaLevel);
            }

            UpdateScaleDifference();
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
            float texelsPerWave = 2f;
            return 2f * maxTexelSize * texelsPerWave;
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            for (var lodIdx = 0; lodIdx < LodCount; lodIdx++)
            {
                _renderData[lodIdx].RunLambda(renderData => renderData._posSnapped -= newOrigin);
            }
        }

        void UpdateScaleDifference()
        {
            // Determine if LOD transform has changed scale and by how much (in exponent of 2).
            float oceanLocalScale = OceanRenderer.Instance.Root.localScale.x;
            if (_oceanLocalScalePrev == -1f) _oceanLocalScalePrev = oceanLocalScale;
            float ratio = oceanLocalScale / _oceanLocalScalePrev;
            _oceanLocalScalePrev = oceanLocalScale;
            float ratio_l2 = Mathf.Log(ratio) / Mathf.Log(2f);
            _scaleDifferencePow2 = Mathf.RoundToInt(ratio_l2);
        }

        public void WriteCascadeParams(BufferedData<OceanRenderer.CascadeParams[]> cascadeParams)
        {
            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                cascadeParams.Current[lodIdx]._posSnapped[0] = _renderData[lodIdx].Current._posSnapped[0];
                cascadeParams.Current[lodIdx]._posSnapped[1] = _renderData[lodIdx].Current._posSnapped[2];
                // NOTE: Current scale was assigned to current and previous frame, but not sure why. 2021.10.17
                cascadeParams.Current[lodIdx]._scale = OceanRenderer.Instance.CalcLodScale(lodIdx);
                cascadeParams.Current[lodIdx]._textureRes = _renderData[lodIdx].Current._textureRes;
                cascadeParams.Current[lodIdx]._oneOverTextureRes = 1f / cascadeParams.Current[lodIdx]._textureRes;
                cascadeParams.Current[lodIdx]._texelWidth = _renderData[lodIdx].Current._texelWidth;
                cascadeParams.Current[lodIdx]._weight = 1f;
                cascadeParams.Current[lodIdx]._maxWavelength = _renderData[lodIdx].Current._maxWavelength;
            }

            // Duplicate last element so that things can safely read off the end of the cascades
            cascadeParams.Current[OceanRenderer.Instance.CurrentLodCount] = cascadeParams.Current[OceanRenderer.Instance.CurrentLodCount - 1];
            cascadeParams.Current[OceanRenderer.Instance.CurrentLodCount]._weight = 0f;
        }
    }
}
