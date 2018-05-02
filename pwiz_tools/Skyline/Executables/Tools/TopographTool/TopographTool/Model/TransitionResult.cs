using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using TopographTool.Model.DataRows;

namespace TopographTool.Model
{
    public class TransitionResult : Immutable
    {
        public TransitionResult(ResultFile resultFile, TransitionResultRow transitionResultRow)
        {
            ResultFile = resultFile;
            Area = transitionResultRow.Area;
            StartTime = transitionResultRow.StartTime;
            EndTime = transitionResultRow.EndTime;
            Truncated = transitionResultRow.Truncated;
            PeakIntensities = ImmutableList.ValueOf(RowReader.ParseDoubles(transitionResultRow.PeakIntensities));
            PeakScanIndexes = ImmutableList.ValueOf(RowReader.ParseIntegers(transitionResultRow.PeakScanIndexes));
        }

        public ResultFile ResultFile { get; private set; }
        public double Area { get; private set; }
        public double StartTime { get; private set; }
        public double EndTime { get; private set; }
        public bool Truncated { get; private set; }
        public ImmutableList<double> PeakIntensities { get; private set; }
        public ImmutableList<int> PeakScanIndexes { get; private set; }
    }
}
