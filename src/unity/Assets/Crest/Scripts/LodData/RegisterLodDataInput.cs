using UnityEngine;

namespace Crest
{
    public class RegisterLodDataInput<LodDataType> : MonoBehaviour
        where LodDataType : LodDataMgr
    {
        private void OnEnable()
        {
            var rend = GetComponent<Renderer>();
            var ocean = OceanRenderer.Instance;
            if (rend && ocean)
            {
                var ld = ocean.GetComponent<LodDataType>();
                if (ld)
                {
                    ld.AddDraw(rend);
                }
            }
        }

        private void OnDisable()
        {
            var rend = GetComponent<Renderer>();
            var ocean = OceanRenderer.Instance;
            if (rend && ocean)
            {
                var ld = ocean.GetComponent<LodDataType>();
                if (ld)
                {
                    ld.RemoveDraw(rend);
                }
            }
        }
    }
}
