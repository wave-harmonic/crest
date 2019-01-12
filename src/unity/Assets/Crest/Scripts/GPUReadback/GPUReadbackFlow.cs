// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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

        private void OnDestroy()
        {
            _instance = null;
        }

        public bool SampleFlow(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector2 flow)
        {
            var data = i_samplingData._tag as PerLodData;
            if (data == null)
            {
                if (i_samplingData._tag != null)
                {
                    Debug.LogError("Wrong kind of SamplingData provided - sampling data for e.g. collision and flow are not interchangeable.", this);
                }

                flow = Vector2.zero;
                return false;
            }
            return data._resultData.SampleRG16(ref i_worldPos, out flow);
        }
    }
}
