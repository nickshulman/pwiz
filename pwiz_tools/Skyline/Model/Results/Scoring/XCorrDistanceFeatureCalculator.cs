using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class XCorrDistanceFeatureCalculator : DetailedPeakFeatureCalculator
    {
        public XCorrDistanceFeatureCalculator() : base("xcorr_dist")
        {

        }

        public override string Name
        {
            get { return "XCorr Distance"; }
        }
        public override bool IsReversedScore
        {
            get { return true; }
        }

        protected override float Calculate(PeakScoringContext context, IPeptidePeakData<IDetailedPeakData> summaryPeakData)
        {
            float max = 0;
            float maxTime = 0;
            TimeIntensities maxChromatogram = null;
            foreach (var transitionGroupPeakData in summaryPeakData.TransitionGroupPeakData)
            {
                var data = transitionGroupPeakData as ITransitionGroupDetailData;
                if (data == null || data.XCorrChromatogram == null)
                {
                    continue;
                }

                var xCorrChromatogram = data.XCorrChromatogram;
                for (int i = 0; i < xCorrChromatogram.NumPoints; i++)
                {
                    if (xCorrChromatogram.Intensities[i] > max)
                    {
                        maxTime = xCorrChromatogram.Times[i];
                        maxChromatogram = xCorrChromatogram;
                    }
                }
            }

            if (maxChromatogram == null)
            {
                return 0;
            }
            var firstData = summaryPeakData.TransitionGroupPeakData.First();
            var firstPeak = firstData.TransitionPeakData.First().PeakData;
            var firstTime = firstPeak.Times[firstPeak.StartIndex];
            var lastTime = firstPeak.Times[firstPeak.EndIndex];
            if (firstTime <= maxTime && lastTime >= maxTime)
            {
                return 0;
            }

            return Math.Min(Math.Abs(firstTime - maxTime), Math.Abs(lastTime - maxTime));
        }

    }
}
