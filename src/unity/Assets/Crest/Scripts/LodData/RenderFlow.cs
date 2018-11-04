using UnityEngine;

namespace Crest
{
    public class RenderFlow : MonoBehaviour
    {
        private void OnEnable()
        {
            var rend = GetComponent<Renderer>();
            if (rend)
            {
                LodDataMgrFlow.AddDraw(rend);
            }
        }

        private void OnDisable()
        {
            var rend = GetComponent<Renderer>();
            if (rend)
            {
                LodDataMgrFlow.RemoveDraw(rend);
            }
        }
    }
}
