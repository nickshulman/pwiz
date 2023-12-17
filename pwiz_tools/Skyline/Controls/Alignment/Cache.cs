using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace pwiz.Skyline.Controls.Alignment
{
    public class Cache
    {
        private Dictionary<TypedKey, object> _staleDictionary;
        private Dictionary<TypedKey, object> _currentDictionary;

        public Cache()
        {
            _staleDictionary = new Dictionary<TypedKey, object>();
            _currentDictionary = new Dictionary<TypedKey, object>();
        }


        public bool TryGetValue<T>(ITuple key, out T value)
        {
            var typedKey = new TypedKey(key, typeof(T));
            if (_staleDictionary.TryGetValue(typedKey, out var valueObject))
            {
                _currentDictionary[typedKey] = valueObject;
                _staleDictionary.Remove(typedKey);
            }
            else if (!_currentDictionary.TryGetValue(typedKey, out valueObject))
            {
                value = default;
                return false;
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
            if (TryGetValue<T>(key, out T result))
            {
                return result;
            }

            result = fallback();
            AddValue(key, result);
            return result;
        }

        public void DumpStaleObjects()
        {
            _staleDictionary = _currentDictionary;
            _currentDictionary = new Dictionary<TypedKey, object>();
        }
        private class TypedKey
        {
            public TypedKey(ITuple key, Type valueType)
            {
                Key = key;
                ValueType = valueType;
            }

            public ITuple Key { get; }
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
