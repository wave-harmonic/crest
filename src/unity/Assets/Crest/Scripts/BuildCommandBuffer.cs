using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class BuildCommandBuffer : MonoBehaviour
    {
        CommandBuffer _buf;

        void Build(OceanRenderer ocean, CommandBuffer buf)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Flow camera renders first
            for (int i = ocean.CurrentLodCount - 1; i >= 0 && ocean._lodDataFlow[i] != null; i--)
            {
                ocean._lodDataFlow[i].BuildCommandBuffer(ocean, buf);
            }
        }

        private void LateUpdate()
        {
            if (_buf == null)
            {
                _buf = new CommandBuffer();
                _buf.name = "CrestLodData";
                var cam = OceanRenderer.Instance.Viewpoint.GetComponent<Camera>();
                cam.AddCommandBuffer(CameraEvent.BeforeDepthTexture, _buf);
            }

            _buf.Clear();
            Build(OceanRenderer.Instance, _buf);
        }
    }
}
