using System.Collections.Generic;

namespace Crest
{
    public class SortedListCachedArrays<TKey, TValue> : SortedList<TKey, TValue>
    {
        public TKey[] KeyArray = new TKey[0];
        public TValue[] ValueArray = new TValue[0];

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);

            if (Count != KeyArray.Length) KeyArray = new TKey[Count];
            if (Count != ValueArray.Length) ValueArray = new TValue[Count];

            Keys.CopyTo(KeyArray, 0);
            Values.CopyTo(ValueArray, 0);
        }
    }
}
