using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using MathNet.Numerics.Statistics;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
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

            var sourceTimes = GetRetentionTimes(document, parameter.AlignmentValueType, parameter.Source);
            var targetTimes = GetRetentionTimes(document, parameter.AlignmentValueType, parameter.Target);
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

            switch (parameter.RegressionMethod)
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

        private Tuple<int, ChromFileInfoId> FindReplicateIndex(MeasuredResults measuredResults, MsDataFileUri msDataFileUri)
        {
            for (int i = 0; i < measuredResults?.Chromatograms.Count; i++)
            {
                var chromFileInfoId = measuredResults.Chromatograms[i].FindFile(msDataFileUri);
                if (chromFileInfoId != null)
                {
                    return Tuple.Create(i, chromFileInfoId);
                }
            }

            return Tuple.Create(-1, (ChromFileInfoId) null);
        }

        private Dictionary<object, double> GetRetentionTimes(SrmDocument document, AlignmentValueType alignmentValueType, MsDataFileUri filePath)
        {
            if (AlignmentValueType.PEAK_APEXES.Equals(alignmentValueType))
            {
                return ToObjectDictionary(GetPeakApexes(document, filePath));
            }

            return ToObjectDictionary(GetPsmTimes(document, filePath));
        }

        private IEnumerable<KeyValuePair<PeptideModKey, double>> GetPeakApexes(SrmDocument document, MsDataFileUri msDataFileUri)
        {
            var (replicateIndex, chromFileInfoId) = FindReplicateIndex(document.MeasuredResults, msDataFileUri);
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
                    yield return new KeyValuePair<PeptideModKey, double>(peptideGroup.Key, times.Mean());
                }
            }
        }

        private IEnumerable<KeyValuePair<Target, double>> GetPsmTimes(SrmDocument document, MsDataFileUri msDataFileUri)
        {
            return (IEnumerable<KeyValuePair<Target, double>>)
                   document.Settings.GetRetentionTimes(msDataFileUri)?.GetFirstRetentionTimes()
                   ?? Array.Empty<KeyValuePair<Target, double>>();
        }

        private static Dictionary<object, double> ToObjectDictionary<TKey>(
            IEnumerable<KeyValuePair<TKey, double>> entries)
        {
            return entries.ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);
        }

        public class Parameter
        {
            public Parameter(AlignmentValueType alignmentValueType, RegressionMethodRT regressionMethod, SrmDocument document, MsDataFileUri source, MsDataFileUri target)
            {
                AlignmentValueType = alignmentValueType;
                RegressionMethod = regressionMethod;
                Document = document;
                Source = source;
                Target = target;
            }
            public AlignmentValueType AlignmentValueType { get; }
            public RegressionMethodRT RegressionMethod { get; }

            public SrmDocument Document { get; }
            public MsDataFileUri Source { get; }
            public MsDataFileUri Target { get; }

            protected bool Equals(Parameter other)
            {
                return Equals(AlignmentValueType, other.AlignmentValueType) && RegressionMethod == other.RegressionMethod && ReferenceEquals(Document, other.Document) &&
                       Equals(Source, other.Source) && Equals(Target, other.Target);
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
                    hashCode = (hashCode * 397) ^ (AlignmentValueType?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ RegressionMethod.GetHashCode();
                    hashCode = (hashCode * 397) ^ Source.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Target?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }
        }
    }
}
