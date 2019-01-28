using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.XCorr
{
    public class SearchParameters : Immutable
    {
        public static readonly SearchParameters DEFAULT = new SearchParameters()
        {
            FragmentationType = FragmentationType.CID,
            UseNLsForXCorr = false,
            FragmentTolerance = MassTolerance.WithPpm(10)
        };


        public FragmentationType FragmentationType { get; private set; }
        /// <summary>
        /// Use Neutral Losses for XCorr
        /// </summary>
        public bool UseNLsForXCorr { get; private set; }
        public MassTolerance FragmentTolerance { get; private set; }

        public SearchParameters ChangeFragmentTolerance(MassTolerance fragmentTolerance)
        {
            return ChangeProp(ImClone(this), im => im.FragmentTolerance = fragmentTolerance);
        }

        public SearchParameters ChangeFragmentationType(FragmentationType fragmentationType)
        {
            return ChangeProp(ImClone(this), im => im.FragmentationType = fragmentationType);
        }
    }
}
