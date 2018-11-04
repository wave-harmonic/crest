// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
            if (ocean._lodDataFlow)
            {
                ocean._lodDataFlow.BuildCommandBuffer(ocean, buf);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Dynamic waves camera renders second
            if (ocean._lodDataDynWaves)
            {
                ocean._lodDataDynWaves.BuildCommandBuffer(ocean, buf);
            }

            // all the other things..

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Foam takes the final combined waves as input and generates foam
            if (ocean._lodDataFoam)
            {
                ocean._lodDataFoam.BuildCommandBuffer(ocean, buf);
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
