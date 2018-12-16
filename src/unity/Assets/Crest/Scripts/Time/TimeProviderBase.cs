// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public abstract class TimeProviderBase : MonoBehaviour
    {
        public abstract float CurrentTime { get; }
    }
}
