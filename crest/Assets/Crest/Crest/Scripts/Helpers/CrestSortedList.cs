// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Crest
{
    /// <summary>
    /// I reallllly wanted to use a sorted list, but was getting garbage when doing foreach loop, so this
    /// implements a "dumb" sorted list that has to be explicitly resorted.
    /// </summary>
    public class CrestSortedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        IComparer<KeyValuePair<TKey, TValue>> _comparer;
        public List<KeyValuePair<TKey, TValue>> _backingList = new List<KeyValuePair<TKey, TValue>>();

        private bool _needsSorting = false;

        public int Count => _backingList.Count;

        private class InternalComparer : IComparer<KeyValuePair<TKey, TValue>>
        {
            private IComparer<TKey> _comparer;
            public InternalComparer(IComparer<TKey> comparer)
            {
                _comparer = comparer;
            }
            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return _comparer.Compare(x.Key, y.Key);
            }
        }

        public CrestSortedList(IComparer<TKey> comparer)
        {
            _comparer = new InternalComparer(comparer);
        }

        public void Add(TKey key, TValue value)
        {
            _backingList.Add(new KeyValuePair<TKey, TValue>(key, value));
            _needsSorting = true;
        }



        public bool Remove(TValue value)
        {
            KeyValuePair<TKey, TValue> itemToRemove = default;
            bool removed = false;
            foreach (KeyValuePair<TKey, TValue> item in _backingList)
            {
                if (item.Value.Equals(value))
                {
                    itemToRemove = item;
                    removed = true;
                }
            }

            if (removed)
            {
                _backingList.Remove(itemToRemove);
                _needsSorting = true;
            }
            return removed;
        }

        public void ResortArrays()
        {
            if (_needsSorting)
            {
                _backingList.Sort(_comparer);
            }
            _needsSorting = false;
        }

        public List<KeyValuePair<TKey, TValue>>.Enumerator GetEnumerator()
        {
            ResortArrays();
            return _backingList.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
