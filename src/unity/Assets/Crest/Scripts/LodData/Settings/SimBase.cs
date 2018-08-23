using UnityEngine;

namespace Crest
{
    public abstract class SimBase : ScriptableObject
    {
        public bool _bindResultToOceanMaterial = true;
        public Shader _simulationShader;
        public RenderTextureFormat _dataFormat = RenderTextureFormat.RHalf;
        public int _cameraDepthOrder = -20;
        public LodData.SimType[] _inputs;
        public SimBase[] _inputSims;

        [Range(0f, 32f), Tooltip("The sim will not run if the simulation grid is smaller in resolution than this size. Useful to limit sim range for performance.")]
        public float _minGridSize = 0f;
        [Range(0f, 32f), Tooltip("The sim will not run if the simulation grid is bigger in resolution than this size. Zero means no constraint/unlimited resolutions. Useful to limit sim range for performance.")]
        public float _maxGridSize = 0f;

        public virtual void SetSimParams(Material simMaterial)
        {
        }
    }
}
