// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
