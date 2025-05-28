using System;
using System.ComponentModel;
using System.Globalization;

namespace pwiz.Skyline.Util
{
    [TypeConverter(typeof(TypeConverterImpl))]

    public class DoubleValue : IComparable, IFormattable, IConvertible
    {
        protected double _doubleValue;

        protected DoubleValue(double value)
        {
            _doubleValue = value;
        }

        public override string ToString()
        {
            return _doubleValue.ToString(null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(string format)
        {
            return _doubleValue.ToString(format);
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToBoolean(provider);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToChar(provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToSByte(provider);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToByte(provider);

        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToInt16(provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToUInt16(provider);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToInt32(provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToUInt32(provider);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToInt64(provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToUInt64(provider);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToSingle(provider);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToDouble(provider);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToDecimal(provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToDateTime(provider);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToString(provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return ((IConvertible)_doubleValue).ToType(conversionType, provider);
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            return _doubleValue.CompareTo(((DoubleValue)obj)._doubleValue);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return _doubleValue.ToString(format, formatProvider);
        }

        private class TypeConverterImpl : TypeConverter
        {
            private TypeConverter doubleTypeConverter = TypeDescriptor.GetConverter(typeof(double));
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return doubleTypeConverter.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var doubleValue = ((DoubleValue)value)._doubleValue;
                return doubleTypeConverter.ConvertTo(context, culture, doubleValue, destinationType);
            }
        }

        public static implicit operator double(DoubleValue doubleValue)
        {
            return doubleValue._doubleValue;
        }
    }
}
