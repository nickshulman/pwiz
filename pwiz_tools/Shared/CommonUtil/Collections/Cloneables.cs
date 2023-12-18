using JetBrains.Annotations;

namespace pwiz.Common.Collections
{
    public interface IDeepCloneable<out T>
    {
        T DeepClone();
    }

    public class DeepCloneable<T> : IDeepCloneable<T>
    {
        T IDeepCloneable<T>.DeepClone()
        {
            return DeepClone();
        }

        protected virtual T DeepClone()
        {
            return (T)MemberwiseClone();
        }
    }

    public struct ImmutableDeepCloneable<T> where T : IDeepCloneable<T>
    {
        private T _value;

        public ImmutableDeepCloneable(T value)
        {
            _value = CloneValue(value);
        }

        public static implicit operator T(ImmutableDeepCloneable<T> value)
        {
            return CloneValue(value._value);
        }

        public static implicit operator ImmutableDeepCloneable<T>([CanBeNull] T value)
        {
            return new ImmutableDeepCloneable<T>(value);
        }

        private static T CloneValue(T value)
        {
            return value != null ? value.DeepClone() : default;
        }
    }
}
