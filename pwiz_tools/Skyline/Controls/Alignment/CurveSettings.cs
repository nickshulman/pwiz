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
    }
}
