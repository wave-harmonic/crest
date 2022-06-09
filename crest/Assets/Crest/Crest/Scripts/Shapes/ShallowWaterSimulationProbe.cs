// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class ShallowWaterSimulationProbe : MonoBehaviour
    {
        [SerializeField] ShallowWaterSimulation _shallowWaterSimulation;

        Material _mat;

        private void Awake()
        {
            _mat = TryGetComponent<MeshRenderer>(out var mr) ? mr.sharedMaterial : null;
        }

        private void Update()
        {
            if (_shallowWaterSimulation == null)
            {
                return;
            }

            _mat.SetVector("_SimOrigin", _shallowWaterSimulation.SimOrigin());
            _mat.SetFloat("_DomainWidth", _shallowWaterSimulation.DomainWidth);
            _mat.SetTexture("_swsGroundHeight", _shallowWaterSimulation.RTGroundHeight);
        }
    }
}
