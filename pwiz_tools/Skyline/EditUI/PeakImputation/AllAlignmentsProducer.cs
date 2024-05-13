using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;

namespace pwiz.Skyline.EditUI.PeakImputation
{

    public class AllAlignments
    {
        public static readonly Producer<Parameter, AllAlignments> PRODUCER =
            new AllAlignmentsProducer();

        private Dictionary<MsDataFileUri, AlignmentFunction> _alignmentFunctions;
        private Dictionary<ReferenceValue<ChromFileInfoId>, MsDataFileUri> _fileUris;

        public AllAlignments(SrmDocument document, AlignmentTarget target, 
            IEnumerable<KeyValuePair<MsDataFileUri, AlignmentFunction>> alignmentFunctions)
        {
            Document = document;
            Target = target;
            _alignmentFunctions = alignmentFunctions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _fileUris = new Dictionary<ReferenceValue<ChromFileInfoId>, MsDataFileUri>();
            var measuredResults = document.MeasuredResults;
            if (measuredResults != null)
            {
                foreach (var chromatogramSet in measuredResults.Chromatograms)
                {
                    foreach (ChromFileInfo fileInfo in chromatogramSet.MSDataFileInfos)
                    {
                        _fileUris[fileInfo.FileId] = fileInfo.FilePath;
                    }
                }
            }
        }



        public SrmDocument Document { get; }
        public AlignmentTarget Target { get; }

        public AlignmentFunction GetAlignmentFunction(MsDataFileUri msDataFileUri)
        {
            if (Target == null)
            {
                return AlignmentFunction.IDENTITY;
            }
            _alignmentFunctions.TryGetValue(msDataFileUri, out var alignmentFunction);
            return alignmentFunction;
        }

        public AlignmentFunction GetAlignmentFunction(ChromFileInfoId fileId)
        {
            if (Target == null)
            {
                return AlignmentFunction.IDENTITY;
            }

            if (!_fileUris.TryGetValue(fileId, out var fileUri))
            {
                return null;
            }

            return GetAlignmentFunction(fileUri);
        }
        public class Parameter
        {
            public Parameter(SrmDocument document, AlignmentTarget alignment)
            {
                Document = document;
                Target = alignment;
            }

            public SrmDocument Document { get; }
            public AlignmentTarget Target { get; }

            protected bool Equals(Parameter other)
            {
                return ReferenceEquals(Document, other.Document) && Equals(Target, other.Target);
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
                    return (RuntimeHelpers.GetHashCode(Document) * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                }
            }
        }
        private  class AllAlignmentsProducer : Producer<Parameter, AllAlignments>
        {
            public override IEnumerable<WorkOrder> GetInputs(Parameter parameter)
            {
                SrmDocument document = parameter.Document;
                if (document.MeasuredResults == null)
                {
                    yield break;
                }

                if (parameter.Target != null)
                {
                    foreach (var msDataFileUri in document.MeasuredResults.MSDataFilePaths)
                    {
                        yield return AlignmentProducer.Instance.MakeWorkOrder(
                            new AlignmentProducer.Parameter(parameter.Target, document, msDataFileUri));
                    }
                }
            }



            public override AllAlignments ProduceResult(ProductionMonitor productionMonitor, Parameter parameter, IDictionary<WorkOrder, object> inputs)
            {
                var alignments = new List<KeyValuePair<MsDataFileUri, AlignmentFunction>>();
                foreach (var input in inputs)
                {
                    if (input.Key.Producer == AlignmentProducer.Instance)
                    {
                        var workParameter = (AlignmentProducer.Parameter)input.Key.WorkParameter;
                        var alignmentFunction = (AlignmentFunction)input.Value;
                        if (alignmentFunction != null)
                        {
                            alignments.Add(new KeyValuePair<MsDataFileUri, AlignmentFunction>(workParameter.Source, alignmentFunction));
                        }
                    }
                }

                return new AllAlignments(parameter.Document, parameter.Target, alignments);
            }

        }
    }
}
