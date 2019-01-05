// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;

namespace Crest
{
    /// <summary>
    /// I reallllly wanted to use a sorted list, but was getting garbage when doing foreach loop, so this
    /// really dumb wrapper caches arrays for keys and values and refreshes them when Adding and Removing.
    /// This is only barely a good idea when keys are not added and removed every frame, and the less they
    /// are added/removed the better.
    /// </summary>
    public class SortedListCachedArrays<TKey, TValue> : SortedList<TKey, TValue>
    {
        public TKey[] KeyArray = new TKey[0];
        public TValue[] ValueArray = new TValue[0];

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);

            RefreshArrays();
        }

        public new void Remove(TKey key)
        {
            base.Remove(key);

            RefreshArrays();
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);

            RefreshArrays();
        }

        void RefreshArrays()
        {
            if (Count != KeyArray.Length) KeyArray = new TKey[Count];
            if (Count != ValueArray.Length) ValueArray = new TValue[Count];

            Keys.CopyTo(KeyArray, 0);
            Values.CopyTo(ValueArray, 0);
        }
    }
}
