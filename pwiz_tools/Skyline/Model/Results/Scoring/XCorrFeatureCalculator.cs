using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class XCorrFeatureCalculator : DetailedPeakFeatureCalculator
    {
        public XCorrFeatureCalculator() : base(@"XCorr")
        {

        }

        public override string Name {
            get { return "XCorr"; }
    }
        public override bool IsReversedScore
        {
            get { return false; }
        }
        protected override float Calculate(PeakScoringContext context, IPeptidePeakData<IDetailedPeakData> summaryPeakData)
        {
            float max = 0;
            foreach (var transitionGroupPeakData in summaryPeakData.TransitionGroupPeakData)
            {
                var data = transitionGroupPeakData as ITransitionGroupDetailData;
                if (data == null || data.XCorrChromatogram == null)
                {
                    continue;
                }

                var xCorrChromatogram = data.XCorrChromatogram;
                var firstData = summaryPeakData.TransitionGroupPeakData.First();
                var firstPeak = firstData.TransitionPeakData.First().PeakData;
                var firstTime = firstPeak.Times[firstPeak.StartIndex];
                var lastTime = firstPeak.Times[firstPeak.EndIndex];
                int i = CollectionUtil.BinarySearch(xCorrChromatogram.Times, firstTime);
                if (i < 0)
                {
                    i = ~i;
                }

                for (; i < xCorrChromatogram.NumPoints && xCorrChromatogram.Times[i] <= lastTime; i++)
                {
                    var xCorr = xCorrChromatogram.Intensities[i];
                    max = Math.Max(xCorr, max);
                }
            }

            return max;
        }
    }
}
