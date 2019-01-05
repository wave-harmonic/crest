// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public abstract class RegisterLodDataInputBase : MonoBehaviour
    {
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
