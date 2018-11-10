using UnityEngine;

namespace Crest
{
    public class GPUReadbackFlow : GPUReadbackBase<LodDataMgrFlow>
    {
        static GPUReadbackFlow _instance;
        public static GPUReadbackFlow Instance
        {
            get
            {
#if !UNITY_EDITOR
                return _instance;
#else
                // Allow hot code edit/recompile in editor - re-init singleton reference.
                return _instance != null ? _instance : (_instance = FindObjectOfType<GPUReadbackFlow>());
#endif
            }
        }

        protected override void Start()
        {
            base.Start();

            if (enabled == false)
            {
                return;
            }

            Debug.Assert(_instance == null);
            _instance = this;

            _minGridSize = 0.5f * _lodComponent.Settings._minObjectWidth / OceanRenderer.Instance._minTexelsPerWave;
            _maxGridSize = 0.5f * _lodComponent.Settings._maxObjectWidth / OceanRenderer.Instance._minTexelsPerWave;
            _maxGridSize = Mathf.Max(_maxGridSize, 2f * _minGridSize);
        }

        public bool SampleFlow(ref Vector3 i_worldPos, out Vector2 flow, float minSpatialLength)
        {
            var data = GetData(new Rect(i_worldPos.x, i_worldPos.z, 0f, 0f), minSpatialLength);
            if (data == null)
            {
                flow = Vector2.zero;
                return false;
            }
            return data._resultData.SampleRG16(ref i_worldPos, out flow);
        }
    }
}
