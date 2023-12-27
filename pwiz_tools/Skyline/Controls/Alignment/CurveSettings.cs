using pwiz.Skyline.Model;
using System.ComponentModel;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveSettings : UserInterfaceObject
    {
        public CurveSettings(IDocumentContainer documentContainer) : base(documentContainer)
        {
        }
        public RetentionTimeSource YAxis { get; set; }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RegressionOptions RegressionOptions { get; private set; } = new RegressionOptions();
        public string Caption { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public CurveFormat CurveFormat
        {
            get;
            private set;
        } = new CurveFormat();

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Caption))
            {
                return Caption;
            }
            return YAxis?.ToString() ?? string.Empty;
        }

        protected bool Equals(CurveSettings other)
        {
            return Equals(YAxis, other.YAxis) && Equals(RegressionOptions, other.RegressionOptions) && Caption == other.Caption && Equals(CurveFormat, other.CurveFormat);
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
                hashCode = (hashCode * 397) ^ (RegressionOptions != null ? RegressionOptions.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Caption != null ? Caption.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CurveFormat != null ? CurveFormat.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
