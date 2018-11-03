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
                LodDataFlow.AddDraw(rend);
            }
        }

        private void OnDisable()
        {
            var rend = GetComponent<Renderer>();
            if (rend)
            {
                LodDataFlow.AddDraw(rend);
            }
        }
    }
}
