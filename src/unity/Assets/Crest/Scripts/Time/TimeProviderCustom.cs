// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    public class TimeProviderCustom : TimeProviderBase
    {
        public float _time = 0f;

        public override float CurrentTime
        {
            get
            {
                return _time;
            }
        }
    }
}
