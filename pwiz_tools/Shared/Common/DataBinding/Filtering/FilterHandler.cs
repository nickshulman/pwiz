using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;
using pwiz.Common.DataBinding.Attributes;

namespace pwiz.Common.DataBinding.Filtering
{
    public interface IFilterHandler
    {
        bool IsBlank(object value);
        object ParseOperand(FilterContext context, string text);
        string OperandToString(FilterContext context, object operand);
        bool ValueEqualsOperand(object value, object operand);
        bool CanBeBlank { get; }
        public interface IComparison
        {
            int? Compare(object value, object operand);
        }

        public interface IContains
        {
            bool StartsWith(object value, object operand);
            bool Contains(object value, object operand);
        }
    }


    public abstract class FilterHandler<TColumn, TOperand> : IFilterHandler
    {
        object IFilterHandler.ParseOperand(FilterContext context, string text)
        {
            return ParseOperand(context, text);
        }

        protected abstract TOperand ParseOperand(FilterContext context, string text);

        public string OperandToString(FilterContext context, object operand)
        {
            if (operand is TOperand tOperand)
            {
                return OperandToString(context, tOperand);
            }
            return string.Empty;
        }

        protected abstract string OperandToString(FilterContext context, TOperand operand);

        public abstract bool IsBlank(object value);

        public bool ValueEqualsOperand(object value, object operand)
        {
            return CallWithOperand(value, operand, ValueEqualsOperand);
        }

        protected abstract bool ValueEqualsOperand(TColumn value, TOperand operand);

        protected T CallWithOperand<T>(object value, object operand, Func<TColumn, TOperand, T> func)
        {
            if (TryConvertColumnValue(value, out var columnValue) && operand is TOperand typedOperand)
            {
                return func(columnValue, typedOperand);
            }
            return default;
        }

        protected abstract bool TryConvertColumnValue(object value, out TColumn columnValue);

        public virtual bool CanBeBlank
        {
            get { return true; }
        }
    }

    public class TextFilterHandler : FilterHandler<string, string>
    {
        public static readonly TextFilterHandler WITHOUT_CONTAINS = new TextFilterHandler();
        public static readonly WithContains WITH_CONTAINS = new WithContains();
        public override bool IsBlank(object value)
        {
            return value == null || ValueEqualsOperand(string.Empty, value);
        }


        protected override string ParseOperand(FilterContext context, string text)
        {
            return text;
        }

        protected override string OperandToString(FilterContext context, string operand)
        {
            return operand;
        }

        protected override bool ValueEqualsOperand(string value, string operand)
        {
            return StringComparer.Ordinal.Equals(value, operand);
        }
        
        protected override bool TryConvertColumnValue(object value, out string columnValue)
        {
            if (value == null)
            {
                columnValue = string.Empty;
                return false;
            }
            columnValue = value as string ?? value.ToString();
            return true;
        }

        public class WithContains : TextFilterHandler, IFilterHandler.IContains
        {
            public bool StartsWith(object value, object operand)
            {
                return CallWithOperand(value, operand,
                    (stringValue, stringOperand) => stringValue?.StartsWith(stringOperand ?? string.Empty) ?? false);
            }

            public bool Contains(object value, object operand)
            {
                return CallWithOperand(value, operand,
                    (stringValue, stringOperand) => stringValue?.Contains(stringOperand ?? string.Empty) ?? false);
            }
        }
    }

    public class NumericFilterHandler : FilterHandler<double, PrecisionNumber>, IFilterHandler.IComparison
    {
        public static readonly NumericFilterHandler INSTANCE = new NumericFilterHandler();
        public override bool IsBlank(object value)
        {
            return value == null;
        }

        protected override PrecisionNumber ParseOperand(FilterContext context, string text)
        {
            return PrecisionNumber.Parse(text, context.CultureInfo, context.DefaultDecimalPlaces);
        }

        protected override string OperandToString(FilterContext context, PrecisionNumber operand)
        {
            return operand.ToString(context.CultureInfo, context.DefaultDecimalPlaces);
        }

        protected override bool ValueEqualsOperand(double value, PrecisionNumber operand)
        {
            return operand.EqualsWithinPrecision(value);
        }

        protected override bool TryConvertColumnValue(object value, out double columnValue)
        {
            if (value != null)
            {
                try
                {
                    columnValue = Convert.ToDouble(value);
                    return true;
                }
                catch
                {
                    // ignore
                }
            }
            columnValue = 0;
            return false;
        }

