// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class LodTransform : MonoBehaviour, IFloatingOrigin
    {
        protected int _transformUpdateFrame = -1;

        static int[] _paramsPosScale = null;
        static int[] _paramsOcean = null;

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posSnapped;
            public int _frame;

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

        public RenderData _renderData = new RenderData();
        public RenderData _renderDataPrevFrame = new RenderData();

        int _lodIndex = -1;
        int _lodCount = -1;
        public void InitLODData(int lodIndex, int lodCount) { _lodIndex = lodIndex; _lodCount = lodCount; }
        public int LodIndex { get { return _lodIndex; } }
        public int LodCount { get { return _lodCount; } }

        Matrix4x4 _worldToCameraMatrix;
        Matrix4x4 _projectionMatrix;

        public void UpdateTransform()
        {
            if (_transformUpdateFrame == Time.frameCount)
                return;

            _transformUpdateFrame = Time.frameCount;

            _renderDataPrevFrame = _renderData;

            float camOrthSize = 2f * transform.lossyScale.x;

            // find snap period
            _renderData._textureRes = OceanRenderer.Instance.LodDataResolution;
            _renderData._texelWidth = 2f * camOrthSize / _renderData._textureRes;
            // snap so that shape texels are stationary
            _renderData._posSnapped = transform.position
                - new Vector3(Mathf.Repeat(transform.position.x, _renderData._texelWidth), 0f, Mathf.Repeat(transform.position.z, _renderData._texelWidth));

            _renderData._frame = Time.frameCount;

            // detect first update and populate the render data if so - otherwise it can give divide by 0s and other nastiness
            if (_renderDataPrevFrame._textureRes == 0f)
            {
                _renderDataPrevFrame._posSnapped = _renderData._posSnapped;
                _renderDataPrevFrame._texelWidth = _renderData._texelWidth;
                _renderDataPrevFrame._textureRes = _renderData._textureRes;
            }

            _worldToCameraMatrix = CalculateWorldToCameraMatrixRHS(_renderData._posSnapped + Vector3.up * 100f, Quaternion.AngleAxis(90f, Vector3.right));

            _projectionMatrix = Matrix4x4.Ortho(-2f * transform.lossyScale.x, 2f * transform.lossyScale.x, -2f * transform.lossyScale.z, 2f * transform.lossyScale.z, 1f, 500f);
        }

        // Borrowed from LWRP code: https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/2a68d8073c4eeef7af3be9e4811327a522434d5f/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs
        public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        }

        public void SetViewProjectionMatrices(CommandBuffer buf)
        {
            buf.SetViewProjectionMatrices(_worldToCameraMatrix, _projectionMatrix);
        }

        public float MaxWavelength()
        {
            float oceanBaseScale = OceanRenderer.Instance.transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, _lodIndex);
            float maxTexelSize = maxDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            return 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;
        }

        public static int ParamIdPosScale(int slot)
        {
            if (_paramsPosScale == null)
                CreateParamIDs(ref _paramsPosScale, "_LD_Pos_Scale_");
            return _paramsPosScale[slot];
        }

        public static int ParamIdOcean(int slot)
        {
            if (_paramsOcean == null)
                CreateParamIDs(ref _paramsOcean, "_LD_Params_");
            return _paramsOcean[slot];
        }

        public static void CreateParamIDs(ref int[] ids, string prefix)
        {
            int count = 2;
            ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = Shader.PropertyToID(string.Format("{0}{1}", prefix, i));
            }
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            _renderData._posSnapped -= newOrigin;
            _renderDataPrevFrame._posSnapped -= newOrigin;
        }
    }
}
