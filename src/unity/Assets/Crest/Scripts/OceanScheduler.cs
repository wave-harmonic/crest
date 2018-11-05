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
        void ApplySchedule(OceanRenderer ocean);
    }

    /// <summary>
    /// Default scheduler, added to Ocean GO if no other scheduler exists. New schedulers can be created by inheriting from this
    /// base class.
    /// </summary>
    public class OceanScheduler : MonoBehaviour, IOceanScheduler
    {
        public bool _warnIfMainCameraDepthLessThan0 = true;

        public virtual void ApplySchedule(OceanRenderer ocean)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Flow camera renders first
            //for (int i = 0; i < ocean.CurrentLodCount && ocean._lodDataFlow[i] != null; i++)
            //{
            //    ocean._lodDataFlow[i].Cam.depth = -50 - i;
            //}

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Dynamic waves camera renders second
            //for (int i = 0; i < ocean.CurrentLodCount && ocean._lodDataDynWaves[i] != null; i++)
            //{
            //    ocean._lodDataDynWaves[i].Cam.depth = -40 - i;
            //}


            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //// --- Animated waves next
            //for (int i = 0; i < ocean.CurrentLodCount; i++)
            //{
            //    ocean._lodDataAnimWaves[i].Cam.depth = -30 - i;

            //    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //    // --- Copy dynamic waves into animated waves (convert to displacements in the process)
            //    //if (ocean._lodDataDynWaves[i] != null)
            //    //{
            //    //    ocean._lodDataDynWaves[i].HookCombinePass(ocean._lodDataAnimWaves[i].Cam, CameraEvent.AfterForwardAlpha);
            //    //}
            //}


            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //// --- Do combine passes to carry long wavelengths/low detail up the chain into the high detail lods
            //ocean._lodDataAnimWaves[0].HookCombinePass(ocean._lodDataAnimWaves[0].Cam, CameraEvent.AfterEverything);


            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --- Foam takes the final combined waves as input and generates foam
            //for (int i = 0; i < ocean.CurrentLodCount && ocean._lodDataFoam[i] != null; i++)
            //{
            //    ocean._lodDataFoam[i].Cam.depth = -20 - i;
            //}


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
