using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CalculatedValues
    {
        private Dictionary<TypedKey, object> _oldDictionary;
        private Dictionary<TypedKey, object> _currentDictionary;

        public CalculatedValues(IEnumerable<KeyValuePair<TypedKey, object>> oldValues)
        {
            if (oldValues != null)
            {
                foreach (var entry in oldValues)
                {
                    _oldDictionary ??= new Dictionary<TypedKey, object>();
                    _oldDictionary[entry.Key] = entry.Value;
                }
            }
            _currentDictionary = new Dictionary<TypedKey, object>();
        }


        public bool TryGetValue<T>(object key, out T value)
        {
            var typedKey = new TypedKey(key, typeof(T));
            if (!_currentDictionary.TryGetValue(typedKey, out var valueObject))
            {
                if (true == _oldDictionary?.TryGetValue(typedKey, out valueObject))
                {
                    _currentDictionary[typedKey] = valueObject;
                }
                else
                {
                    value = default;
                    return false;
                }
            }
            value = (T) valueObject;
            return true;
        }

        public void AddValue<T>(ITuple key, T value)
        {
            _currentDictionary[new TypedKey(key, typeof(T))] = value;
        }

        public T GetValue<T>(ITuple key, Func<T> fallback)
        {
            if (TryGetValue(key, out T result))
            {
                return result;
            }

            result = fallback();
            AddValue(key, result);
            return result;
        }

        public IEnumerable<KeyValuePair<TypedKey, object>> GetCurrentValues()
        {
            return _currentDictionary.AsEnumerable();
        }
        public class TypedKey
        {
            public TypedKey(object key, Type valueType)
            {
                Key = key;
                ValueType = valueType;
            }

            public object Key { get; }
            public Type ValueType { get; }

            protected bool Equals(TypedKey other)
            {
                return Key.Equals(other.Key) && ValueType == other.ValueType;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((TypedKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Key.GetHashCode() * 397) ^ ValueType.GetHashCode();
                }
            }
        }
    }
}
