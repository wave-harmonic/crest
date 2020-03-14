#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngineInternal;
using UnityEngine.Profiling;
using UnityEditorInternal;

namespace Crest
{
    public class MemoryTests
    {
        private struct ProfilerState
        {
            public bool enabled;
            public bool driverEnabled;
            public ProfilerMemoryRecordMode profilerMemoryRecord;
        }

        private static ProfilerState SaveProfilerState()
        {
            return new ProfilerState()
            {
                enabled = Profiler.enabled,
                driverEnabled = ProfilerDriver.enabled,
                profilerMemoryRecord = ProfilerDriver.memoryRecordMode,
            };
        }

        private static void RestoreProfilerState(ProfilerState profilerState)
        {
            Profiler.enabled = profilerState.enabled;
            ProfilerDriver.enabled = profilerState.driverEnabled;
            ProfilerDriver.memoryRecordMode = profilerState.profilerMemoryRecord;
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator NoGarbageIsGenerated()
        {
            ProfilerState profilerState = SaveProfilerState();
            try
            {
                Profiler.enabled = true;
                ProfilerDriver.enabled = true;
                ProfilerDriver.memoryRecordMode = ProfilerMemoryRecordMode.ManagedAllocations;
                SceneManager.LoadScene("main", LoadSceneMode.Single);
                yield return null;
                ProfilerDriver.ClearAllFrames();
                // Use the Assert class to test conditions.
                // Use yield to skip a frame.
                yield return new WaitForSeconds(0.5f);

                // need to grab GC info from the profiler. Would use this API:
                // https://docs.unity3d.com/ScriptReference/Profiling.HierarchyFrameDataView.html
                // But it doesn't seem to be available in 2018.4 :(
            }
            finally
            {
                RestoreProfilerState(profilerState);
            }
        }
    }
}

#endif
