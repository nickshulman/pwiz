using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using pwiz.Common.Collections;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveFormat : DeepCloneable<CurveFormat>
    {
        public Color SymbolColor { get; set; } = Color.Black;
        [DefaultValue(SymbolType.Default)] public SymbolType SymbolType { get; set; } = SymbolType.Default;

        [DefaultValue(7)] public float SymbolSize { get; set; } = 7;
 
        public Color LineColor { get; set; } = Color.Black;
        public DashStyle? LineDashStyle { get; set; }
        [DefaultValue(1)]
        public float LineWidth { get; set; } = 1;

        public override string ToString()
        {
            return string.Format("Symbol: {0} {1} {2}pt Line: {3} {4} {5}pt", SymbolType, SymbolColor, SymbolSize,
                LineDashStyle, LineColor, LineWidth);
        }

        protected bool Equals(CurveFormat other)
        {
            return SymbolColor.Equals(other.SymbolColor) && SymbolType == other.SymbolType &&
                   SymbolSize.Equals(other.SymbolSize) && LineColor.Equals(other.LineColor) &&
                   LineDashStyle == other.LineDashStyle && LineWidth.Equals(other.LineWidth);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CurveFormat)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SymbolColor.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)SymbolType;
                hashCode = (hashCode * 397) ^ SymbolSize.GetHashCode();
                hashCode = (hashCode * 397) ^ LineColor.GetHashCode();
                hashCode = (hashCode * 397) ^ LineDashStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ LineWidth.GetHashCode();
                return hashCode;
            }
        }
    }
}
