using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveFormat
    {
        public Color SymbolColor { get; set; } = Color.Black;
        [DefaultValue(SymbolType.Default)] public SymbolType SymbolType { get; set; } = SymbolType.Default;

        [DefaultValue(7)] public float SymbolSize { get; set; } = 7;
 
        public Color LineColor { get; set; } = Color.Black;
        public DashStyle? LineDashStyle { get; set; }
        [DefaultValue(1)]
        public float LineWidth { get; set; } = 1;

        public CurveFormat Clone()
        {
            return (CurveFormat)MemberwiseClone();
        }

        public override string ToString()
        {
            return string.Format("Symbol: {0} {1} {2}pt Line: {3} {4} {5}pt", SymbolType, SymbolColor, SymbolSize,
                LineDashStyle, LineColor, LineWidth);
        }
    }
}
