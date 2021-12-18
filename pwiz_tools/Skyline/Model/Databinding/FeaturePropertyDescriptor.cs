﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding
{
    public interface IFeatureScores
    {
        float? GetFeature(IPeakFeatureCalculator calculator);
    }
    public class FeaturePropertyDescriptor : PropertyDescriptor
    {
        public const string FEATURE_PREFIX = "feature_";
        private IPeakFeatureCalculator _calculator;
        public FeaturePropertyDescriptor(IPeakFeatureCalculator calculator, CultureInfo language) 
            : base(FEATURE_PREFIX + calculator.HeaderName, GetAttributes(calculator, language))
        {
            _calculator = calculator;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            return (component as IFeatureScores)?.GetFeature(_calculator);
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
            throw new InvalidOperationException();
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override Type ComponentType => typeof(IFeatureScores);

        public override bool IsReadOnly => true;

        public override Type PropertyType => typeof(float?);

        public static Attribute[] GetAttributes(IPeakFeatureCalculator calculator, CultureInfo language)
        {
            string displayName = LocalizationHelper.CallWithCulture(language, () => calculator.Name);
            string toolTip = calculator.Tooltip;
            var attributes = new List<Attribute> { new DisplayNameAttribute(displayName) };
            if (!string.IsNullOrEmpty(toolTip))
            {
                attributes.Add(new DescriptionAttribute(toolTip));
            }
            attributes.Add(new FormatAttribute(Formats.PEAK_SCORE) { NullValue = TextUtil.EXCEL_NA });
            return attributes.ToArray();
        }

        public static IEnumerable<FeaturePropertyDescriptor> ListProperties(Type type, CultureInfo language)
        {
            if (!typeof(IFeatureScores).IsAssignableFrom(type))
            {
                return ImmutableList<FeaturePropertyDescriptor>.EMPTY;
            }

            return PeakFeatureCalculator.Calculators.Select(calc => new FeaturePropertyDescriptor(calc, language));
        }
    }
}
