using System;
using System.Collections.Generic;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class DeconvolutedChromatogram : ChromatogramGroupInfo
    {
        public DeconvolutedChromatogram(ChromGroupHeaderInfo groupHeaderInfo,
            IDictionary<Type, int> scoreTypeIndices,
            IList<ChromCachedFile> allFiles,
            IReadOnlyList<ChromTransition> allTransitions,
            IReadOnlyList<ChromPeak> allPeaks,
            IReadOnlyList<float> allScores)
            : base(groupHeaderInfo, scoreTypeIndices, allFiles, allTransitions, allPeaks, allScores)
        {
            
        }

        public override ChromPeak GetTransitionPeak(int transitionIndex, int peakIndex)
        {
            var basePeak = base.GetTransitionPeak(transitionIndex, peakIndex);
            if (Equals(basePeak.StartTime, basePeak.EndTime))
            {
                return basePeak;
            }
            var info = GetTransitionInfo(transitionIndex, TransformChrom.interpolated);
            int startIndex = info.IndexOfNearestTime(basePeak.StartTime);
            int endIndex = info.IndexOfNearestTime(basePeak.EndTime);

            return info.CalcPeak(startIndex, endIndex, basePeak.Flags);
        }
    }
}