using UnityEngine;

namespace Crest
{
    public abstract class RegisterLodDataInputBase : MonoBehaviour
    {
        [SerializeField, Tooltip("Which octave to render into, set this to 2 to use render into the 2m-4m octave. These refer to the same octaves as the wave spectrum. Set this value to 0 to render into all LODs.")]
        float _octaveWavelength = 0f;
        public float OctaveWavelength
        {
            get
            {
                return _octaveWavelength;
            }
        }

        Renderer _renderer;
        public Renderer RendererComponent
        {
            get
            {
                return _renderer != null ? _renderer : (_renderer = GetComponent<Renderer>());
            }
        }
    }

    public class RegisterLodDataInput<LodDataType> : RegisterLodDataInputBase
        where LodDataType : LodDataMgr
    {
        [SerializeField] bool _disableRenderer = true;

        protected virtual void OnEnable()
        {
            var rend = GetComponent<Renderer>();
            var ocean = OceanRenderer.Instance;
            if (rend && ocean)
            {
                if (_disableRenderer)
                {
                    rend.enabled = false;
                }

                var ld = ocean.GetComponent<LodDataType>();
                if (ld)
                {
                    ld.AddDraw(this);
                }
            }
        }

        protected virtual void OnDisable()
        {
            var rend = GetComponent<Renderer>();
            var ocean = OceanRenderer.Instance;
            if (rend && ocean)
            {
                var ld = ocean.GetComponent<LodDataType>();
                if (ld)
                {
                    ld.RemoveDraw(this);
                }
            }
        }
    }
}
