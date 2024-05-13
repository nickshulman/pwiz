using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.PeakImputation
{
    public class AlignedPeak : Immutable
    {
        public AlignedPeak(ResultFileInfo resultFileInfo, ApexPeakBounds rawPeakBounds, double? score, bool manuallyIntegrated)
        {
            ResultFileInfo = resultFileInfo;
            RawPeakBounds = rawPeakBounds;
            AlignedPeakBounds = rawPeakBounds?.Align(resultFileInfo.AlignmentFunction);
            ManuallyIntegrated = manuallyIntegrated;
            Score = score;
        }

        public ResultFileInfo ResultFileInfo { get; }
        public ApexPeakBounds RawPeakBounds { get; }

        public ApexPeakBounds AlignedPeakBounds { get; private set; }

        public double? Score { get; }
        public bool ManuallyIntegrated { get; }
        public double? Percentile { get; private set; }

        public AlignedPeak ChangePercentile(double? value)
        {
            return ChangeProp(ImClone(this), im => im.Percentile = value);
        }

        public double? PValue { get; private set; }

        public AlignedPeak ChangePValue(double? value)
        {
            return ChangeProp(ImClone(this), im => im.PValue = value);
        }

        public double? QValue { get; private set; }

        public AlignedPeak ChangeQValue(double? value)
        {
            return ChangeProp(ImClone(this), im => im.QValue = value);
        }
    }
}