using UnityEngine;

namespace Crest
{
    public class TimeProviderDefault : TimeProviderBase
    {
        public override float CurrentTime
        {
            get
            {
                return Time.time;
            }
        }
    }
}
