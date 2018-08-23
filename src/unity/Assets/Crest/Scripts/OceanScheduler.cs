// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Interface for configuring render order of ocean systems and creating the necessary hooks
    /// </summary>
    public interface IOceanScheduler
    {
        void ApplySchedule(OceanBuilder ocean);
    }

    /// <summary>
    /// Default scheduler, added to Ocean GO if no other scheduler exists. New schedulers can be created by inheriting from this
    /// base class.
    /// </summary>
    public class OceanScheduler : MonoBehaviour, IOceanScheduler
    {
        public bool _warnIfMainCameraDepthLessThan0 = true;

        public virtual void ApplySchedule(OceanBuilder ocean)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Dynamic waves camera renders first
            bool dynamicWavesPresent = ocean._simLodDatas.ContainsKey("DynamicWaves") && ocean._simLodDatas["DynamicWaves"] != null;
            for (int i = 0; dynamicWavesPresent && i < ocean.CurrentLodCount; i++)
            {
                ocean._simLodDatas["DynamicWaves"][i].Cam.depth = -40 - i;
            }


            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Animated waves next
            for (int i = 0; i < ocean.CurrentLodCount; i++)
            {
                ocean._camsAnimWaves[i].depth = -30 - i;

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // --- Copy dynamic waves into animated waves (convert to displacements in the process)
                if (dynamicWavesPresent)
                {
                    (ocean._simLodDatas["DynamicWaves"][i] as LodDataDynamicWaves).HookCombinePass(ocean._camsAnimWaves[i], CameraEvent.AfterForwardAlpha);
                }
            }


            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Do combine passes to carry long wavelengths/low detail up the chain into the high detail lods
            ocean._lodDataAnimWaves[0].HookCombinePass(ocean._camsAnimWaves[0], CameraEvent.AfterEverything);


            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Foam takes the final combined waves as input and generates foam
            bool foamPresent = ocean._simLodDatas.ContainsKey("Foam") && ocean._simLodDatas["Foam"] != null;
            for (int i = 0; foamPresent && i < ocean.CurrentLodCount; i++)
            {
                ocean._simLodDatas["Foam"][i].Cam.depth = -20 - i;
            }


            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Data driven sims
            foreach (var simList in ocean._simLodDatas)
            {
                for(int i = 0; i < simList.Value.Count; i++)
                {
                    var lds = simList.Value[i] as LodDataSim;
                    if(lds != null)
                        simList.Value[i].Cam.depth = lds._sim._cameraDepthOrder;
                }
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- User cameras render the ocean surface at depth ~0 - above data should be ready to go!


            // warn if main camera scheduled early
            CheckMainCameraDepth();
        }

        void CheckMainCameraDepth()
        {
            if (_warnIfMainCameraDepthLessThan0)
            {
                var mainCam = Camera.main;
                var warningMin = -10;
                if (mainCam != null && mainCam.depth <= warningMin)
                {
                    Debug.LogWarning("Main camera is scheduled at depth " + mainCam.depth + " which is close to the ocean data cameras, if possible set the main camera depth to be greater than " + warningMin + " or adjust the depths by creating a new IOceanScheduler.", this);
                }
            }
        }
    }
}
