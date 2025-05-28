using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Util
{
    public class RetentionTimeValue : DoubleValue
    {
        public RetentionTimeValue(double value) : this(value, null)
        {
        }
        public RetentionTimeValue(double value, double? normalizedValue) : base(value)
        {
            Normalized = normalizedValue;
        }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? Normalized { get; private set; }
    }
}
