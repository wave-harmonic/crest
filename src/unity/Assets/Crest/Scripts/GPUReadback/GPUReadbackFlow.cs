using UnityEngine;

namespace Crest
{
    public class GPUReadbackFlow : GPUReadbackBase<LodDataFlow>
    {
        PerLodData _areaData;

        public static GPUReadbackFlow Instance { get; private set; }

        protected override void Start()
        {
            base.Start();

            if (enabled == false)
            {
                return;
            }

            Debug.Assert(Instance == null);
            Instance = this;

            _minGridSize = 0.5f * _lodComponents[0].Settings._minObjectWidth / OceanRenderer.Instance._minTexelsPerWave;
            _maxGridSize = 0.5f * _lodComponents[0].Settings._maxObjectWidth / OceanRenderer.Instance._minTexelsPerWave;
            _maxGridSize = Mathf.Max(_maxGridSize, 2f * _minGridSize);
        }

        public bool SampleFlow(ref Vector3 in__worldPos, out Vector2 flow, float minSpatialLength)
        {
            var data = GetData(new Rect(in__worldPos.x, in__worldPos.z, 0f, 0f), minSpatialLength);
            if (data == null)
            {
                flow = Vector2.zero;
                return false;
            }
            return data._resultData.SampleRG16(ref in__worldPos, out flow);
        }
    }
}
