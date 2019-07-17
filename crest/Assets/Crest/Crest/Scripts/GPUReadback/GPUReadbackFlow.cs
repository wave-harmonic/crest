// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Reads back flow data - horizontal velocity of water - so that it is available for physics.
    /// </summary>
    public class GPUReadbackFlow : GPUReadbackBase<LodDataMgrFlow>
    {
        public static GPUReadbackFlow Instance { get; private set; }

        protected override void Start()
        {
            base.Start();

            Instance = this;

            if (enabled == false)
            {
                return;
            }

            _minGridSize = 0.5f * _lodComponent.Settings._minObjectWidth / OceanRenderer.Instance.MinTexelsPerWave;
            _maxGridSize = 0.5f * _lodComponent.Settings._maxObjectWidth / OceanRenderer.Instance.MinTexelsPerWave;
            _maxGridSize = Mathf.Max(_maxGridSize, 2f * _minGridSize);
        }

        private void OnDestroy()
        {
            Instance = null;
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

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReLoadScripts()
        {
            Instance = FindObjectOfType<GPUReadbackFlow>();
        }
#endif
    }
}
