using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class TransitionChromatogram : AbstractChromatogram
    {
        private readonly ChromatogramInfo _chromatogramInfo;

        public TransitionChromatogram(ChromatogramGroup chromatogramGroup, ChromatogramInfo chromatogramInfo) : base(chromatogramGroup.DataSchema)
        {
            ChromatogramGroup = chromatogramGroup;
            _chromatogramInfo = chromatogramInfo;
        }

        public ChromatogramGroup ChromatogramGroup { get; private set; }

        protected override ChromatogramInfo ChromatogramInfo
        {
            get { return _chromatogramInfo; }
        }

        [Expensive]
        [ChildDisplayName("Raw{0}")]
        public Data RawData
        {
            get
            {
                var timeIntensitiesGroup = ChromatogramGroup.ReadTimeIntensitiesGroup();
                if (timeIntensitiesGroup is RawTimeIntensities)
                {
                    return new Chromatogram.Data(timeIntensitiesGroup.TransitionTimeIntensities[_chromatogramInfo.TransitionIndex]);
                }
                return null;
            }
        }

        [Expensive]
        [ChildDisplayName("Interpolated{0}")]
        public Data InterpolatedData
        {
            get
            {
                var timeIntensitiesGroup = ChromatogramGroup.ReadTimeIntensitiesGroup();
                if (null == timeIntensitiesGroup)
                {
                    return null;
                }
                var rawTimeIntensities = timeIntensitiesGroup as RawTimeIntensities;
                if (null != rawTimeIntensities)
                {
                    var interpolatedTimeIntensities = rawTimeIntensities
                        .TransitionTimeIntensities[_chromatogramInfo.TransitionIndex]
                        .Interpolate(rawTimeIntensities.GetInterpolatedTimes(), rawTimeIntensities.InferZeroes);
                    return new Data(interpolatedTimeIntensities);
                }
                return new Data(timeIntensitiesGroup.TransitionTimeIntensities[_chromatogramInfo.TransitionIndex]);
            }
        }
    }
}
