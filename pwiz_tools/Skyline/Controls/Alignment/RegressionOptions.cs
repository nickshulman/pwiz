using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Controls.Alignment
{
    public class RegressionOptions
    {
        public RegressionMethodRT? RegressionMethod
        {
            get;
            set;
        }

        [TypeConverter(typeof(SpectrumDigestLengthConvert))]
        public int SpectrumDigestLength { get; set; } = 16;

        public class SpectrumDigestLengthConvert : TypeConverter
        {
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(new[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048 });
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string stringValue)
                {
                    return int.Parse(stringValue);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (RegressionMethod.HasValue)
            {
                parts.Add("Regression Method: " + RegressionMethod);
            }
            parts.Add("Spectrum Digest Length: " + SpectrumDigestLength);
            return TextUtil.SpaceSeparate(parts);
        }
    }
}
