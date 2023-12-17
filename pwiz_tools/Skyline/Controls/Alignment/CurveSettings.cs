using System.Drawing;
using System.Drawing.Drawing2D;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.RetentionTimes;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveSettings : Immutable
    {
        public static readonly CurveSettings Default = new CurveSettings
        {
            LineColor = Color.Black,
            SymbolColor = Color.Black
        };
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

        public DashStyle? LineDashStyle { get; private set; }

        public CurveSettings ChangeLineDashStyle(DashStyle? value)
        {
            return ChangeProp(ImClone(this), im => im.LineDashStyle = value);
        }

        public Color LineColor { get; private set; }

        public CurveSettings ChangeLineColor(Color value)
        {
            return ChangeProp(ImClone(this), im => im.LineColor = value);
        }

        public string Caption { get; private set; }

        public CurveSettings ChangeCaption(string value)
        {
            return ChangeProp(ImClone(this), im => im.Caption = value);
        }

        public SymbolType SymbolType { get; private set; }

        public CurveSettings ChangeSymbolType(SymbolType value)
        {
            return ChangeProp(ImClone(this), im => im.SymbolType = value);
        }

        public Color SymbolColor { get; private set; }

        public CurveSettings ChangeSymbolColor(Color value)
        {
            return ChangeProp(ImClone(this), im => im.SymbolColor = value);
        }

        protected bool Equals(CurveSettings other)
        {
            return Equals(YAxis, other.YAxis) && RegressionMethod == other.RegressionMethod &&
                   LineDashStyle == other.LineDashStyle && LineColor.Equals(other.LineColor) &&
                   Caption == other.Caption && SymbolType == other.SymbolType && SymbolColor.Equals(other.SymbolColor);
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
                hashCode = (hashCode * 397) ^ LineDashStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ LineColor.GetHashCode();
                hashCode = (hashCode * 397) ^ (Caption != null ? Caption.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)SymbolType;
                hashCode = (hashCode * 397) ^ SymbolColor.GetHashCode();
                return hashCode;
            }
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
