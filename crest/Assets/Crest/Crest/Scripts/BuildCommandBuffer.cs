// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Base class for the command buffer builder, which takes care of updating all ocean-related data. If you wish to provide your
    /// own update logic, you can create a new component that inherits from this class and attach it to the same GameObject as the
    /// OceanRenderer script. The new component should be set to update after the Default bucket, similar to BuildCommandBuffer.
    /// </summary>
    public abstract class BuildCommandBufferBase : MonoBehaviour
    {
        /// <summary>
        /// Used to validate update order
        /// </summary>
        public static int _lastUpdateFrame = -1;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            _lastUpdateFrame = -1;
        }
    }

    /// <summary>
    /// The default builder for the ocean update command buffer which takes care of updating all ocean-related data, for
    /// example rendering animated waves and advancing sims. This runs in LateUpdate after the Default bucket, after the ocean
    /// system been moved to an up to date position and frame processing is done.
    /// </summary>
    public class BuildCommandBuffer : BuildCommandBufferBase
    {
        CommandBuffer _buf;

        void Build(OceanRenderer ocean, CommandBuffer buf)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Ocean depths
            if (ocean._lodDataSeaDepths && ocean._lodDataSeaDepths.enabled)
            {
                ocean._lodDataSeaDepths.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Flow data
            if (ocean._lodDataFlow && ocean._lodDataFlow.enabled)
            {
                ocean._lodDataFlow.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Dynamic wave simulations
            if (ocean._lodDataDynWaves && ocean._lodDataDynWaves.enabled)
            {
                ocean._lodDataDynWaves.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Animated waves next
            if (ocean._lodDataAnimWaves && ocean._lodDataAnimWaves.enabled)
            {
                ocean._lodDataAnimWaves.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Foam simulation
            if (ocean._lodDataFoam && ocean._lodDataFoam.enabled)
            {
                ocean._lodDataFoam.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Clip surface
            if (ocean._lodDataClipSurface && ocean._lodDataClipSurface.enabled)
            {
                ocean._lodDataClipSurface.BuildCommandBuffer(ocean, buf);
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

            // This will execute at the beginning of the frame before the graphics queue
            Graphics.ExecuteCommandBuffer(_buf);

            _lastUpdateFrame = Time.frameCount;
        }
    }
}
