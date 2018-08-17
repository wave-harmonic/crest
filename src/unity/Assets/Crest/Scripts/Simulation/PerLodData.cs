using UnityEngine;

public class PerLodData : MonoBehaviour
{
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

    Camera _camera; protected Camera Cam { get { return _camera ?? (_camera = GetComponent<Camera>()); } }
}
