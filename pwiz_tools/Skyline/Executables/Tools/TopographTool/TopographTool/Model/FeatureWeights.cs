using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class FeatureWeights : Immutable
    {
        public static readonly FeatureWeights EMPTY = new FeatureWeights
        {
            TransitionKeys = ImmutableList<TransitionKey>.EMPTY,
            FeatureKeys = ImmutableList<FeatureKey>.EMPTY,
            LabelContribs = ImmutableList<LabelContribution>.EMPTY
        };
        private FeatureWeights()
        {
        }
        public int RowCount { get { return TransitionKeys.Count;} }
        public ImmutableList<TransitionKey> TransitionKeys { get; private set; }
        public ImmutableList<FeatureKey> FeatureKeys { get; private set; }
        public ImmutableList<LabelContribution> LabelContribs { get; private set; }
        public FeatureWeights AddFeatureWeights(TransitionKey transitionKey, FeatureKey featureKey,
            LabelContribution labelContribs)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.TransitionKeys = ImmutableList.ValueOf(TransitionKeys.Concat(new[] {transitionKey}));
                im.FeatureKeys = ImmutableList.ValueOf(FeatureKeys.Concat(new[] {featureKey}));
                im.LabelContribs = ImmutableList.ValueOf(LabelContribs.Concat(new[]
                {
                    labelContribs
                }));
            });
        }

        public FeatureWeights Filter(IEnumerable<TransitionKey> transitionKeys)
        {
            var transitonKeys = new HashSet<TransitionKey>(transitionKeys);
            var indexes = Enumerable.Range(0, TransitionKeys.Count)
                .Where(i => transitonKeys.Contains(TransitionKeys[i])).ToArray();
            return new FeatureWeights
            {
                TransitionKeys = ImmutableList.ValueOf(indexes.Select(i => TransitionKeys[i])),
                FeatureKeys = ImmutableList.ValueOf(indexes.Select(i => FeatureKeys[i])),
                LabelContribs = ImmutableList.ValueOf(indexes.Select(i => LabelContribs[i]))
            };
        }
    }   
}
