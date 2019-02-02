using System;
using System.Collections.Generic;
using System.Linq;

namespace pwiz.Common.Collections
{
    /// <summary>
    /// Run-length-encoded list
    /// </summary>
    public static class RunLengthEncodedList
    {
        public static IList<T> Compress<T>(IEnumerable<T> items)
        {
            var indices = new List<int>();
            var itemList = new List<T>();
            int count = 0;
            foreach (var item in items)
            {
                if (itemList.Count == 0)
                {
                    itemList.Add(item);
                }
                else
                {
                    if (!Equals(item, itemList[itemList.Count - 1]))
                    {
                        indices.Add(count);
                        itemList.Add(item);
                    }
                }
                count++;
            }

            if (count == 0)
            {
                return ImmutableList<T>.EMPTY;
            }
            indices.Add(count);
            var impl = new Impl<T>(indices, itemList);
            if (itemList.Count >= count / 2)
            {
                return ImmutableList.ValueOf(impl);
            }

            return impl;
        }

        private class Impl<T> : AbstractReadOnlyList<T>
        {
            private ImmutableList<int> _indices;
            private ImmutableList<T> _items;

            public Impl(IEnumerable<int> indices, IEnumerable<T> items)
            {
                _indices = ImmutableList.ValueOf(indices);
                _items = ImmutableList.ValueOf(items);
            }

            public override int Count
            {
                get
                {
                    if (_indices.Count == 0)
                    {
                        return 0;
                    }

                    return _indices[_indices.Count - 1];
                }
            }

            public override T this[int index]
            {
                get
                {
                    if (index < 0 || index >= Count)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    int bucket = CollectionUtil.BinarySearch(_indices, index);
                    if (bucket < 0)
                    {
                        bucket = ~bucket;
                    }

                    return _items[bucket];
                }
            }

            public override IEnumerator<T> GetEnumerator()
            {
                return Enumerable.Range(0, _indices.Count).SelectMany(GetBucketItems).GetEnumerator();
            }

            private IEnumerable<T> GetBucketItems(int bucketIndex)
            {
                int count = _indices[bucketIndex] - (bucketIndex == 0 ? 0 : _indices[bucketIndex - 1]);
                return Enumerable.Repeat(_items[bucketIndex], count);
            }
        }
    }
}
