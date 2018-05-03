namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class TransitionFeatureWeight
    {
        public TransitionFeatureWeight(
            PrecursorClass precursorClass,
            PeptideDocNode.TransitionKey transitionKey,
            TransitionGroupDocNode transitionGroup, 
            TransitionDocNode transition,
            FeatureKey featureKey, 
            double weight)
        {
            PrecursorClass = precursorClass;
            TransitionGroup = transitionGroup;
            Transition = transition;
            TransitionKey = transitionKey;
            FeatureKey = featureKey;
            Weight = weight;
        }

        public PrecursorClass PrecursorClass { get; private set; }
        public PeptideDocNode.TransitionKey TransitionKey { get; private set; }
        public TransitionGroupDocNode TransitionGroup { get; private set; }
        public TransitionDocNode Transition { get; private set; }
        public FeatureKey FeatureKey { get; private set; }
        public double Weight { get; private set; }
    }
}
