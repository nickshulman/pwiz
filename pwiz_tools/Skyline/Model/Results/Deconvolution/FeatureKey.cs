using pwiz.Skyline.Model.Hibernate;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class FeatureKey
    {
        public FeatureKey(IsolationWindow? isolationWindow, double mz)
        {
            Window = isolationWindow;
            Mz = mz;
        }

        public IsolationWindow? Window { get; private set; }
        public double Mz { get; private set; }

        public override string ToString()
        {
            string str = Mz.ToString(Formats.Mz);
            if (Window.HasValue)
            {
                return Window + "/" + str;
            }
            return str;
        }

        public struct IsolationWindow
        {
            public IsolationWindow(double start, double end) : this()
            {
                Start = start;
                End = end;
            }
            public double Start { get; private set; }
            public double End { get; private set; }

            public override string ToString()
            {
                return "[" + Start.ToString("0.0") + "," + End.ToString("0.0") + ")";
            }
        }
    }
}
