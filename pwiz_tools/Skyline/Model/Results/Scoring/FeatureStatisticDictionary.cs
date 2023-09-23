using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class FeatureStatisticDictionary
    {
        public static readonly FeatureStatisticDictionary EMPTY =
            new FeatureStatisticDictionary(new Dictionary<PeakTransitionGroupIdKey, PeakFeatureStatistics>(),
                ScoreQValueMap.EMPTY);
        private Dictionary<PeakTransitionGroupIdKey, PeakFeatureStatistics> _dictionary;

        public FeatureStatisticDictionary(Dictionary<PeakTransitionGroupIdKey, PeakFeatureStatistics> dictionary,
            ScoreQValueMap scoreQValueMap)
        {
            _dictionary = dictionary;
            ScoreQValueMap = scoreQValueMap;
        }

        public FeatureStatisticDictionary ReplaceValues(
            IEnumerable<KeyValuePair<PeakTransitionGroupIdKey, PeakFeatureStatistics>> replacements)
        {
            var dictionary = new Dictionary<PeakTransitionGroupIdKey, PeakFeatureStatistics>(_dictionary);
            foreach (var entry in replacements)
            {
                dictionary[entry.Key] = entry.Value;
            }

            return new FeatureStatisticDictionary(dictionary, ScoreQValueMap);

        }
        public ScoreQValueMap ScoreQValueMap { get; }

        public PeakFeatureStatistics GetPeakFeatureStatistics(PeakTransitionGroupIdKey key)
        {
            _dictionary.TryGetValue(key, out var result);
            return result;
        }

        public PeakFeatureStatistics this[PeakTransitionGroupIdKey key]
        {
            get
            {
                return GetPeakFeatureStatistics(key);
            }
        }

        public bool TryGetValue(PeakTransitionGroupIdKey key, out PeakFeatureStatistics result)
        {
            return _dictionary.TryGetValue(key, out result);
        }
        public int Count
        {
            get { return _dictionary.Count; }
        }

        public static FeatureStatisticDictionary MakeFeatureDictionary(PeakScoringModelSpec ScoringModel,
            PeakTransitionGroupFeatureSet _features, bool releaseRawFeatures)
        {
            var bestTargetPvalues = new List<double>(_features.TargetCount);
            var bestTargetScores = new List<double>(_features.TargetCount);
            var targetIds = new List<PeakTransitionGroupIdKey>(_features.TargetCount);
            var featureDictionary = new Dictionary<PeakTransitionGroupIdKey, PeakFeatureStatistics>();
            foreach (var transitionGroupFeatures in _features.Features)
            {
                int bestIndex = 0;
                float bestScore = float.MinValue;
                float bestPvalue = float.NaN;
                var peakGroupFeatures = transitionGroupFeatures.PeakGroupFeatures;
                IList<float> mProphetScoresGroup = null, pvalues = null;
                if (!releaseRawFeatures)
                    mProphetScoresGroup = new float[peakGroupFeatures.Count];
                if (!releaseRawFeatures)
                    pvalues = new float[peakGroupFeatures.Count];

                for (int i = 0; i < peakGroupFeatures.Count; i++)
                {
                    double score = ScoringModel.Score(peakGroupFeatures[i].Features);
                    if (double.IsNaN(bestScore) || score > bestScore)
                    {
                        bestIndex = i;
                        bestScore = (float)score;
                        bestPvalue = (float)(1 - Statistics.PNorm(score));
                    }

                    if (mProphetScoresGroup != null)
                        mProphetScoresGroup[i] = (float)score;
                    if (pvalues != null)
                        pvalues[i] = (float)(1 - Statistics.PNorm(score));
                }

                if (bestScore == float.MinValue)
                    bestScore = float.NaN;

                var featureStats = new PeakFeatureStatistics(transitionGroupFeatures,
                    mProphetScoresGroup, pvalues, bestIndex, bestScore, null);
                featureDictionary.Add(transitionGroupFeatures.Key, featureStats);
                if (!transitionGroupFeatures.IsDecoy)
                {
                    bestTargetPvalues.Add(bestPvalue);
                    bestTargetScores.Add(bestScore);
                    targetIds.Add(transitionGroupFeatures.Key);
                }
            }

            var qValues = new Statistics(bestTargetPvalues).Qvalues(MProphetPeakScoringModel.DEFAULT_R_LAMBDA,
                MProphetPeakScoringModel.PI_ZERO_MIN);
            for (int i = 0; i < qValues.Length; ++i)
            {
                featureDictionary[targetIds[i]] = featureDictionary[targetIds[i]].ChangeQValue((float)qValues[i]);
            }

            return new FeatureStatisticDictionary(featureDictionary,
                ScoreQValueMap.FromScoreQValues(bestTargetPvalues, qValues));
        }
    }
}
