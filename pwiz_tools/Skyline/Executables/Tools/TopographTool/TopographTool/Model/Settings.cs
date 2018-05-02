using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class Settings : Immutable
    {
        public static readonly Settings DEFAULT = new Settings
        {
            PrecursorMassResolution = .1,
            ProductMassResolution = .1
        };
        public double PrecursorMassResolution { get; private set; }
        public double ProductMassResolution { get; private set; }
    }
}
