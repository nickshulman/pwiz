﻿using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveSettings : Immutable
    {
        public static readonly CurveSettings Default = new CurveSettings();
        private ImmutableDeepCloneable<CurveFormat> _curveFormat;
        private ImmutableDeepCloneable<RegressionOptions> _regressionOptions;

        private CurveSettings()
        {
            _curveFormat = new CurveFormat();
            _regressionOptions = new RegressionOptions();
        }
        public RetentionTimeSource YAxis { get; private set; }

        public CurveSettings ChangeYAxis(RetentionTimeSource value)
        {
            return ChangeProp(ImClone(this), im => im.YAxis = value);
        }

        public RegressionOptions RegressionOptions
        {
            get { return _regressionOptions; }
        }

        public CurveSettings ChangeRegressionOptions(RegressionOptions regressionOptions)
        {
            return ChangeProp(ImClone(this), im => im._regressionOptions = regressionOptions);
        }

        public string Caption { get; private set; }

        public CurveSettings ChangeCaption(string value)
        {
            return ChangeProp(ImClone(this), im => im.Caption = value);
        }

        protected bool Equals(CurveSettings other)
        {
            return Equals(YAxis, other.YAxis) && Equals(RegressionOptions, other.RegressionOptions) &&
                   Equals(CurveFormat, other.CurveFormat) && 
                   Caption == other.Caption;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CurveSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (YAxis != null ? YAxis.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _regressionOptions.GetHashCode();
                hashCode = (hashCode * 397) ^ _curveFormat.GetHashCode();
                hashCode = (hashCode * 397) ^ (Caption != null ? Caption.GetHashCode() : 0);
                return hashCode;
            }
        }

        public CurveFormat CurveFormat
        {
            get { return _curveFormat;}
            private set
            {
                _curveFormat = value;
            }
        }

        public CurveSettings ChangeCurveFormat(CurveFormat curveFormat)
        {
            return ChangeProp(ImClone(this), im => im.CurveFormat = curveFormat);
        }


        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Caption))
            {
                return Caption;
            }
            return YAxis?.ToString() ?? string.Empty;
        }
    }
}
