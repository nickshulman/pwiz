using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results.Scoring;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public class ScoringProducer : Producer<ScoringProducer.Parameters, MProphetResultsHandler>
    {
        public static readonly ScoringProducer Instance = new ScoringProducer();
        public override MProphetResultsHandler ProduceResult(ProductionMonitor productionMonitor, Parameters parameter, IDictionary<WorkOrder, object> inputs)
        {
            var featureSet = (PeakTransitionGroupFeatureSet)inputs.First().Value;
            var resultsHandler = new MProphetResultsHandler(parameter.Document, parameter.ScoringModel, featureSet);
            resultsHandler.ScoreFeatures();
            return resultsHandler;
        }

        public override IEnumerable<WorkOrder> GetInputs(Parameters parameter)
        {
            yield return FeatureSetProducer.Instance.MakeWorkOrder(new FeatureSetProducer.Parameters(parameter.Document,
                parameter.ScoringModel.PeakFeatureCalculators));
        }

        public class Parameters
        {
            public Parameters(SrmDocument document, PeakScoringModelSpec scoringModel)
            {
                Document = document;
                ScoringModel = scoringModel;
            }
            public ReferenceValue<SrmDocument> Document { get; }
            public PeakScoringModelSpec ScoringModel { get; }

            protected bool Equals(Parameters other)
            {
                return Document.Equals(other.Document) && Equals(ScoringModel, other.ScoringModel);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Parameters)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Document.GetHashCode() * 397) ^ (ScoringModel != null ? ScoringModel.GetHashCode() : 0);
                }
            }
        }
    }

    public class FeatureSetProducer : Producer<FeatureSetProducer.Parameters, PeakTransitionGroupFeatureSet>
    {
        public static readonly FeatureSetProducer Instance = new FeatureSetProducer();

        public override PeakTransitionGroupFeatureSet ProduceResult(ProductionMonitor productionMonitor, Parameters parameter,
            IDictionary<WorkOrder, object> inputs)
        {
            return parameter.Document.Value.GetPeakFeatures(parameter.FeatureCalculators,
                new SilentProgressMonitor(productionMonitor.CancellationToken));
        }

        public class Parameters
        {
            public Parameters(SrmDocument document, FeatureCalculators featureCalculators)
            {
                Document = document;
                FeatureCalculators = featureCalculators;
            }
            public ReferenceValue<SrmDocument> Document { get; }
            public FeatureCalculators FeatureCalculators { get; }

            protected bool Equals(Parameters other)
            {
                return Document.Equals(other.Document) && Equals(FeatureCalculators, other.FeatureCalculators);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Parameters)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Document.GetHashCode() * 397) ^ (FeatureCalculators != null ? FeatureCalculators.GetHashCode() : 0);
                }
            }
        }
    }
}
