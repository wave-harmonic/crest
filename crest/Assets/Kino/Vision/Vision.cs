// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

using UnityEngine;

namespace Kino
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Kino Image Effects/Vision")]
    public sealed class Vision : MonoBehaviour
    {
        #region Common properties

        public enum Source { Depth, Normals, MotionVectors }

        [SerializeField]
        Source _source;

        [SerializeField, Range(0, 1)]
        float _blendRatio = 0.5f;

        [SerializeField]
        bool _useDepthNormals;

        #endregion

        #region Properties for depth

        [SerializeField]
        float _depthRepeat = 1;

        #endregion

        #region Properties for normals

        [SerializeField]
        bool _validateNormals = false;

        #endregion

        #region Properties for motion vectors

        [SerializeField]
        float _motionOverlayAmplitude = 10;

        [SerializeField, Range(0, 10)]
        float _motionVectorsAmplitude = 1;

        [SerializeField, Range(8, 64)]
        int _motionVectorsResolution = 16;

        #endregion

        #region Private members

        [SerializeField] Shader _shader;
        Material _material;

        // Check if the G-buffer is available.
        bool IsGBufferAvailable {
            get {
                var actualPath = GetComponent<Camera>().actualRenderingPath;
                return actualPath == RenderingPath.DeferredShading;
            }
        }

        #endregion

        #region MonoBehaviour functions

        void OnDestroy()
        {
            if (_material != null)
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
        }

        void Update()
        {
            var camera = GetComponent<Camera>();

            // Update depth texture mode.
            if (_source == Source.Depth)
                if (_useDepthNormals)
                    camera.depthTextureMode |= DepthTextureMode.DepthNormals;
                else
                    camera.depthTextureMode |= DepthTextureMode.Depth;

            if (_source == Source.Normals)
                if (_useDepthNormals || !IsGBufferAvailable)
                    camera.depthTextureMode |= DepthTextureMode.DepthNormals;

            if (_source == Source.MotionVectors)
                camera.depthTextureMode |=
                    DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Lazy initialization of the material.
            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }

            if (_source == Source.Depth)
            {
                // Depth
                _material.SetFloat("_Blend", _blendRatio);
                _material.SetFloat("_Repeat", _depthRepeat);
                var pass = _useDepthNormals ? 1 : 0;
                Graphics.Blit(source, destination, _material, pass);
            }
            else if (_source == Source.Normals)
            {
                // Normals
                _material.SetFloat("_Blend", _blendRatio);
                _material.SetFloat("_Validate", _validateNormals ? 1 : 0);
                var pass = (!_useDepthNormals && IsGBufferAvailable) ? 3 : 2;
                Graphics.Blit(source, destination, _material, pass);
            }
            else if (_source == Source.MotionVectors)
            {
                // Motion vectors (overlay)
                _material.SetFloat("_Blend", _blendRatio);
                _material.SetFloat("_Amplitude", _motionOverlayAmplitude);
                Graphics.Blit(source, destination, _material, 4);

                // Motion vectors (arrays)
                var rows = _motionVectorsResolution;
                var cols = rows * source.width / source.height;
                _material.SetInt("_ColumnCount", cols);
                _material.SetInt("_RowCount", rows);
                _material.SetFloat("_Amplitude", _motionVectorsAmplitude);
                _material.SetPass(5);
                Graphics.DrawProceduralNow(MeshTopology.Lines, cols * rows * 6, 1);
            }
        }

        #endregion
    }
}