        public int? Compare(object value, object operand)
        {
            return CallWithOperand(value, operand, Compare);
        }

        protected int? Compare(double doubleValue, PrecisionNumber precisionNumber)
        {
            // Return the negative of the result because we want the doubleValue compared to the precisionNumber
            return -precisionNumber.CompareTo(doubleValue);
        }

        private bool ExplicitPrecision(IFilterOperation filterOperation, CultureInfo cultureInfo)
        {
            return string.IsNullOrEmpty(cultureInfo.Name) || !filterOperation.UsesEquality();
        }

        public override bool CanBeBlank
        {
            get { return false; }
        }
    }

    public class ListFilterHandler : IFilterHandler, IFilterHandler.IContains
    {
        public ListFilterHandler(IFilterHandler elementHandler)
        {
            ElementHandler = elementHandler;
        }

        public IFilterHandler ElementHandler { get; }

        public bool IsBlank(object value)
        {
            return 0 == ((value as IListColumnValue)?.Count ?? 0);
        }

        public object ParseOperand(FilterContext context, string text)
        {
            if (text == null)
            {
                return null;
            }

            var strings = ListColumnValue.Parse(text, ListColumnValue.GetCsvSeparator(context.CultureInfo));
            if (strings == null)
            {
                return null;
            }

            return ListColumnValue.FromItems(strings.Items.Select(str =>
                ElementHandler.ParseOperand(context.ChangeFilterOperation(FilterOperations.OP_EQUALS), str)));
        }

        public bool ValueEqualsOperand(object value, object operand)
        {
            var listValue = ToListValue(value);
            if (listValue == null)
            {
                return false;
            }
            if (!(operand is IListColumnValue listOperand))
            {
                return false;
            }

            if (listOperand.Count == 0)
            {
                return listValue.Count == 0;
            }

            if (listOperand.Count == 1)
            {
                return listValue.Count > 0 && listValue.AsEnumerable()
                    .All(item => ElementHandler.ValueEqualsOperand(item, listOperand.AsEnumerable().First()));
            }

            if (listOperand.Count != listValue.Count)
            {
                return false;
            }
            return ElementsEqual(listValue.AsEnumerable(), listOperand.AsEnumerable().ToList());
        }

        public string OperandToString(FilterContext context, object operand)
        {
            var operandList = operand as IListColumnValue;
            if (operandList == null)
            {
                return string.Empty;
            }

            var strings = operandList.AsEnumerable()
                .Select(v => ElementHandler.OperandToString(context.ChangeFilterOperation(FilterOperations.OP_EQUALS), v))
                .ToImmutable();
            return ListColumnValue.ItemsToString(context.CultureInfo, strings);
        }

        public bool StartsWith(object value, object operand)
        {
            var listValue = ToListValue(value);
            if (listValue == null || !(operand is IList<object> listOperand))
            {
                return false;
            }

            return listValue.Count >= listOperand.Count &&
                   ElementsEqual(listValue.AsEnumerable().Take(listOperand.Count), listOperand);
        }

