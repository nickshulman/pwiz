using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public class AlignmentProducer : Producer<AlignmentProducer.Parameter, AlignmentFunction>
    {
        public static AlignmentProducer Instance = new AlignmentProducer();
        public override AlignmentFunction ProduceResult(ProductionMonitor productionMonitor, Parameter parameter, IDictionary<WorkOrder, object> inputs)
        {
            var document = parameter.Document;
            if (parameter.Target == null || Equals(parameter.Source, parameter.Target))
            {
                return AlignmentFunction.IDENTITY;
            }
            var sourceReplicateFile = FindReplicateIndex(document.MeasuredResults, parameter.Source);
            var targetReplicateFile = FindReplicateIndex(document.MeasuredResults, parameter.Target);
            if (sourceReplicateFile == null || targetReplicateFile == null)
            {
                return null;
            }

            var sourceTimes = GetRetentionTimes(document, sourceReplicateFile.Item1, sourceReplicateFile.Item2);
            var targetTimes = GetRetentionTimes(document, targetReplicateFile.Item1, targetReplicateFile.Item2);
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
            var kdeAligner = new KdeAligner(-1, -1);
            kdeAligner.Train(xValues.ToArray(), yValues.ToArray(), productionMonitor.CancellationToken);
            return AlignmentFunction.Define(kdeAligner.GetValue, kdeAligner.GetValueReversed);
        }

        private Tuple<int, ChromFileInfoId> FindReplicateIndex(MeasuredResults measuredResults, MsDataFileUri msDataFileUri)
        {
            if (measuredResults == null)
            {
                return null;
            }

            for (int i = 0; i < measuredResults.Chromatograms.Count; i++)
            {
                var chromFileInfoId = measuredResults.Chromatograms[i].FindFile(msDataFileUri);
                if (chromFileInfoId != null)
                {
                    return Tuple.Create(i, chromFileInfoId);
                }
            }

            return null;
        }

        private Dictionary<PeptideModKey, double> GetRetentionTimes(SrmDocument document, int replicateIndex, ChromFileInfoId chromFileInfoId)
        {
            var dictionary = new Dictionary<PeptideModKey, double>();
            foreach (var peptideGroup in document.Molecules.GroupBy(peptideDocNode => peptideDocNode.Key))
            {
                var times = new List<double>();
                foreach (var peptideDocNode in peptideGroup)
                {
                    foreach (var peptideChromInfo in peptideDocNode.GetSafeChromInfo(replicateIndex))
                    {
                        if (ReferenceEquals(peptideChromInfo.FileId, chromFileInfoId))
                        {
                            if (peptideChromInfo.RetentionTime.HasValue)
                            {
                                times.Add(peptideChromInfo.RetentionTime.Value);
                            }
                        }
                    }
                }

                if (times.Count > 0)
                {
                    dictionary.Add(peptideGroup.Key, times.Mean());
                }
            }

            return dictionary;
        }

        public class Parameter
        {
            public Parameter(SrmDocument document, MsDataFileUri source, MsDataFileUri target)
            {
                Document = document;
                Source = source;
                Target = target;
            }

            public SrmDocument Document { get; }
            public MsDataFileUri Source { get; }
            public MsDataFileUri Target { get; }

            protected bool Equals(Parameter other)
            {
                return ReferenceEquals(Document, other.Document) && Equals(Source, other.Source) && Equals(Target, other.Target);
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
                    hashCode = (hashCode * 397) ^ Source.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Target?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }
        }
    }
}
