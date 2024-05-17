using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.RetentionTimes
{
    public class AlignmentProducer : Producer<AlignmentProducer.Parameter, AlignmentFunction>
    {
        public static AlignmentProducer Instance = new AlignmentProducer();
        public override AlignmentFunction ProduceResult(ProductionMonitor productionMonitor, Parameter parameter, IDictionary<WorkOrder, object> inputs)
        {
            var document = parameter.Document;
            if (Equals(parameter.Source, parameter.Target.File))
            {
                return AlignmentFunction.IDENTITY;
            }

            var targetTimes = inputs.FirstOrDefault().Value as AlignmentTargetTimes;
            var sourceTimes = parameter.Target.GetRetentionTimes(document, parameter.Source)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var xValues = new List<double>();
            var yValues = new List<double>();
            foreach (var entry in sourceTimes)
            {
                if (targetTimes.TryGetValue(entry.Key, out var targetTime))
                {
                    xValues.Add(entry.Value);
                    yValues.Add(targetTime);
                }
            }

            if (xValues.Count < 2)
            {
                Trace.TraceWarning("Unable to perform alignment from {0} to {1}", parameter.Source, parameter.Target);
                return AlignmentFunction.IDENTITY;
            }

            switch (parameter.Target.RegressionMethod)
            {
                case RegressionMethodRT.kde:
                    var kdeAligner = new KdeAligner(-1, -1);
                    kdeAligner.Train(xValues.ToArray(), yValues.ToArray(), productionMonitor.CancellationToken);
                    return AlignmentFunction.Define(kdeAligner.GetValue, kdeAligner.GetValueReversed);
                case RegressionMethodRT.loess:
                    var loessAligner = new LoessAligner(-1, -1);
                    loessAligner.Train(xValues.ToArray(), yValues.ToArray(), productionMonitor.CancellationToken);
                    return AlignmentFunction.Define(loessAligner.GetValue, loessAligner.GetValueReversed);
            }

            var regressionLine = new RegressionLine(xValues.ToArray(), yValues.ToArray());
            return AlignmentFunction.Define(regressionLine.GetX, regressionLine.GetY);
        }

        public override IEnumerable<WorkOrder> GetInputs(Parameter parameter)
        {
            yield return AlignmentTargetTimes.MakeWorkOrder(parameter.Target, parameter.Document);
        }

        public class Parameter
        {
            public Parameter(AlignmentTarget target, SrmDocument document, MsDataFileUri source)
            {
                Target = target;
                Document = document;
                Source = source;
            }
            public AlignmentTarget Target { get; private set; }

            public SrmDocument Document { get; }
            public MsDataFileUri Source { get; }

            protected bool Equals(Parameter other)
            {
                return Equals(Target, other.Target) && ReferenceEquals(Document, other.Document) &&
                       Equals(Source, other.Source);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Parameter)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = RuntimeHelpers.GetHashCode(Document);
                    hashCode = (hashCode * 397) ^ Target.GetHashCode();
                    hashCode = (hashCode * 397) ^ Source.GetHashCode();
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return @$"{nameof(Target)}: {Target}, {nameof(Document)}: {Document}, {nameof(Source)}: {Source}";
            }
        }
    }

    public class AlignmentTargetTimes : IEnumerable<KeyValuePair<object, double>>
    {
        private static Producer<Tuple<AlignmentTarget, ReferenceValue<SrmDocument>>, AlignmentTargetTimes> PRODUCER = new Producer();

        private Dictionary<object, double> _dictionary;

        public AlignmentTargetTimes(IEnumerable<KeyValuePair<object, double>> entries)
        {
            _dictionary = entries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public bool TryGetValue(object key, out double value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public static WorkOrder MakeWorkOrder(AlignmentTarget alignmentTarget, SrmDocument document)
        {
            return PRODUCER.MakeWorkOrder(Tuple.Create(alignmentTarget, ReferenceValue.Of(document)));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<object, double>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        private class Producer : Producer<Tuple<AlignmentTarget, ReferenceValue<SrmDocument>>, AlignmentTargetTimes>
        {
            public override AlignmentTargetTimes ProduceResult(ProductionMonitor productionMonitor, Tuple<AlignmentTarget, ReferenceValue<SrmDocument>> parameter, IDictionary<WorkOrder, object> inputs)
            {
                return new AlignmentTargetTimes(parameter.Item1.GetRetentionTimes(parameter.Item2));
            }
        }
    }
}
