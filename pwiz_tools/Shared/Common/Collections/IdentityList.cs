using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace pwiz.Common.Collections
{
    public class IdentityList<T> : AbstractReadOnlyList<T> where T : class
    {
        public static readonly IdentityEqualityComparer<T> EQUALITY_COMPARER = new IdentityEqualityComparer<T>();
        public static readonly IdentityList<T> EMPTY = new IdentityList<T>(ImmutableList.Empty<T>());
        private ImmutableList<T> _list;
        public IdentityList(IEnumerable<T> identities)
        {
            _list = ImmutableList.ValueOfOrEmpty(identities);
        }

        public static IdentityList<T> ValueOf(IEnumerable<T> identities)
        {
            var list = ImmutableList.ValueOfOrEmpty(identities);
            if (list.Count == 0)
            {
                return EMPTY;
            }
            return new IdentityList<T>(list);
        }

        public override T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public override int Count => _list.Count;

        public override int IndexOf(T item)
        {
            int index = 0;
            foreach (var x in this)
            {
                if (ReferenceEquals(x, item))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        protected bool Equals(IdentityList<T> other)
        {
            return _list.SequenceEqual(other._list, EQUALITY_COMPARER);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IdentityList<T>) obj);
        }

        public override int GetHashCode()
        {
            return _list.Aggregate(0, (seed, item) => seed * 397 + RuntimeHelpers.GetHashCode(item));
        }
    }
}
