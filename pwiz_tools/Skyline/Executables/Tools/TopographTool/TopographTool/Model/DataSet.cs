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
            Replicates = ImmutableList.ValueOf(Transitions.SelectMany(t => t.Transition.Results.Select(r => r.Replicate)).Distinct()
                .OrderBy(replicate => Tuple.Create(replicate.TimePoint, replicate.Cohort, replicate.Name)));
            TransitionsByFeature = Transitions.ToLookup(t => Tuple.Create(t.TransitionKey, GetFeature(t)));
        }

        public Settings Settings { get; private set; }

        public Peptide Peptide { get; private set; }

        public ImmutableList<Replicate> Replicates { get; private set; }

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
            var featureKeys = grouping.Select(GetFeature).Distinct().ToArray();
            if (featureKeys.Contains(null))
            {
                return featureWeights;
            }
            var conflicts = featureKeys.SelectMany(EnumerateConflicts).Where(c=>!Equals(c.TransitionKey, grouping.Key));
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
                    int contrib = labelRow.Count(transitionData => Equals(featureKey, GetFeature(transitionData)));
                    if (contrib == 0)
                    {
                        continue;
                    }
                    labelContribs.Add(new KeyValuePair<int, double>(labelRow.Key, (double) contrib / labelRow.Count()));
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
                var f = GetFeature(t);
                return f != null && HasConflict(featureKey, f);
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

        public FeatureKey GetFeature(TransitionData transitionData)
        {
            if (transitionData.Transition.FragmentIon.StartsWith("precursor"))
            {
                return new FeatureKey(null, transitionData.Transition.ProductMz);
            }
            var windows = Settings.IsolationScheme.GetWindows(transitionData.Precursor.PrecursorMz).ToArray();
            if (windows.Length != 1)
            {
                return null;
            }
            return new FeatureKey(windows[0], transitionData.Transition.ProductMz);
        }
    }
}
