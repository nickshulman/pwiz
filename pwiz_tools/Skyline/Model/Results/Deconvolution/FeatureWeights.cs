using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class FeatureWeights : Immutable
    {
        public FeatureWeights(IEnumerable<PrecursorClass> precursorClasses)
        {
            PrecursorClasses = ImmutableList.ValueOf(precursorClasses);
            TransitionKeys = ImmutableList<PeptideDocNode.TransitionKey>.EMPTY;
            FeatureKeys = ImmutableList<FeatureKey>.EMPTY;
            PrecursorContributions = ImmutableList<ImmutableList<double>>.EMPTY;
        }

        public ImmutableList<PrecursorClass> PrecursorClasses { get; private set; }
        public ImmutableList<PeptideDocNode.TransitionKey> TransitionKeys { get; private set; }
        public ImmutableList<FeatureKey> FeatureKeys { get; private set; }
        public ImmutableList<ImmutableList<double>> PrecursorContributions { get; private set; }

        public FeatureWeights AddFeatureWeights(PeptideDocNode.TransitionKey transitionKey, FeatureKey featureKey,
            IEnumerable<double> contributions)
        {
            var lstContributions = ImmutableList.ValueOf(contributions);
            if (lstContributions.Count != PrecursorClasses.Count)
            {
                throw new ArgumentException();
            }
            return ChangeProp(ImClone(this), im =>
            {
                im.TransitionKeys = ImmutableList.ValueOf(TransitionKeys.Concat(new[] {transitionKey}));
                im.FeatureKeys = ImmutableList.ValueOf(FeatureKeys.Concat(new[] {featureKey}));
                im.PrecursorContributions = ImmutableList.ValueOf(PrecursorContributions.Concat(new[]{lstContributions}));
            });
        }
    }
}
