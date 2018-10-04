
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
