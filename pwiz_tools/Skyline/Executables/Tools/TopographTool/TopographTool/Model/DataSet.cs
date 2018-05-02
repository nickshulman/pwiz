using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace TopographTool.Model
{
    public class DataSet
    {
        public DataSet(Settings settings, Peptide peptide)
        {
            Settings = settings;
            Peptide = peptide;
            Replicates = ImmutableList.ValueOf(Transitions.SelectMany(t => t.Transition.Results.Select(r => r.ResultFile)).Distinct()
                .OrderBy(replicate => Tuple.Create(replicate.Replicate.TimePoint, replicate.Replicate.Cohort, replicate.Replicate.Name)));
            IsolationWindows = ImmutableList.ValueOf(Replicates
                .SelectMany(r => r.ScanInfos.SelectMany(si => si.IsolationWindows)).Distinct().OrderBy(si => si.Start));
            TransitionsByFeature = Transitions.SelectMany(t =>
                    GetFeatures(t).Select(featureContrib => Tuple.Create(t, featureContrib.Item1)))
                .ToLookup(tranFeature => Tuple.Create(tranFeature.Item1.TransitionKey, tranFeature.Item2),
                    tranFeature => tranFeature.Item1);
        }

        public Settings Settings { get; private set; }

        public Peptide Peptide { get; private set; }

        public ImmutableList<ResultFile> Replicates { get; private set; }

        public ImmutableList<IsolationWindow> IsolationWindows { get; private set; }

        public IEnumerable<TransitionData> Transitions
        {
            get
            {
                return Peptide.Precursors.SelectMany(precursor =>
                    precursor.Transitions.Select(t => new TransitionData(Peptide, precursor, t)));
            }
        }

        public ILookup<Tuple<TransitionKey, FeatureKey>, TransitionData> TransitionsByFeature { get; private set; }

        public FeatureWeights GetFeatureWeights()
        {
            var featureWeights = FeatureWeights.EMPTY;
            foreach (var grouping in Transitions.ToLookup(t => t.TransitionKey))
            {
                featureWeights = AddWeights(featureWeights, grouping);
            }
            return featureWeights;
        }

        private FeatureWeights AddWeights(FeatureWeights featureWeights, IGrouping<TransitionKey, TransitionData> grouping)
        {
            var featureKeys = grouping.SelectMany(GetFeatures)
                .Select(tuple=>tuple.Item1).Distinct().ToArray();
            if (featureKeys.Contains(null))
            {
                return featureWeights;
            }
            var conflicts = featureKeys.SelectMany(EnumerateConflicts)
                .Where(c=>!Equals(c.TransitionKey, grouping.Key));
            if (conflicts.Any())
            {
                return featureWeights;
            }

            var byLabelCount = grouping.ToLookup(t => t.LabelCount).OrderBy(g => g.Key).ToArray();
            foreach (var featureKey in featureKeys)
            {
                var labelContribs = new List<KeyValuePair<int, double>>();
                foreach (var labelRow in byLabelCount)
                {
                    double contrib = 0;
                    foreach (var transitionData in labelRow)
                    {
                        foreach (var featureContrib in GetFeatures(transitionData))
                        {
                            if (Equals(featureContrib.Item1, featureKey))
                            {
                                contrib += featureContrib.Item2;
                            }
                        }
                    }
                    if (contrib <= 0)
                    {
                        continue;
                    }
                    labelContribs.Add(new KeyValuePair<int, double>(labelRow.Key, contrib / labelRow.Count()));
                }
                if (labelContribs.Any())
                {
                    featureWeights = featureWeights.AddFeatureWeights(grouping.Key, featureKey, new LabelContribution(labelContribs));
                }
            }
            return featureWeights;
        }

        public IEnumerable<TransitionData> EnumerateConflicts(FeatureKey featureKey)
        {
            return Transitions.Where(t =>
            {
                return GetFeatures(t).Any(fc=>HasConflict(fc.Item1, featureKey));
            });
        }

        public bool HasConflict(FeatureKey featureKey1, FeatureKey featureKey2)
        {
            if (!Equals(featureKey1.Window, featureKey2.Window))
            {
                return false;
            }
            if (Math.Abs(featureKey1.Mz - featureKey2.Mz) <= Settings.ProductMassResolution)
            {
                return true;
            }
            return false;
        }

        public IEnumerable<Tuple<FeatureKey, double>> GetFeatures(TransitionData transitionData)
        {
            if (transitionData.Transition.FragmentIon.StartsWith("precursor"))
            {
                yield return Tuple.Create(new FeatureKey(null, transitionData.Transition.ProductMz), 1.0);
                yield break;
            }
            foreach (var window in IsolationWindows)
            {
                var precursorOverlap = transitionData.Precursor.MzDistribution.Where(kvp => window.Contains(kvp.Key)).ToArray();
                if (precursorOverlap.Length == 0)
                {
                    continue;
                }
                yield return Tuple.Create(new FeatureKey(window, transitionData.Transition.ProductMz),
                    precursorOverlap.Sum(kvp => kvp.Value));
            }
        }
    }
}
