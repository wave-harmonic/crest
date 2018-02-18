// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Support script for gerstner wave ocean shapes.
    /// Generates a number of gerstner octaves in child gameobjects.
    /// </summary>
    public class ShapeGerstner : ShapeGerstnerBase
    {
        Material[] _materials;

        void InitMaterials()
        {
            foreach( var child in transform )
            {
                Destroy((child as Transform).gameObject);
            }

            _materials = new Material[_wavelengths.Length];
            _amplitudes = new float[_wavelengths.Length];

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                GameObject GO = new GameObject(string.Format("Wavelength {0}", _wavelengths[i].ToString("0.000")));
                GO.layer = gameObject.layer;

                MeshFilter meshFilter = GO.AddComponent<MeshFilter>();
                meshFilter.mesh = _rasterMesh;

                GO.transform.parent = transform;
                GO.transform.localPosition = Vector3.zero;
                GO.transform.localRotation = Quaternion.identity;
                GO.transform.localScale = Vector3.one;

                _materials[i] = new Material(_waveShader);

                MeshRenderer renderer = GO.AddComponent<MeshRenderer>();
                renderer.material = _materials[i];
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_materials == null || _materials.Length != _wavelengths.Length)
            {
                InitMaterials();
            }

            UpdateMaterials();
        }

        private void LateUpdate()
        {
            LateUpdateSetLODAssignments();
        }

        public void LateUpdateSetLODAssignments()
        {
            // this could be run only when ocean scale changes. i'm leaving it on every frame in this research code because
            // that way its completely dynamic and will respond to LOD count changes, etc.

            int editorOnlyLayerMask = LayerMask.NameToLayer("EditorOnly");

            int lodIdx = 0;
            int lodCount = OceanRenderer.Instance._lodCount;
            float minWl = OceanRenderer.Instance.MaxWavelength(0) / 2f;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (_wavelengths[i] < minWl || _amplitudes[i] < 0.001f)
                {
                    transform.GetChild(i).gameObject.layer = editorOnlyLayerMask;
                    continue;
                }

                while (_wavelengths[i] >= 2f * minWl && lodIdx < lodCount)
                {
                    lodIdx++;
                    minWl *= 2f;
                }

                int layer = lodIdx < lodCount ? LayerMask.NameToLayer("WaveData" + lodIdx.ToString()) : LayerMask.NameToLayer("WaveDataBigWavelengths");
                transform.GetChild(i).gameObject.layer = layer;
            }
        }

        void UpdateMaterials()
        {
            for (int i = 0; i < _wavelengths.Length; i++)
            {
                // Wavelength
                _materials[i].SetFloat("_Wavelength", _wavelengths[i]);

                // Amplitude
                _materials[i].SetFloat("_Amplitude", _amplitudes[i]);

                // Direction
                _materials[i].SetFloat("_Angle", Mathf.Deg2Rad * (OceanRenderer.Instance._windDirectionAngle + _angleDegs[i]));

                // Phase
                _materials[i].SetFloat("_Phase", _phases[i]);
            }
        }
    }
}
