// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace OceanResearch
{
    /// <summary>
    /// Positions wave data render camera. Snaps to shape texels to avoid aliasing.
    /// </summary>
    [RequireComponent( typeof( Camera ) )]
    public class WaveDataCam : MonoBehaviour
    {
        [HideInInspector]
        public int _lodIndex = 0;
        [HideInInspector]
        public int _lodCount = 5;

        int _shapeRes = -1;

        public struct RenderData
        {
            public float _texelWidth;
            public float _textureRes;
            public Vector3 _posContinuous;
            public Vector3 _posSnapped;
            public Vector3 _posSnappedLast;
        }
        public RenderData _renderData = new RenderData();

        void Start()
        {
            cam.depthTextureMode = DepthTextureMode.None;
        }

        private void Update()
        {
            _renderData._posSnappedLast = _renderData._posSnapped;
        }

        // script execution order ensures this runs after CircleOffset
        void LateUpdate()
        {
            // ensure camera size matches geometry size
            cam.orthographicSize = 2f * Mathf.Abs( transform.lossyScale.x );
            bool flip = transform.lossyScale.z < 0f;
            transform.localEulerAngles = new Vector3( flip ? -90f : 90f, 0f, 0f );

            // find snap period
            int width = cam.targetTexture.width;
            // debug functionality to resize RT if different size was specified.
            if( _shapeRes == -1 )
            {
                _shapeRes = width;
            }
            else if( width != _shapeRes )
            {
                cam.targetTexture.Release();
                cam.targetTexture.width = cam.targetTexture.height = _shapeRes;
                cam.targetTexture.Create();
            }
            _renderData._textureRes = (float)cam.targetTexture.width;
            _renderData._texelWidth = 2f * cam.orthographicSize / _renderData._textureRes;
            // snap so that shape texels are stationary
            _renderData._posContinuous = transform.position;
            _renderData._posSnapped = _renderData._posContinuous
                - new Vector3( Mathf.Repeat( _renderData._posContinuous.x, _renderData._texelWidth ), 0f, Mathf.Repeat( _renderData._posContinuous.z, _renderData._texelWidth ) );

            // set projection matrix to snap to texels
            cam.ResetProjectionMatrix();
            Matrix4x4 P = cam.projectionMatrix, T = new Matrix4x4();
            T.SetTRS( new Vector3( _renderData._posContinuous.x - _renderData._posSnapped.x, _renderData._posContinuous.z - _renderData._posSnapped.z ), Quaternion.identity, Vector3.one );
            P = P * T;
            cam.projectionMatrix = P;
        }

        public void ApplyMaterialParams( int shapeSlot, Material mat )
        {
            mat.SetTexture( "_WD_Sampler_" + shapeSlot.ToString(), cam.targetTexture );
            float shapeWeight = (_lodIndex == _lodCount - 1) ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            mat.SetVector( "_WD_Params_" + shapeSlot.ToString(), new Vector3( _renderData._texelWidth, _renderData._textureRes, shapeWeight ) );
            mat.SetVector( "_WD_Pos_" + shapeSlot.ToString(), new Vector2( _renderData._posSnapped.x, _renderData._posSnapped.z ) );
            mat.SetVector( "_WD_Pos_Cont_" + shapeSlot.ToString(), new Vector2( _renderData._posContinuous.x, _renderData._posContinuous.z ) );
            mat.SetInt( "_WD_LodIdx_" + shapeSlot.ToString(), _lodIndex );
        }

        private void OnPostRender()
        {
            // accumulate sim lod data when simulations stop rendering.

            // in ocean builder, we set camera depths to ensure the LOD0 camera renders last. so if this is the camera for LOD0, we know its safe now
            // to start combining sim results.
            if( OceanRenderer.Instance.Builder.GetShapeCamIndex( cam ) == 0 )
            {
                ShapeWaveSim.OnShapeCamerasFinishedRendering();
            }
        }

        Camera _camera; Camera cam { get { return _camera != null ? _camera : (_camera = GetComponent<Camera>()); } }
    }
}
