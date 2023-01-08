// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections;
using System.Collections.Generic;

namespace Crest
{
    /// <summary>
    /// This is a list this is meant to be similar in behaviour to the C#
    /// SortedList, but without allocations when used directly in a foreach loop.
    ///
    /// It works by using a regular list as as backing and ensuring that it is
    /// sorted when the enumerator is accessed and used. This is a simple approach
    /// that means we avoid sorting each time an element is added, and helps us
    /// avoid having to develop our own more complex data structure.
    /// </summary>
    public class CrestSortedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        public int Count => _backingList.Count;

        private List<KeyValuePair<TKey, TValue>> _backingList = new List<KeyValuePair<TKey, TValue>>();
        System.Comparison<TKey> _comparison;
        private bool _needsSorting = false;

        int Comparison(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return _comparison(x.Key, y.Key);
        }

        public CrestSortedList(System.Comparison<TKey> comparison)
        {
            // We provide the only constructors that SortedList provides that
            // we need. We wrap the input IComparer to ensure that our backing list
            // is sorted in the same way a SortedList would be with the same one.
            _comparison = comparison;
        }

        public void Add(TKey key, TValue value)
        {
            _backingList.Add(new KeyValuePair<TKey, TValue>(key, value));
            _needsSorting = true;
        }

        public bool Remove(TValue value)
        {
            // This remove function has a fairly high complexity, as we need to search
            // the list for a matching Key-Value pair, and then remove it. However,
            // for the small lists we work with this is fine, as we don't use this
            // function more often. But it's worth bearing in mind if we decide to
            // expand where we use this list. At that point we might need to take a
            // different approach.

            var removeIndex = -1;
            var index = 0;
            foreach (KeyValuePair<TKey, TValue> item in _backingList)
            {
                if (item.Value.Equals(value))
                {
                    removeIndex = index;
                }

                index++;
            }

            if (removeIndex > -1)
            {
                // Remove method produces garbage.
                _backingList.RemoveAt(removeIndex);
            }

            return removeIndex > -1;
        }

        public void Clear()
        {
            _backingList.Clear();
            _needsSorting = false;
        }

        #region GetEnumerator
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
        #endregion

        private void ResortArrays()
        {
            if (_needsSorting)
            {
                // @GC: Allocates 112B.
                _backingList.Sort(Comparison);
            }
            _needsSorting = false;
        }
    }
}
