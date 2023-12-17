using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Controls.Alignment
{
    public class RunAlignmentProperties : UserInterfaceObject
    {
        private CurveSettings _curveSettings;
        public RunAlignmentProperties(IDocumentContainer documentContainer) : base(documentContainer)
        {
            _curveSettings = CurveSettings.Default;
        }
        [Category("1_Data")]
        public RetentionTimeSource XAxis { get; set; }

        [Category("1_Data")]
        public RetentionTimeSource YAxis
        {
            get { return CurveSettings.YAxis; }
            set { CurveSettings = CurveSettings.ChangeYAxis(value); }
        }

        [Category("2_Regression")]
        public RegressionMethodRT? RegressionMethod
        {
            get { return CurveSettings.RegressionMethod; }
            set { CurveSettings = CurveSettings.ChangeRegressionMethod(value); }
        }

        [Category("2_Regression")]
        public DashStyle? LineDashStyle
        {
            get { return CurveSettings.LineDashStyle; }
            set { CurveSettings = CurveSettings.ChangeLineDashStyle(value); }
        }

        [Category("2_Regression")]
        public Color LineColor
        {
            get { return CurveSettings.LineColor; }
            set { CurveSettings = CurveSettings.ChangeLineColor(value); }
        }

        [Category("3_Formatting")]
        public string Caption
        {
            get { return CurveSettings.Caption; }
            set { CurveSettings = CurveSettings.ChangeCaption(value); }
        }

        [Category("3_Formatting")]
        public SymbolType SymbolType
        {
            get { return CurveSettings.SymbolType; }
            set { CurveSettings = CurveSettings.ChangeSymbolType(value); }
        }

        [Category("3_Formatting")]
        public Color SymbolColor
        {
            get { return CurveSettings.SymbolColor; }
            set { CurveSettings = CurveSettings.ChangeSymbolColor(value); }
        }

        [Browsable(false)] 
        public CurveSettings CurveSettings {
            get
            {
                return _curveSettings;
            }
            set
            {
                Assume.IsNotNull(value);
                _curveSettings = value;
            }
        }
    }
}
