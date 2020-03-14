// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using System.Collections;
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

        public SortedListCachedArrays(IComparer<TKey> comparer) : base(comparer) { }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
        }

        public new void Remove(TKey key)
        {
            base.Remove(key);
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
        }

        public void RefreshArrays()
        {
            if (Count != KeyArray.Length) KeyArray = new TKey[Count];
            if (Count != ValueArray.Length) ValueArray = new TValue[Count];

            Keys.CopyTo(KeyArray, 0);
            Values.CopyTo(ValueArray, 0);
        }

        new public Enumerator GetEnumerator()
        {
            return new Enumerator(0, this);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
        {
            public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(_sortedListCachedArrays.KeyArray[_index], _sortedListCachedArrays.ValueArray[_index]);

            object IEnumerator.Current => Current;

            public Enumerator(int index, SortedListCachedArrays<TKey, TValue> sortedListCachedArrays)
            {
                _index = index;
                _sortedListCachedArrays = sortedListCachedArrays;
            }

            private int _index;
            private SortedListCachedArrays<TKey, TValue> _sortedListCachedArrays;

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                _index++;
                if (_index < _sortedListCachedArrays.KeyArray.Length)
                {
                    return true;
                }
                return false;
            }

            public void Reset()
            {

            }
        }
    }
}
