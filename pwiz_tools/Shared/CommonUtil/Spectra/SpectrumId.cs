using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using pwiz.Common.Collections;

namespace pwiz.Common.Spectra
{
    public readonly struct SpectrumId 
    {
        private readonly object _value;

        public static SpectrumId FromString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return default;
            }
            var parts = str.Split('.');
            if (parts.Length == 0)
            {
                return new SpectrumId(str);
            }

            var integers = new List<int>(parts.Length);
            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int intValue) || intValue.ToString() != part)
                {
                    return new SpectrumId(str);
                }
                integers.Add(intValue);
            }

            return FromIntegers(integers);
        }

        public static SpectrumId FromIntegers(IEnumerable<int> integers)
        {
            var intArray = integers.ToArray();
            if (intArray.Length == 0)
            {
                return default;
            }
            if (intArray.All(i => 0 <= i && i <= ushort.MaxValue))
            {
                return new SpectrumId(intArray.Select(i => (ushort)i).ToArray());
            }

            return new SpectrumId(intArray);
        }

        private SpectrumId(string value)
        {
            _value = value;
        }

        private SpectrumId(ushort[] value)
        {
            _value = value;
        }

        private SpectrumId(int[] value)
        {
            _value = value;
        }
       

        public bool Equals(SpectrumId other)
        {
            if (ReferenceEquals(_value, other._value))
            {
                return true;
            }
            if (_value == null || other._value == null)
            {
                return false;
            }
            if (_value.GetType() != other._value.GetType())
            {
                return false;
            }

            if (_value is string)
            {
                return Equals(_value, other._value);
            }

            if (_value is ushort[] ushorts)
            {
                return ushorts.SequenceEqual((ushort[])other._value);
            }
            return ((int[])_value).SequenceEqual((int[])other._value);
        }
        
        public override bool Equals(object obj)
        {
            return obj is SpectrumId other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (_value == null)
            {
                return 0;
            }
            if (_value is string)
            {
                return _value.GetHashCode();
            }

            if (_value is ushort[] ushorts)
            {
                return CollectionUtil.GetHashCodeDeep(ushorts);
            }
            return CollectionUtil.GetHashCodeDeep((int[])_value);
        }

        public override string ToString()
        {
            if (_value == null)
            {
                return string.Empty;
            }

            if (_value is string str)
            {
                return str;
            }

            return string.Join(@".", AsIntegers()!);
        }

        [CanBeNull]
        public IEnumerable<int> AsIntegers()
        {
            if (_value is ushort[] ushorts)
            {
                return ushorts.Select(v => (int)v);
            }

            if (_value is int[] ints)
            {
                return ints.AsEnumerable();
            }

            return null;
        }
    }
}
