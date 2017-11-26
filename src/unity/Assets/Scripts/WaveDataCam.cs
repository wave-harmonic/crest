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
        }
        public RenderData _renderData = new RenderData();

        Material _matCombineSims;

        public Vector3 _lastPosition;

        void Start()
        {
            camera.depthTextureMode = DepthTextureMode.None;
            _matCombineSims = new Material( Shader.Find( "Ocean/Shape/Sim/Combine" ) );
        }

        private void Update()
        {
            _lastPosition = _renderData._posSnapped;
        }

        // script execution order ensures this runs after CircleOffset
        void LateUpdate()
        {
            // ensure camera size matches geometry size
            camera.orthographicSize = 2f * Mathf.Abs( transform.lossyScale.x );
            bool flip = transform.lossyScale.z < 0f;
            transform.localEulerAngles = new Vector3( flip ? -90f : 90f, 0f, 0f );

            // find snap period
            int width = camera.targetTexture.width;
            // debug functionality to resize RT if different size was specified.
            if( _shapeRes == -1 )
            {
                _shapeRes = width;
            }
            else if( width != _shapeRes )
            {
                camera.targetTexture.Release();
                camera.targetTexture.width = camera.targetTexture.height = _shapeRes;
                camera.targetTexture.Create();
            }
            _renderData._textureRes = (float)camera.targetTexture.width;
            _renderData._texelWidth = 2f * camera.orthographicSize / _renderData._textureRes;
            // snap so that shape texels are stationary
            _renderData._posContinuous = transform.position;
            _renderData._posSnapped = _renderData._posContinuous
                - new Vector3( Mathf.Repeat( _renderData._posContinuous.x, _renderData._texelWidth ), 0f, Mathf.Repeat( _renderData._posContinuous.z, _renderData._texelWidth ) );

            // set projection matrix to snap to texels
            camera.ResetProjectionMatrix();
            Matrix4x4 P = camera.projectionMatrix, T = new Matrix4x4();
            T.SetTRS( new Vector3( _renderData._posContinuous.x - _renderData._posSnapped.x, _renderData._posContinuous.z - _renderData._posSnapped.z ), Quaternion.identity, Vector3.one );
            P = P * T;
            camera.projectionMatrix = P;
        }

        public void ApplyMaterialParams( int shapeSlot, Material mat )
        {
            mat.SetTexture( "_WD_Sampler_" + shapeSlot.ToString(), camera.targetTexture );
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
            if( OceanRenderer.Instance.Builder.GetShapeCamIndex( camera ) == 0 )
            {
                var cams = OceanRenderer.Instance.Builder._shapeCameras;
                for( int L = cams.Length-2; L >= 0; L-- )
                {
                    // save the projection params to enable combining results across multiple shape textures
                    cams[L].GetComponent<WaveDataCam>().ApplyMaterialParams( 0, _matCombineSims );
                    cams[L + 1].GetComponent<WaveDataCam>().ApplyMaterialParams( 1, _matCombineSims );

                    // accumulate simulation results down the lod chain - combine L+1 into L
                    Graphics.Blit( cams[L + 1].GetComponent<PingPongRts>()._targetThisFrame, cams[L].GetComponent<PingPongRts>()._targetThisFrame, _matCombineSims );
                }
            }
        }

        Camera _camera; new Camera camera { get { return _camera != null ? _camera : (_camera = GetComponent<Camera>()); } }
    }
}
