// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public abstract class BuildCommandBufferBase : MonoBehaviour
    {
        public static int _lastUpdateFrame = -1;
    }

    public class BuildCommandBuffer : BuildCommandBufferBase
    {
        CommandBuffer _buf;

        void Build(OceanRenderer ocean, CommandBuffer buf)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Ocean depths
            if (ocean._lodDataSeaDepths)
            {
                ocean._lodDataSeaDepths.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Flow data
            if (ocean._lodDataFlow)
            {
                ocean._lodDataFlow.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Dynamic wave simulations
            if (ocean._lodDataDynWaves)
            {
                ocean._lodDataDynWaves.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Animated waves next
            if (ocean._lodDataAnimWaves)
            {
                ocean._lodDataAnimWaves.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Foam simulation
            if (ocean._lodDataFoam)
            {
                ocean._lodDataFoam.BuildCommandBuffer(ocean, buf);
            }
        }

        /// <summary>
        /// Construct the command buffer and attach it to the camera so that it will be executed in the render.
        /// </summary>
        public void LateUpdate()
        {
            if (OceanRenderer.Instance == null) return;

            if (_buf == null)
            {
                _buf = new CommandBuffer();
                _buf.name = "CrestLodData";
            }

            _buf.Clear();

            Build(OceanRenderer.Instance, _buf);

            Graphics.ExecuteCommandBuffer(_buf);

            _lastUpdateFrame = Time.frameCount;
        }

        CameraEvent GetHookEvent(Camera cam)
        {
            // unfortunately deferred and forward paths do not have a shared 'start' event we can hook on to, so decide
            // base on path. https://docs.unity3d.com/Manual/GraphicsCommandBuffers.html

            switch (cam.actualRenderingPath)
            {
                case RenderingPath.DeferredShading:
                    return CameraEvent.BeforeGBuffer;

                case RenderingPath.Forward:
                case RenderingPath.DeferredLighting:
                    return CameraEvent.BeforeDepthTexture;

                case RenderingPath.VertexLit:
                    // This didn't work for me when i tested it - no ocean surface was visible at all. Since this is a legacy path I'm
                    // not going to investigate further.
                    Debug.LogWarning("Vertex Lit is not tested with Crest and may not work.", this);
                    return CameraEvent.BeforeDepthTexture;

                default:
                    Debug.LogWarning("Not sure where to hook ocean rendering command buffer - rendering path is: " + cam.actualRenderingPath
                        + ". Assuming forward rendering is enabled.", this);
                    return CameraEvent.BeforeDepthTexture;
            }
        }
    }
}
