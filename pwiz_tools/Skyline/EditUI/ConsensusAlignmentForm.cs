using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;
using Peptide = pwiz.Skyline.Model.Databinding.Entities.Peptide;
using Statistics = pwiz.Skyline.Util.Statistics;

namespace pwiz.Skyline.EditUI
{
    public partial class ConsensusAlignmentForm : StandardDataboundGridForm
    {
        private Receiver<Parameter, Data> _receiver;
        private BindingList<Row> _bindingList;
        private List<Row> _rows;
        
        public ConsensusAlignmentForm(SkylineWindow skylineWindow) : base(skylineWindow)
        {
            InitializeComponent();
            comboSource.Items.Add(RtValueType.IRT);
            comboSource.Items.Add(RtValueType.PEAK_APEXES);
            comboSource.Items.Add(RtValueType.PSM_TIMES);
            _receiver = DataProducer.INSTANCE.RegisterCustomer(this, OnDataAvailable);
            _rows = new List<Row>();
            _bindingList = new BindingList<Row>();
            var rowSource = BindingListRowSource.Create(_bindingList);
            var viewContext = new SkylineViewContext(ColumnDescriptor.RootColumn(DataSchema, typeof(Row)), rowSource);
            BindingListSource.SetViewContext(viewContext);
        }

        private void OnDataAvailable()
        {
            if (_receiver.TryGetCurrentProduct(out var data))
            {
                Console.Out.WriteLine("OnDataAvailable: Available");
                _rows.Clear();
                _rows.AddRange(GetRows(data));
                _bindingList.ResetBindings();
            }
            else
            {
                Console.Out.WriteLine("OnDataAvailable: Not Available");
            }

            var error = _receiver.GetError();
            if (error != null)
            {
                Trace.TraceWarning("PeakImputationForm Error: {0}", error);
            }
        }

        private IEnumerable<Row> GetRows(Data data)
        {
            var rowsByKey = data.Rows.ToDictionary(row => row.Key);
            var document = data.Parameter.Document;
            foreach (var moleculeGroup in document.MoleculeGroups)
            {
                foreach (var molecule in document.Molecules)
                {
                    RowData rowData = null;
                    foreach (var key in EnumerateKeys(molecule))
                    {
                        if (rowsByKey.TryGetValue(key, out rowData))
                        {
                            break;
                        }
                    }
                    if (rowData == null)
                    {
                        continue;
                    }

                    var peptide = new Peptide(DataSchema,
                        new IdentityPath(moleculeGroup.PeptideGroup, molecule.Peptide));
                    yield return new Row(peptide, rowData.Results.ToDictionary(kvp=>kvp.Key.ToString(), kvp=>kvp.Value));
                }
            }
        }

        public class Row
        {
            public Row(Peptide peptide, Dictionary<string, ResultFileData> results)
            {
                Peptide = peptide;
                Results = results;
            }

            public Peptide Peptide { get; }
            public Dictionary<string, ResultFileData> Results { get; }
        }

        public class RowData
        {
            public RowData(object key, double averageTime, bool consensus,
                Dictionary<ReplicateFileInfo, ResultFileData> results)
            {
                Key = key;
                AverageTime = averageTime;
                Consensus = consensus;
                Results = results;
            }

            public object Key { get; }
            public double AverageTime { get; }
            public bool Consensus { get; }
            public Dictionary<ReplicateFileInfo, ResultFileData> Results { get; }
        }

        public class ResultFileData
        {
            public ResultFileData(RetentionTimeSummary retentionTime, bool included)
            {
                RetentionTime = retentionTime;
                Included = included;
            }

            public RetentionTimeSummary RetentionTime { get; }
            public bool Included { get; }
        }

        private class Parameter : Immutable
        {
            public Parameter(SrmDocument document, RtValueType rtValueType,
                IEnumerable<KeyValuePair<ReplicateFileId, bool>> replicateFiles)
            {
                Document = document;
                RtValueType = rtValueType;
                ReplicateFiles = ImmutableList.ValueOf(replicateFiles);
            }

            public SrmDocument Document { get; private set; }
            public RtValueType RtValueType { get; private set; }

            public ImmutableList<KeyValuePair<ReplicateFileId, bool>> ReplicateFiles { get; }

            protected bool Equals(Parameter other)
            {
                return ReferenceEquals(Document, other.Document) && RtValueType.Equals(other.RtValueType) &&
                       ReplicateFiles.Equals(other.ReplicateFiles);
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
                    hashCode = (hashCode * 397) ^ RtValueType.GetHashCode();
                    hashCode = (hashCode * 397) ^ ReplicateFiles.GetHashCode();
                    return hashCode;
                }
            }
        }

        private class Data
        {
            public Data(Parameter paramter, IEnumerable<RowData> rows)
            {
                Parameter = paramter;
                Rows = ImmutableList.ValueOf(rows);
            }

            public Parameter Parameter { get; }
            public ImmutableList<RowData> Rows { get; }
        }

        private class DataProducer : Producer<Parameter, Data>
        {
            public static readonly DataProducer INSTANCE = new DataProducer();
            public override Data ProduceResult(ProductionMonitor productionMonitor, Parameter parameter,
                IDictionary<WorkOrder, object> inputs)
            {
                var fileInfos = ReplicateFileInfo.List(parameter.Document.MeasuredResults).ToList();
                var fileTimes = new List<Dictionary<object, double>>();
                var fileSequences = new List<List<object>>();

                foreach (var fileInfo in fileInfos)
                {
                    var dictionary = GetRetentionTimes(parameter, fileInfo);
                    fileTimes.Add(dictionary);
                    fileSequences.Add(dictionary.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList());
                }

                var rows = new List<RowData>();
                List<object> longestSequence = new List<object>();
                var longestSequenceHashSet = longestSequence.ToHashSet();
                if (fileTimes.Count > 0)
                {
                    longestSequence.AddRange(MultiSequenceLcs<object>.GreedyMultiSequenceLCS(fileSequences));
                }

                var allTargets = longestSequence.Concat(fileTimes.SelectMany(dictionary => dictionary.Keys))
                    .ToHashSet();
                foreach (var target in allTargets)
                {
                    var results = new Dictionary<ReplicateFileInfo, ResultFileData>();
                    var times = new List<double>();
                    for (int iFile = 0; iFile < fileInfos.Count; iFile++)
                    {
                        var fileInfo = fileInfos[iFile];
                        if (fileTimes[iFile].TryGetValue(target, out var time))
                        {
                            results.Add(fileInfo,
                                new ResultFileData(new RetentionTimeSummary(new Statistics(new[] { time })), true));
                            times.Add(time);
                        }
                    }

                    rows.Add(new RowData(target, times.Mean(), longestSequenceHashSet.Contains(target), results));
                }

                return new Data(parameter, rows);

            }
            private Dictionary<object, double> GetRetentionTimes(Parameter parameter, ReplicateFileInfo resultFileInfo)
            {
                return parameter.RtValueType.GetRetentionTimesForFile(parameter.Document, resultFileInfo.MsDataFileUri)
                    .GroupBy(kvp => kvp.Key, kvp => kvp.Value).ToDictionary(grouping => grouping.Key,
                        grouping => grouping.SelectMany(g => g).Median());
            }

        }

        private static IEnumerable<object> EnumerateKeys(PeptideDocNode peptideDocNode)
        {
            yield return peptideDocNode.ModifiedTarget;
            yield return peptideDocNode.Key;
        }
    }
}
