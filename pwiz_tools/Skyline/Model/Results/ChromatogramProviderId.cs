using System;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results
{
    public struct ChromatogramProviderId : IComparable<ChromatogramProviderId>
    {
        public static readonly ChromatogramProviderId INVALID = new ChromatogramProviderId(-1);
        private int _intValue;
        private ChromatogramProviderId(int value)
        {
            _intValue = value;
        }

        public static TypeSafeList<ChromatogramProviderId, TItem> TypeSafeList<TItem>()
        {
            return new ListImpl<TItem>();
        }

        public int CompareTo(ChromatogramProviderId other)
        {
            return _intValue.CompareTo(other._intValue);
        }

        private class ListImpl<TItem> : TypeSafeList<ChromatogramProviderId, TItem>
        {
            protected override int IndexFromKey(ChromatogramProviderId key)
            {
                return key._intValue;
            }

            protected override ChromatogramProviderId KeyFromIndex(int index)
            {
                return new ChromatogramProviderId(index);
            }
        }
    }
}