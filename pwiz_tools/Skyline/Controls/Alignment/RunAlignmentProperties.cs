using System.ComponentModel;
using pwiz.Skyline.Model;

namespace pwiz.Skyline.Controls.Alignment
{
    public class RunAlignmentProperties : UserInterfaceObject
    {
        public RunAlignmentProperties(IDocumentContainer documentContainer) : base(documentContainer)
        {
            CurveFormat = new CurveFormat();
            RegressionOptions = new RegressionOptions();
        }


        public RetentionTimeSource XAxis { get; set; }

        public RetentionTimeSource YAxis
        {
            get; set;
        }
        public string Caption
        {
            get;
            set;
        }

        [ReadOnly(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RegressionOptions RegressionOptions { get; set; }


        [Browsable(false)]
        public CurveSettings CurveSettings {
            get
            {
                return CurveSettings.Default.ChangeCurveFormat(CurveFormat).ChangeCaption(Caption)
                    .ChangeRegressionOptions(RegressionOptions).ChangeYAxis(YAxis);
            }
            set
            {
                CurveFormat = value.CurveFormat;
                Caption = value.Caption;
                RegressionOptions = value.RegressionOptions;
                YAxis = value.YAxis;
            }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [ReadOnly(true)]
        public CurveFormat CurveFormat 
        {
            get;
            set;
        }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [ReadOnly(true)]
        public CurveResult Result { get; set; }

        public override PropertyDescriptor GetDefaultProperty()
        {
            return FindProperty(nameof(XAxis));
        }

        
    }
}
