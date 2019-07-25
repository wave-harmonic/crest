// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Default time provider - sets the ocean time to Unity's game time.
    /// </summary>
    public class TimeProviderDefault : TimeProviderBase
    {
        public override float CurrentTime => Time.time;
        public override float DeltaTime => Time.deltaTime;
    }
}
