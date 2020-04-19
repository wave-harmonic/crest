// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    /// <summary>
    /// This time provider fixes the ocean time at a custom value which is usable for testing/debugging.
    /// </summary>
    public class TimeProviderCustom : TimeProviderBase
    {
        public float _time = 0f;
        public float _deltaTime = 0f;

        public override float CurrentTime => _time;
        public override float DeltaTime => _deltaTime;
    }
}
