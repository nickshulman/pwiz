using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using MathNet.Numerics.Statistics;
using NHibernate.SqlCommand;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public partial class PeakImputationForm : DataboundGridForm
    {
        private PeakRowSource _rowSource;
        private readonly Receiver<Parameters, Data> _receiver;
        public PeakImputationForm(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            var dataSchema = new SkylineWindowDataSchema(skylineWindow);
            var rootColumn = ColumnDescriptor.RootColumn(dataSchema, typeof(Row));
            _rowSource = new PeakRowSource(dataSchema);
            var viewContext = new SkylineViewContext(rootColumn, _rowSource);
            BindingListSource.SetViewContext(viewContext);
            _receiver = DataProducer.Instance.RegisterCustomer(this, OnDataAvailable);
        }

        private void OnDataAvailable()
        {
            if (_receiver.TryGetCurrentProduct(out var data))
            {
                _rowSource.Data = data;
            }

            var error = _receiver.GetError();
            if (error != null)
            {
                Trace.TraceWarning("PeakImputationForm Error: {0}", error);
            }
        }

        public SkylineWindow SkylineWindow { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SkylineWindow.DocumentUIChangedEvent += SkylineWindow_OnDocumentUIChangedEvent;
            OnDocumentChanged();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            SkylineWindow.DocumentUIChangedEvent -= SkylineWindow_OnDocumentUIChangedEvent;
            base.OnHandleDestroyed(e);
        }

        private void SkylineWindow_OnDocumentUIChangedEvent(object sender, DocumentChangedEventArgs e)
        {
            OnDocumentChanged();
        }

        private bool _inChange;
        private void OnDocumentChanged()
        {
            if (_inChange)
            {
                return;
            }
            try
            {
                _inChange = true;
                UpdateComboBoxes();
            }
            finally
            {
                _inChange = false;
            }
        }

        private void UpdateComboBoxes()
        {
            var document = SkylineWindow.DocumentUI;
            ReplaceItems(comboAlignToFile, GetResultFiles(document).Prepend(null));
        }

        private void ReplaceItems<T>(ComboBox comboBox, IEnumerable<T> items)
        {
            var itemArray = items.Select(item=>(object) item ?? string.Empty).ToArray();
            if (itemArray.SequenceEqual(comboBox.Items.Cast<object>()))
            {
                return;
            }
            var oldSelectedItem = comboBox.SelectedItem as KeyValuePair<string, T>?;
            comboBox.Items.Clear();
            comboBox.Items.AddRange(itemArray);
            int newSelectedIndex = -1;
            if (oldSelectedItem.HasValue)
            {
                newSelectedIndex = itemArray.Select(Tuple.Create<object, int>)
                    .FirstOrDefault(tuple => Equals(tuple.Item1, oldSelectedItem.Value.Value))?.Item2 ?? -1;
            }
            if (newSelectedIndex >= 0)
            {
                comboBox.SelectedIndex = newSelectedIndex;
            }
            else if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private static IEnumerable<PeakResultFile> GetResultFiles(SrmDocument document)
        {
            var measuredResults = document.MeasuredResults;
            if (measuredResults == null)
            {
                return Array.Empty<PeakResultFile>();
            }

            if (measuredResults.Chromatograms.All(c => c.FileCount == 1))
            {
                return measuredResults.Chromatograms.Select(c =>
                    new PeakResultFile(c.Name, c.MSDataFilePaths.Single()));
            }

            var files = measuredResults.Chromatograms.SelectMany(f => f.MSDataFilePaths).Distinct().ToList();
            if (files.Select(file => file.GetFileName()).Distinct().Count() == files.Count)
            {
                return files.Select(file => new PeakResultFile(file.GetFileName(), file));
            }

            return files.Select(file => new PeakResultFile(file.ToString(), file));
        }

        public class Row : SkylineObject
        {
            public Row(Model.Databinding.Entities.Peptide peptide, Dictionary<PeakResultFile, Peak> peaks)
            {
                Peptide = peptide;
                Peaks = peaks;
                MeanRetentionTime = peaks.Values.Select(peak => peak.ApexTime).Mean();
                StdDevRetentionTime = peaks.Values.Select(peak => peak.ApexTime).StandardDeviation();
            }

            protected override SkylineDataSchema GetDataSchema()
            {
                return Peptide.DataSchema;
            }

            [InvariantDisplayName("Molecule", ExceptInUiMode = UiModes.PROTEOMIC)]
            public Model.Databinding.Entities.Peptide Peptide { get; }
            public double MeanRetentionTime { get; }
            public double StdDevRetentionTime { get; }

            public Dictionary<PeakResultFile, Peak> Peaks { get; }
        }

        public class Peak
        {
            public Peak(double apexTime, double startTime, double endTime)
            {
                ApexTime = apexTime;
                StartTime = startTime;
                EndTime = endTime;
            }
            public double ApexTime { get; }
            public double StartTime { get; }
            public double EndTime { get; }
            public double Width
            {
                get { return EndTime - StartTime; }
            }
        }

        public class PeakResultFile
        {
            private string _display;
            public PeakResultFile(string display, MsDataFileUri path)
            {
                _display = display;
                Path = path;
            }

            public MsDataFileUri Path { get; }

            public override string ToString()
            {
                return _display;
            }

            protected bool Equals(PeakResultFile other)
            {
                return Path.Equals(other.Path);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PeakResultFile)obj);
            }

            public override int GetHashCode()
            {
                return Path.GetHashCode();
            }
        }

        class PeakRowSource : SkylineObjectList<Row>
        {
            private Data _data;
            public PeakRowSource(SkylineDataSchema dataSchema) : base(dataSchema)
            {
            }

            public Data Data
            {
                get
                {
                    return _data;
                }
                set
                {
                    lock (this)
                    {
                        _data = value;
                        FireListChanged();
                    }
                }
            }

            public override IEnumerable GetItems()
            {
                Data data;
                lock (this)
                {
                    data = Data;
                }
                var document = DataSchema.Document;
                var peakResultFiles = GetResultFiles(document).ToDictionary(resultFile=>resultFile.Path);
                foreach (var moleculeGroup in document.MoleculeGroups)
                {
                    foreach (var molecule in moleculeGroup.Molecules)
                    {
                        var peptide = new Model.Databinding.Entities.Peptide(DataSchema,
                            new IdentityPath(moleculeGroup.PeptideGroup, molecule.Peptide));
                        var peaks = new Dictionary<PeakResultFile, Peak>();
                        foreach (var peptideResult in peptide.Results.Values)
                        {
                            if (!peptideResult.PeptideRetentionTime.HasValue)
                            {
                                continue;
                            }
                            AlignmentFunction alignmentFunction = AlignmentFunction.IDENTITY;
                            if (data != null)
                            {
                                alignmentFunction =
                                    data.GetAlignmentFunction(peptideResult.ResultFile.ChromFileInfo.FilePath);
                                if (alignmentFunction == null)
                                {
                                    continue;
                                }
                            }
                            if (!peakResultFiles.TryGetValue(peptideResult.ResultFile.ChromFileInfo.FilePath, out var peakResultFile))
                            {
                                continue;
                            }
                            var peak = new Peak(alignmentFunction.GetY(peptideResult.PeptideRetentionTime.Value),
                                peptideResult.PeptideRetentionTime.Value, peptideResult.PeptideRetentionTime.Value);
                            peaks[peakResultFile] = peak;
                        }
                        yield return new Row(peptide, peaks);
                    }
                }
            }
        }

        class RetentionTimeValue
        {
            private Func<string> _getLabelFunc;
            private Func<PeptideResult, double?> _getValueFunc;
            public RetentionTimeValue(Func<string> getLabelFunc, Func<PeptideResult, double?> getValueFunc)
            {
                _getLabelFunc = getLabelFunc;
                _getValueFunc = getValueFunc;
            }

            public override string ToString()
            {
                return _getLabelFunc();
            }

            public double? GetValue(PeptideResult peptideResult)
            {
                return _getValueFunc(peptideResult);
            }
        }

        private ImmutableList<RetentionTimeValue> RetentionTimeValues = ImmutableList.ValueOf(new[]
        {
            new RetentionTimeValue(() => "Apex Time", peptideResult => peptideResult.PeptideRetentionTime),
        });

        public class AlignmentKey
        {
            public MsDataFileUri Source { get; }
            public MsDataFileUri Target { get; }
        }

        public class Parameters
        {
            public Parameters(SrmDocument document, MsDataFileUri alignmenTarget)
            {
                Document = document;
                AlignmentTarget = alignmenTarget;
            }
            public SrmDocument Document { get; }
            public MsDataFileUri AlignmentTarget { get; }
        }

        public class Data
        {
            private Dictionary<MsDataFileUri, AlignmentFunction> _alignmentFunctions;

            public Data(IEnumerable<KeyValuePair<MsDataFileUri, AlignmentFunction>> alignmentFunctions)
            {
                _alignmentFunctions = alignmentFunctions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            public AlignmentFunction GetAlignmentFunction(MsDataFileUri msDataFileUri)
            {
                _alignmentFunctions.TryGetValue(msDataFileUri, out var alignmentFunction);
                return alignmentFunction;
            }
        }

        private class DataProducer : Producer<Parameters, Data>
        {
            public static readonly DataProducer Instance = new DataProducer();
            public override Data ProduceResult(ProductionMonitor productionMonitor, Parameters parameter, IDictionary<WorkOrder, object> inputs)
            {
                var alignments = new List<KeyValuePair<MsDataFileUri, AlignmentFunction>>();
                foreach (var input in inputs)
                {
                    var workParameter = (AlignmentProducer.Parameter)input.Key.WorkParameter;
                    var alignmentFunction = (AlignmentFunction)input.Value;
                    if (alignmentFunction != null)
                    {
                        alignments.Add(new KeyValuePair<MsDataFileUri, AlignmentFunction>(workParameter.Source, alignmentFunction));
                    }
                }
                return new Data(alignments);
            }

            public override IEnumerable<WorkOrder> GetInputs(Parameters parameter)
            {
                var document = parameter.Document;
                if (document.MeasuredResults == null)
                {
                    yield break;
                }

                foreach (var msDataFileUri in document.MeasuredResults.MSDataFilePaths)
                {
                    yield return AlignmentProducer.Instance.MakeWorkOrder(
                        new AlignmentProducer.Parameter(document, msDataFileUri, parameter.AlignmentTarget));
                }
            }
        }

        private void comboAlignToFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_inChange)
            {
                return;
            }
            var alignmentTarget = comboAlignToFile.SelectedItem as PeakResultFile;
            var parameters = new Parameters(SkylineWindow.Document, alignmentTarget?.Path);
            if (_receiver.TryGetProduct(parameters, out var data))
            {
                _rowSource.Data = data;
            }
        }
    }
}
