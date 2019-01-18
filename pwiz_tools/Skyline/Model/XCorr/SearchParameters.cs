namespace pwiz.Skyline.Model.XCorr
{
    public class SearchParameters
    {
        public FragmentationType FragmentationType { get; private set; }
        /// <summary>
        /// Use Neutral Losses for XCorr
        /// </summary>
        public bool UseNLsForXCorr { get; private set; }
        public MassTolerance FragmentTolerance { get; private set; }
    }
}
