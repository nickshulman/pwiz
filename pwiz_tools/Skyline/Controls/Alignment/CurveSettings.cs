using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.RetentionTimes;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveSettings : UserInterfaceObject
    {
        public CurveSettings(IDocumentContainer documentContainer) : base(documentContainer)
        {
            LineColor = Color.Black;
            SymbolColor = Color.Black;
        }
        [Category("1_Data")]
        public RetentionTimeSource XAxis { get; set; }
        [Category("1_Data")]
        public RetentionTimeSource YAxis { get; set; }
        [Category("2_Regression")]
        public RegressionMethodRT? RegressionMethod { get; set; }
        [Category("2_Regression")]
        public DashStyle? LineDashStyle { get; set; }
        [Category("2_Regression")]
        public Color LineColor { get; set; }

        [Category("3_Formatting")]
        public string Caption { get; set; }
        [Category("3_Formatting")]
        public SymbolType SymbolType { get; set; }
        [Category("3_Formatting")]
        public Color SymbolColor { get; set; }
    }
}
