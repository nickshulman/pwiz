using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class FeatureStatisticDictionary
    {
        public static readonly FeatureStatisticDictionary EMPTY =
            new FeatureStatisticDictionary(ChromFileInfoIndex.EMPTY, new Dictionary<ReferenceValue<Peptide>, ImmutableList<PeakFeatureStatistics>>(),
                ScoreQValueMap.EMPTY, false);
        private Dictionary<ReferenceValue<Peptide>, ImmutableList<PeakFeatureStatistics>> _dictionary;

        private FeatureStatisticDictionary(ChromFileInfoIndex chromFileInfoIndex, Dictionary<ReferenceValue<Peptide>, ImmutableList<PeakFeatureStatistics>> dictionary,
            ScoreQValueMap scoreQValueMap, bool missingScores)
        {
            FileIndex = chromFileInfoIndex;
            _dictionary = dictionary;
            ScoreQValueMap = scoreQValueMap;
            MissingScores = missingScores;
        }

        public FeatureStatisticDictionary ReplaceValues(
            IEnumerable<KeyValuePair<Peptide, ImmutableList<PeakFeatureStatistics>>> replacements)
        {
            var dictionary = new Dictionary<ReferenceValue<Peptide>, ImmutableList<PeakFeatureStatistics>>(_dictionary);
            foreach (var entry in replacements)
            {
                dictionary[entry.Key] = entry.Value;
            }
            return new FeatureStatisticDictionary(FileIndex, dictionary, ScoreQValueMap, MissingScores);
        }
        public ChromFileInfoIndex FileIndex { get; }
        public ScoreQValueMap ScoreQValueMap { get; }
        public bool MissingScores { get; }

        public PeakFeatureStatistics GetPeakFeatureStatistics(PeakTransitionGroupIdKey key)
        {
            TryGetValue(key, out var result);
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
            result = default;
            int fileIndex = FileIndex.IndexOf(key.FileId);
            if (fileIndex < 0)
            {
                return false;
            }

            if (!_dictionary.TryGetValue(key.Peptide, out var list))
            {
                return false;
            }

            result = list[fileIndex];
            return true;
        }
        public int Count
        {
            get { return _dictionary.Count; }
        }

        public static FeatureStatisticDictionary MakeFeatureDictionary(ChromFileInfoIndex chromFileIndex, PeakScoringModelSpec ScoringModel,
            PeakTransitionGroupFeatureSet features, bool releaseRawFeatures)
        {
            var bestTargetPvalues = new List<double>(features.TargetCount);
            var bestTargetScores = new List<double>(features.TargetCount);
            var targetIds = new List<PeakTransitionGroupIdKey>(features.TargetCount);
            var featureDictionary = new Dictionary<ReferenceValue<Peptide>, PeakFeatureStatistics[]>();
            foreach (var grouping in features.Features.GroupBy(feature => ReferenceValue.Of(feature.Key.Peptide)))
            {
                var array = new PeakFeatureStatistics[chromFileIndex.Count];
                foreach (var transitionGroupFeatures in grouping)
                {
                    int fileIndex = chromFileIndex.IndexOf(transitionGroupFeatures.Key.FileId);
                    if (fileIndex < 0)
                    {
                        continue;
                    }
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
                    array[fileIndex] = featureStats;
                    if (!transitionGroupFeatures.IsDecoy)
                    {
                        bestTargetPvalues.Add(bestPvalue);
                        bestTargetScores.Add(bestScore);
                        targetIds.Add(transitionGroupFeatures.Key);
                    }
                }
                featureDictionary.Add(grouping.Key, array);
            }

            var qValues = new Statistics(bestTargetPvalues).Qvalues(MProphetPeakScoringModel.DEFAULT_R_LAMBDA,
                MProphetPeakScoringModel.PI_ZERO_MIN);
            for (int i = 0; i < qValues.Length; ++i)
            {
                var targetid = targetIds[i];
                var fileIndex = chromFileIndex.IndexOf(targetid.FileId);
                var array = featureDictionary[targetid.Peptide];
                array[fileIndex] = array[fileIndex].ChangeQValue((float)qValues[i]);
            }

            bool missingValues = qValues.Any(double.IsNaN);

            return new FeatureStatisticDictionary(chromFileIndex, featureDictionary.ToDictionary(kvp=>kvp.Key, kvp=>ImmutableList.ValueOf(kvp.Value)),
                ScoreQValueMap.FromScoreQValues(bestTargetScores, qValues), missingValues);
        }

        private T[] MakeArray<T>(IEnumerable<KeyValuePair<ReferenceValue<ChromFileInfoId>, T>> items)
        {
            var result = new T[FileIndex.Count];
            foreach (var item in items)
            {
                int index = FileIndex.IndexOf(item.Key);
                if (index >= 0)
                {
                    result[index] = item.Value;
                }
            }

            return result;
        }
    }
}
