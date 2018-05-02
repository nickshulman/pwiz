using JetBrains.Annotations;

namespace TopographTool.Model.DataRows
{
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    [UsedImplicitly]
    public class TransitionResultRow
    {
        public string PrecursorLocator { get; private set; }
        public string TransitionLocator { get; private set; }
        public string PeakIntensities { get; private set; }
        public string PeakScanIndexes { get; private set; }
        public string ResultFileLocator { get; private set; }
        public double Area { get; private set; }
        public double StartTime { get; private set; }
        public double EndTime { get; private set; }
        public bool Truncated { get; private set; }
    }
}
