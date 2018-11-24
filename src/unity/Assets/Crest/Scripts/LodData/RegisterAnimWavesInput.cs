// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)
using UnityEngine;

namespace Crest
{
    public class RegisterAnimWavesInput : RegisterLodDataInput<LodDataMgrAnimWaves>
    {
        [SerializeField, Tooltip("Which octave to render into, for example set this to 2 to use render into the 2m-4m octave. These refer to the same octaves as the wave spectrum editor. Set this value to 0 to render into all LODs.")]
        float _octaveWavelength = 0f;
        public float OctaveWavelength
        {
            get
            {
                return _octaveWavelength;
            }
        }
    }
}
