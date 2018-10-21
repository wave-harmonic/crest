using UnityEngine;

namespace Crest
{
    public abstract class TimeProviderBase : MonoBehaviour
    {
        public abstract float CurrentTime { get; }
    }
}
