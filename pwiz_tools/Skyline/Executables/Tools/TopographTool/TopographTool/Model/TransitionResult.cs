using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class TransitionResult : Immutable
    {
        public TransitionResult(Replicate replicate, double area, bool truncated)
        {
            Replicate = replicate;
            Area = area;
            Truncated = truncated;
        }

        public Replicate Replicate { get; private set; }
        public double Area { get; private set; }
        public bool Truncated { get; private set; }
    }
}
