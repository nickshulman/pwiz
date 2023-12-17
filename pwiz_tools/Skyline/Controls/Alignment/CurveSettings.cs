using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveSettings : Immutable
    {
        public static readonly CurveSettings Default = new CurveSettings();
        private CurveFormat _curveFormat;

        private CurveSettings()
        {
            CurveFormat = new CurveFormat();
        }
        public RetentionTimeSource YAxis { get; private set; }

        public CurveSettings ChangeYAxis(RetentionTimeSource value)
        {
            return ChangeProp(ImClone(this), im => im.YAxis = value);
        }

        public RegressionMethodRT? RegressionMethod { get; private set; }

        public CurveSettings ChangeRegressionMethod(RegressionMethodRT? value)
        {
            return ChangeProp(ImClone(this), im => im.RegressionMethod = value);
        }

        public string Caption { get; private set; }

        public CurveSettings ChangeCaption(string value)
        {
            return ChangeProp(ImClone(this), im => im.Caption = value);
        }

        protected bool Equals(CurveSettings other)
        {
            return Equals(YAxis, other.YAxis) && RegressionMethod == other.RegressionMethod &&
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
                hashCode = (hashCode * 397) ^ RegressionMethod.GetHashCode();
                hashCode = (hashCode * 397) ^ CurveFormat.GetHashCode();
                hashCode = (hashCode * 397) ^ (Caption != null ? Caption.GetHashCode() : 0);
                return hashCode;
            }
        }

        public CurveFormat CurveFormat
        {
            get { return _curveFormat.Clone();}
            private set
            {
                _curveFormat = value.Clone();
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