        public bool Contains(object value, object operand)
        {
            var listValue = ToListValue(value);
            if (listValue == null || !(operand is IList<object> listOperand))
            {
                return false;
            }

            if (listOperand.Count == 0)
            {
                return true;
            }

            var window = new List<object>();
            foreach (var item in listValue.AsEnumerable())
            {
                if (window.Count == listOperand.Count)
                {
                    window.RemoveAt(0);
                }
                window.Add(item);
                if (window.Count == listOperand.Count && ElementsEqual(window, listOperand))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual IListColumnValue ToListValue(object columnValue)
        {
            return columnValue as IListColumnValue;
        }

        private bool ElementsEqual(IEnumerable<object> items, IList<object> operands)
        {
            return items.Zip(operands, (item, operand) =>
                ElementHandler.ValueEqualsOperand(item, operand)).All(result => result);
        }

        public bool CanBeBlank
        {
            get { return true; }
        }
    }
    public class EnumFilterHandler : IFilterHandler
    {
        public EnumFilterHandler(Type enumType)
        {
            EnumType = enumType;
        }

        public Type EnumType { get; }
        public bool IsBlank(object value)
        {
            return value == null;
        }

        public object ParseOperand(FilterContext context, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            try
            {
                return Enum.Parse(EnumType, text);
            }
            catch
            {
                return Enum.Parse(EnumType, text, true);
            }
        }

        public string OperandToString(FilterContext context, object operand)
        {
            return operand?.ToString() ?? string.Empty;
        }

        public bool ValueEqualsOperand(object value, object operand)
        {
            return Equals(value, operand);
        }

        public string OperandToString(object operand, CultureInfo cultureInfo)
        {
            return operand?.ToString() ?? string.Empty;
        }

        public bool CanBeBlank
        {
            get { return false; }
        }
    }

    public class SimpleFilterHandler : IFilterHandler
    {
        public SimpleFilterHandler(Type type)
        {
            ValueType = type;
        }

        public Type ValueType { get; }

        public bool IsBlank(object value)
        {
            return value == null;
        }

        public object ParseOperand(FilterContext context, string text)
        {
            var typeConverter = TypeDescriptor.GetConverter(ValueType);
            // ReSharper disable AssignNullToNotNullAttribute
            return typeConverter.ConvertFrom(null, context.CultureInfo, text);
        }

        public string OperandToString(FilterContext context, object operand)
        {
            return Convert.ToString(operand, context.CultureInfo);
        }

        public bool ValueEqualsOperand(object value, object operand)
        {
            return Equals(value, operand);
        }

        public bool CanBeBlank
        {
            get
            {
                return !ValueType.IsValueType;
            }
        }

        public class Comparable : SimpleFilterHandler, IFilterHandler.IComparison
        {
            public Comparable(Type type) : base(type)
            {
            }

            public int? Compare(object value, object operand)
            {
                return (value as IComparable)?.CompareTo(operand);
            }
        }
    }

    public class FilterContext : Immutable
    {
        public FilterContext(CultureInfo cultureInfo, IFilterOperation filterOperation, int? defaultDecimalPlaces)
        {
            CultureInfo = cultureInfo;
            FilterOperation = filterOperation;
            DefaultDecimalPlaces = defaultDecimalPlaces;
        }
        public CultureInfo CultureInfo { get; }
        public IFilterOperation FilterOperation { get; private set; }
        public int? DefaultDecimalPlaces { get; }

        public FilterContext ChangeFilterOperation(IFilterOperation filterOperation)
        {
            return ChangeProp(ImClone(this), im => im.FilterOperation = filterOperation);
        }

        public static FilterContext Invariant(IFilterOperation operation)
        {
            return new FilterContext(CultureInfo.InvariantCulture, operation, null);
        }

        public static FilterContext ForColumn(DataPropertyDescriptor propertyDescriptor, IFilterOperation filterOperation)
        {
            int? precision = GetFormatStringPrecision(((FormatAttribute)propertyDescriptor.Attributes[typeof(FormatAttribute)])?.Format);
            return new FilterContext(CultureInfo.CurrentCulture, filterOperation, precision);
        }

        public static int? GetFormatStringPrecision(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return null;

            format = format.Trim();

            // Standard numeric format strings: F2, N4, C2, P2, E6, G8
            var standardMatch = Regex.Match(format, @"^([FfNnCcPpEeGg])(\d{0,2})$");
            if (standardMatch.Success)
            {
                var specifier = standardMatch.Groups[1].Value.ToUpper();
                var digits = standardMatch.Groups[2].Value;

                // These specifiers don't have a meaningful decimal precision
                if (specifier == @"D" || specifier == @"X" || specifier == @"R")
                    return null;

                if (digits.Length > 0)
                    return int.Parse(digits);

                // Defaults when no digit is specified
                switch (specifier)
                {
                    case "F":
                    case "N": return 2;
                    case "E": return 6;
                    case "G": return null;  // Varies — too ambiguous
                    case "C": return 2;     // Culture-dependent, but 2 is a safe guess
                    case "P": return 2;
                    default: return null;
                }
            }

            // Custom format strings: "0.00##", "#,##0.0000", etc.
            var decimalIndex = format.IndexOf('.');
            if (decimalIndex >= 0)
            {
                var afterDecimal = format.Substring(decimalIndex + 1);
                // Stop at any non-placeholder character (e.g. 'E' in "0.00E+0")
                var precisionChars = Regex.Match(afterDecimal, @"^[0#]+");
                if (precisionChars.Success)
                    return precisionChars.Value.Length;
            }

            return null;
        }
    }
}
