using pwiz.Skyline.Model.Hibernate;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class FeatureKey
    {
        public FeatureKey(ScanInfo.IsolationWindow isolationWindow, double mz)
        {
            Window = isolationWindow;
            Mz = mz;
        }

        public ScanInfo.IsolationWindow Window { get; private set; }
        public double Mz { get; private set; }

        public override string ToString()
        {
            string str = Mz.ToString(Formats.Mz);
            if (Window != null)
            {
                return Window + "/" + str;
            }
            return str;
        }
    }
}
