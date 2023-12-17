using System.ComponentModel;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.Controls.Alignment
{
    public class RunAlignmentProperties : UserInterfaceObject
    {
        public RunAlignmentProperties(IDocumentContainer documentContainer) : base(documentContainer)
        {
            CurveFormat = new CurveFormat();
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

        public RegressionMethodRT? RegressionMethod
        {
            get;
            set;
        }


        [Browsable(false)]
        public CurveSettings CurveSettings {
            get
            {
                return CurveSettings.Default.ChangeCurveFormat(CurveFormat).ChangeCaption(Caption)
                    .ChangeRegressionMethod(RegressionMethod).ChangeYAxis(YAxis);
            }
            set
            {
                CurveFormat = value.CurveFormat;
                Caption = value.Caption;
                RegressionMethod = value.RegressionMethod;
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
