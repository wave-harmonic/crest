using UnityEngine;

namespace Crest
{
    public class ShapeDynamicSim : MonoBehaviour
    {
        public float _resolution = 0.5f;
        int _lodIndex = -1;

        Camera _cam;
        PingPongRts _pprts;

        public GameObject _copySimResultsToDisplacements;
        Material _copySimMaterial;

        public GameObject _renderSim;
        Material _renderSimMaterial;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _pprts = GetComponent<PingPongRts>();

            _copySimResultsToDisplacements.GetComponent<Renderer>().material = _copySimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Add To Disps"));
            _copySimMaterial.SetColor("_TintColor", Color.white);

            _renderSim.GetComponent<Renderer>().material = _renderSimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/2D Wave Equation"));
        }

        void LateUpdate()
        {
            _lodIndex = OceanRenderer.Instance.GetLodIndex(_resolution);
            if (_lodIndex == -1)
            {
                _copySimResultsToDisplacements.SetActive(false);
                return;
            }

            var lodCam = OceanRenderer.Instance.Builder._shapeCameras[_lodIndex];
            transform.position = lodCam.transform.position;

            _cam.orthographicSize = lodCam.orthographicSize;
            transform.localScale = (Vector3.right + Vector3.up) * _cam.orthographicSize * 2f + Vector3.forward;

            _renderSimMaterial.SetTexture("_WavePPTSource", _pprts._sourceThisFrame);

            _copySimMaterial.mainTexture = _pprts._targetThisFrame;
            _copySimResultsToDisplacements.SetActive(true);
            _copySimResultsToDisplacements.layer = LayerMask.NameToLayer("WaveData" + _lodIndex);
        }
    }
}
