using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace pwiz.Common.Collections
{
    public abstract class TypeSafeList<TKey, TValue> : ICollection<TValue>
    {
        public static readonly TypeSafeList<TKey, TValue> EMPTY = new Empty();
        private IList<TValue> _list;

        protected TypeSafeList(IList<TValue> list)
        {
            _list = list;
        }

        protected TypeSafeList() : this(new List<TValue>())
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> KeyValuePairs
        {
            get
            {
                return _list.Select((item, index) => new KeyValuePair<TKey, TValue>(KeyFromIndex(index), item));
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                return Enumerable.Range(0, _list.Count).Select(KeyFromIndex);
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        void ICollection<TValue>.Add(TValue item)
        {
            Add(item);
        }

        public TKey Add(TValue item)
        {
            var key = KeyFromIndex(_list.Count);
            _list.Add(item);
            return key;
        }


        public bool Contains(TValue item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        bool ICollection<TValue>.Remove(TValue item)
        {
            throw new InvalidOperationException();
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsReadOnly
        {
            get
            {
                return _list.IsReadOnly;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _list[IndexFromKey(key)];
            }
            set
            {
                _list[IndexFromKey(key)] = value;
            }
        }

        protected abstract int IndexFromKey(TKey key);
        protected abstract TKey KeyFromIndex(int index);

        public virtual TypeSafeList<TKey, TValue> MakeImmutable()
        {
            var immutableList = ImmutableList.ValueOf(_list);
            if (ReferenceEquals(immutableList, _list))
            {
                return this;
            }

            var typeSafeList = (TypeSafeList<TKey, TValue>)MemberwiseClone();
            typeSafeList._list = immutableList;
            return typeSafeList;
        }

        public void AddRange(IEnumerable<TValue> items)
        {
            _list.AddRange(items);
        }

        public bool ContainsKey(TKey key)
        {
            int index = IndexFromKey(key);
            return index >= 0 && index < _list.Count;
        }

        private class Empty : TypeSafeList<TKey, TValue>
        {
            public Empty() : base(ImmutableList.Empty<TValue>())
            {
            }

            protected override int IndexFromKey(TKey key)
            {
                return 0;
            }

            protected override TKey KeyFromIndex(int index)
            {
                return default; 
            }
        }
    }
}
