using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.SystemUtil;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Properties;
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
            comboAlignmentType.Items.AddRange(new object[]
            {
                RegressionMethodRT.linear,
                RegressionMethodRT.kde,
                RegressionMethodRT.loess
            });
            comboAlignmentType.SelectedIndex = 0;
            comboValuesToAlign.Items.AddRange(new object[]
            {
                AlignmentValueType.PEAK_APEXES,
                AlignmentValueType.MS2_IDENTIFICATIONS
            });
            comboValuesToAlign.SelectedIndex = 0;
            ComboHelper.AutoSizeDropDown(comboAlignmentType);
            comboManualPeaks.Items.AddRange(new object[]
            {
                ManualPeakTreatment.SKIP,
                ManualPeakTreatment.ACCEPT,
                ManualPeakTreatment.OVERWRITE
            });
            comboManualPeaks.SelectedIndex = 0;
            ComboHelper.AutoSizeDropDown(comboManualPeaks);

            _receiver = DataProducer.Instance.RegisterCustomer(this, OnDataAvailable);
        }

        private void OnDataAvailable()
        {
            if (_receiver.TryGetCurrentProduct(out var data))
            {
                Console.Out.WriteLine("OnDataAvailable: Not Available");
                _rowSource.Data = data;
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
                UpdateData();
            }
            finally
            {
                _inChange = false;
            }
            
        }

        private void UpdateComboBoxes()
        {
            var document = SkylineWindow.DocumentUI;
            ReplaceItems(comboAlignToFile, GetResultFileOptions(document).Prepend(null));
            ReplaceItems(comboScoringModel, GetScoringModels(document).Prepend(null), 1);
        }

        private void ReplaceItems<T>(ComboBox comboBox, IEnumerable<T> items, int defaultSelectedIndex = 0)
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
            else
            {
                comboBox.SelectedIndex = Math.Min(defaultSelectedIndex, comboBox.Items.Count - 1);
            }
            ComboHelper.AutoSizeDropDown(comboBox);
        }

        private static IEnumerable<ResultFileOption> GetResultFileOptions(SrmDocument document)
        {
            var measuredResults = document.MeasuredResults;
            if (measuredResults == null)
            {
                return Array.Empty<ResultFileOption>();
            }

            if (measuredResults.Chromatograms.All(c => c.FileCount == 1))
            {
                return measuredResults.Chromatograms.Select((c,index)=>
                    new ResultFileOption(c.Name, index, c.MSDataFilePaths.Single()));
            }

            var fileUris = new HashSet<MsDataFileUri>();
            var peakResultFiles = new List<ResultFileOption>();
            for (int replicateIndex = 0; replicateIndex < measuredResults.Chromatograms.Count; replicateIndex++)
            {
                var chromatogramSet = measuredResults.Chromatograms[replicateIndex];
                foreach (var fileUri in chromatogramSet.MSDataFilePaths)
                {
                    if (fileUris.Add(fileUri))
                    {
                        peakResultFiles.Add(new ResultFileOption(fileUri.ToString(), replicateIndex, fileUri));
                    }
                }
            }

            if (peakResultFiles.Select(file => file.Path.GetFileName()).Distinct().Count() == peakResultFiles.Count)
            {
                return peakResultFiles.Select(file =>
                    new ResultFileOption(file.Path.GetFileName(), file.ReplicateIndex, file.Path));
            }

            return peakResultFiles;
        }

        private static IEnumerable<PeakScoringModelSpec> GetScoringModels(SrmDocument document)
        {
            return Settings.Default.PeakScoringModelList
                .Prepend(LegacyScoringModel.DEFAULT_MODEL)
                .Prepend(document.Settings.PeptideSettings.Integration.PeakScoringModel)
                .Distinct()
                .Where(model => true == model?.IsTrained);
        }

        public class Row : SkylineObject
        {
            public Row(Model.Databinding.Entities.Peptide peptide, IEnumerable<Peak> corePeaks, IEnumerable<Peak> outlierPeaks)
            {
                Peptide = peptide;
                Peaks = new Dictionary<ResultFileOption, Peak>();
                var allRetentionTimes = new List<double>();
                var coreRetentionTimes = new List<double>();
                foreach (var outlier in outlierPeaks)
                {
                    if (outlier.ApexTime.HasValue)
                    {
                        allRetentionTimes.Add(outlier.ApexTime.Value);
                    }
                    Peaks.Add(outlier.ResultFileInfo.ResultFileOption, outlier.ChangeOutlier(true));
                }
                foreach (var corePeak in corePeaks)
                {
                    allRetentionTimes.Add(corePeak.ApexTime.Value);
                    coreRetentionTimes.Add(corePeak.ApexTime.Value);
                    Peaks[corePeak.ResultFileInfo.ResultFileOption] = corePeak;
                }

                MeanRetentionTime = allRetentionTimes.Mean();
                StdDevRetentionTime = allRetentionTimes.StandardDeviation();
                CoreMeanRetentionTime = coreRetentionTimes.Mean();
                CoreStdDevRetentionTime = coreRetentionTimes.StandardDeviation();
                CoreCount = coreRetentionTimes.Count;
                OutlierCount = Peaks.Count - CoreCount;
                if (Peaks.Count > 0)
                {
                    BestScore = Peaks.Values.Max(peak => peak.Score);
                }
            }

            protected override SkylineDataSchema GetDataSchema()
            {
                return Peptide.DataSchema;
            }

            [InvariantDisplayName("Molecule", ExceptInUiMode = UiModes.PROTEOMIC)]
            public Model.Databinding.Entities.Peptide Peptide { get; }
            public double? MeanRetentionTime { get; }
            public double? StdDevRetentionTime { get; }
            public double? BestScore { get; }
            public int CoreCount { get; }
            public double? CoreMeanRetentionTime { get; }
            public double? CoreStdDevRetentionTime { get; }
            public int OutlierCount { get; }

            public Dictionary<ResultFileOption, Peak> Peaks { get; }
        }

        public class Peak : Immutable
        {
            public Peak(ResultFileInfo resultFileInfo, double? apexTime, double? score, bool manuallyIntegrated)
            {
                ResultFileInfo = resultFileInfo;
                ApexTime = apexTime;
                Score = score;
                ManuallyIntegrated = manuallyIntegrated;
            }
            public ResultFileInfo ResultFileInfo { get; }
            public double? ApexTime { get; }
            public double? Score { get; }
            public bool ManuallyIntegrated { get; }
            public bool Outlier { get; private set; }

            public Peak ChangeOutlier(bool outlier)
            {
                return ChangeProp(ImClone(this), im => im.Outlier = outlier);
            }
        }

        public class ResultFileOption
        {
            private string _display;
            public ResultFileOption(string display, int replicateIndex, MsDataFileUri path)
            {
                _display = display;
                ReplicateIndex = replicateIndex;
                Path = path;
            }

            public int ReplicateIndex { get; }
            public MsDataFileUri Path { get; }

            public override string ToString()
            {
                return _display;
            }

            protected bool Equals(ResultFileOption other)
            {
                return Path.Equals(other.Path);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ResultFileOption)obj);
            }

            public override int GetHashCode()
            {
                return Path.GetHashCode();
            }
        }

        public class ResultFileInfo
        {
            public ResultFileInfo(ResultFileOption resultFileOption, ChromFileInfoId chromFileInfoId, AlignmentFunction alignmentFunction)
            {
                ResultFileOption = resultFileOption;
                ChromFileInfoId = chromFileInfoId;
                AlignmentFunction = alignmentFunction;
            }

            public ResultFileOption ResultFileOption { get; }
            public ChromFileInfoId ChromFileInfoId { get; }
            public AlignmentFunction AlignmentFunction { get; }
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
                    if (ReferenceEquals(_data, value))
                    {
                        return;
                    }
                    _data = value;
                    FireListChanged();
                }
            }

            public override IEnumerable GetItems()
            {
                Data data = Data;
                if (data == null)
                {
                    yield break;
                }
                var document = DataSchema.Document;
                var resultFileInfos = data.GetResultFileInfos().ToDictionary(info => info.ResultFileOption.Path);
                foreach (var moleculeGroup in document.MoleculeGroups)
                {
                    foreach (var molecule in moleculeGroup.Molecules)
                    {
                        if (molecule.GlobalStandardType != null)
                        {
                            continue;
                        }
                        CancellationToken.ThrowIfCancellationRequested();
                        var peptide = new Model.Databinding.Entities.Peptide(DataSchema,
                            new IdentityPath(moleculeGroup.PeptideGroup, molecule.Peptide));
                        var peaks = new List<Peak>();
                        foreach (var peptideResult in peptide.Results.Values)
                        {
                            CancellationToken.ThrowIfCancellationRequested();
                            if (!resultFileInfos.TryGetValue(peptideResult.ResultFile.ChromFileInfo.FilePath, out var peakResultFile))
                            {
                                // Shouldn't happen
                                continue;
                            }

                            bool manuallyIntegrated = IsManualIntegrated(molecule, peptideResult.ResultFile);
                            if (data.Parameters.ManualPeakTreatment == ManualPeakTreatment.SKIP && manuallyIntegrated)
                            {
                                continue;
                            }

                            var retentionTime = peptideResult.PeptideRetentionTime;
                            if (retentionTime.HasValue)
                            {
                                AlignmentFunction alignmentFunction = 
                                data.GetAlignmentFunction(peptideResult.ResultFile.ChromFileInfo.FilePath);
                                if (alignmentFunction == null)
                                {
                                    continue;
                                }

                                retentionTime = alignmentFunction.GetY(retentionTime.Value);
                            }

                            var peakFeatureStatistics = data.ResultsHandler?.GetPeakFeatureStatistics(molecule.Peptide,
                                peptideResult.ResultFile.ChromFileInfoId);
                            var peak = new Peak(peakResultFile, retentionTime, peakFeatureStatistics?.BestScore,
                                manuallyIntegrated);
                            peaks.Add(peak);
                        }

                        var row = MakeRow(data.Parameters, peptide, peaks);
                        yield return row;
                    }
                }
            }
        }

        private static Row MakeRow(Parameters parameters, Model.Databinding.Entities.Peptide peptide, List<Peak> peaks)
        {
            var outliers = new List<Peak>();
            var candidates = new List<Peak>();
            var core = new List<Peak>();
            foreach (var peak in peaks)
            {
                if (!peak.ApexTime.HasValue)
                {
                    outliers.Add(peak);
                    continue;
                }
                if (peak.ManuallyIntegrated)
                {
                    if (parameters.ManualPeakTreatment == ManualPeakTreatment.SKIP)
                    {
                        continue;
                    }

                    if (parameters.ManualPeakTreatment == ManualPeakTreatment.ACCEPT)
                    {
                        core.Add(peak);
                    }
                    else
                    {
                        outliers.Add(peak);
                    }
                    continue;
                }

                candidates.Add(peak);
            }

            {
                var newCandidates = new List<Peak>();
                foreach (var peak in candidates.OrderByDescending(peak => peak.Score).ToList())
                {
                    if (parameters.ScoreCutoff.HasValue && peak.Score >= parameters.ScoreCutoff)
                    {
                        core.Add(peak);
                        continue;
                    }

                    if (peak.Score.HasValue && core.Count < parameters.MinCoreCount)
                    {
                        core.Add(peak);
                        continue;
                    }

                    newCandidates.Add(peak);
                }

                candidates = newCandidates;
            }

            if (parameters.StandardDeviationsCutoff.HasValue)
            {
                while (candidates.Count > 0)
                {
                    var retentionTimes = new Util.Statistics(core.Select(peak => peak.ApexTime.Value));
                    if (retentionTimes.Length < 2)
                    {
                        break;
                    }

                    var meanRetentionTime = retentionTimes.Mean();
                    var stdDevRetentionTime = retentionTimes.StdDev();
                    if (double.IsNaN(stdDevRetentionTime))
                    {
                        break;
                    }

                    var newCandidates = new List<Peak>();
                    foreach (var peak in candidates)
                    {
                        if (Math.Abs(peak.ApexTime.Value - meanRetentionTime) < stdDevRetentionTime * parameters.StandardDeviationsCutoff)
                        {
                            core.Add(peak);
                        }
                        else
                        {
                            newCandidates.Add(peak);
                        }
                    }

                    if (newCandidates.Count == candidates.Count)
                    {
                        break;
                    }

                    candidates = newCandidates;
                }
            }
            outliers.AddRange(candidates);
            return new Row(peptide, core, outliers);
        }

        private static bool IsManualIntegrated(PeptideDocNode peptideDocNode, ResultFile resultFile)
        {
            foreach (var transitionGroupDocNode in peptideDocNode.TransitionGroups)
            {
                foreach (var transitionGroupChromInfo in transitionGroupDocNode.GetSafeChromInfo(resultFile.Replicate
                             .ReplicateIndex))
                {
                    if (ReferenceEquals(resultFile.ChromFileInfoId, transitionGroupChromInfo.FileId) &&
                        transitionGroupChromInfo.UserSet == UserSet.TRUE)
                    {
                        return true;
                    }
                }
            }

            return false;
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

        public class Parameters : Immutable
        {
            public Parameters(SrmDocument document)
            {
                Document = document;
            }
            public ReferenceValue<SrmDocument> Document { get; }
            public ManualPeakTreatment ManualPeakTreatment { get; private set; } = ManualPeakTreatment.SKIP;

            public Parameters ChangeManualPeakTreatment(ManualPeakTreatment manualPeakTreatment)
            {
                return ChangeProp(ImClone(this), im => im.ManualPeakTreatment = manualPeakTreatment);
            }
            public MsDataFileUri AlignmentTarget { get; private set; }
            public AlignmentValueType AlignmentValueType { get; private set; }
            public RegressionMethodRT RegressionMethod { get; private set; }

            public Parameters ChangeAlignment(MsDataFileUri target, AlignmentValueType alignmentValueType, RegressionMethodRT regressionMethod)
            {
                return ChangeProp(ImClone(this), im =>
                {
                    im.AlignmentTarget = target;
                    im.AlignmentValueType = alignmentValueType;
                    im.RegressionMethod = regressionMethod;
                });
            }
            
            public PeakScoringModelSpec PeakScoringModel { get; private set; }
            public int MinCoreCount { get; private set; }
            public double? ScoreCutoff { get; private set; }
            public double? StandardDeviationsCutoff { get; private set; }

            public Parameters ChangeScoringModel(PeakScoringModelSpec model, int minCoreCount, double? scoreCutoff,
                double? standardDeviationsCutoff)
            {
                return ChangeProp(ImClone(this), im =>
                {
                    im.PeakScoringModel = model;
                    im.MinCoreCount = minCoreCount;
                    im.ScoreCutoff = scoreCutoff;
                    im.StandardDeviationsCutoff = standardDeviationsCutoff;
                });
            }

            protected bool Equals(Parameters other)
            {
                return Document.Equals(other.Document) && 
                       Equals(ManualPeakTreatment, other.ManualPeakTreatment) &&
                       Equals(AlignmentTarget, other.AlignmentTarget) &&
                       RegressionMethod == other.RegressionMethod && Equals(PeakScoringModel, other.PeakScoringModel) &&
                       MinCoreCount == other.MinCoreCount && Nullable.Equals(ScoreCutoff, other.ScoreCutoff) &&
                       Nullable.Equals(StandardDeviationsCutoff, other.StandardDeviationsCutoff);
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
                    var hashCode = Document.GetHashCode();
                    hashCode = (hashCode * 397) ^ ManualPeakTreatment.GetHashCode();
                    hashCode = (hashCode * 397) ^ (AlignmentTarget != null ? AlignmentTarget.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (AlignmentValueType?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ (int)RegressionMethod;
                    hashCode = (hashCode * 397) ^ (PeakScoringModel != null ? PeakScoringModel.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ MinCoreCount;
                    hashCode = (hashCode * 397) ^ ScoreCutoff.GetHashCode();
                    hashCode = (hashCode * 397) ^ StandardDeviationsCutoff.GetHashCode();
                    return hashCode;
                }
            }
        }

        public class Data
        {
            private Dictionary<MsDataFileUri, AlignmentFunction> _alignmentFunctions;

            public Data(Parameters parameters, MProphetResultsHandler resultsHandler, IEnumerable<KeyValuePair<MsDataFileUri, AlignmentFunction>> alignmentFunctions)
            {
                Parameters = parameters;
                ResultsHandler = resultsHandler;
                _alignmentFunctions = alignmentFunctions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            public Parameters Parameters { get; }

            public AlignmentFunction GetAlignmentFunction(MsDataFileUri msDataFileUri)
            {
                if (Parameters.AlignmentTarget == null)
                {
                    return AlignmentFunction.IDENTITY;
                }
                _alignmentFunctions.TryGetValue(msDataFileUri, out var alignmentFunction);
                return alignmentFunction;
            }

            public MProphetResultsHandler ResultsHandler { get; }

            public IEnumerable<ResultFileInfo> GetResultFileInfos()
            {
                var measuredResults = Parameters.Document.Value.MeasuredResults;
                if (measuredResults == null)
                {
                    yield break;
                }
                foreach (var resultFileOption in GetResultFileOptions(Parameters.Document))
                {
                    var chromFileInfoId = measuredResults.Chromatograms[resultFileOption.ReplicateIndex]
                        .FindFile(resultFileOption.Path);
                    var alignmentFunction = GetAlignmentFunction(resultFileOption.Path);
                    yield return new ResultFileInfo(resultFileOption, chromFileInfoId, alignmentFunction);
                }
            }
        }

        private class DataProducer : Producer<Parameters, Data>
        {
            public static readonly DataProducer Instance = new DataProducer();
            public override Data ProduceResult(ProductionMonitor productionMonitor, Parameters parameter, IDictionary<WorkOrder, object> inputs)
            {
                var alignments = new List<KeyValuePair<MsDataFileUri, AlignmentFunction>>();
                MProphetResultsHandler resultsHandler = ScoringProducer.Instance.GetResult(inputs, new ScoringProducer.Parameters(parameter.Document, parameter.PeakScoringModel));
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
                return new Data(parameter, resultsHandler, alignments);
            }

            public override IEnumerable<WorkOrder> GetInputs(Parameters parameter)
            {
                SrmDocument document = parameter.Document;
                if (document.MeasuredResults == null)
                {
                    yield break;
                }

                foreach (var msDataFileUri in document.MeasuredResults.MSDataFilePaths)
                {
                    yield return AlignmentProducer.Instance.MakeWorkOrder(
                        new AlignmentProducer.Parameter(parameter.AlignmentValueType, parameter.RegressionMethod, document, msDataFileUri, parameter.AlignmentTarget));
                }

                if (parameter.PeakScoringModel != null)
                {
                    yield return ScoringProducer.Instance.MakeWorkOrder(
                        new ScoringProducer.Parameters(parameter.Document, parameter.PeakScoringModel));
                }
            }
        }

        private void SettingsControlChanged(object sender, EventArgs e)
        {
            if (_inChange)
            {
                return;
            }
            UpdateData();
        }

        private void UpdateData()
        {
            var parameters = new Parameters(SkylineWindow.Document);
            var alignmentTarget = comboAlignToFile.SelectedItem as ResultFileOption;
            if (alignmentTarget != null)
            {
                var regressionMethod = comboAlignmentType.SelectedItem as RegressionMethodRT? ?? RegressionMethodRT.linear;
                var alignmentValueType = comboValuesToAlign.SelectedItem as AlignmentValueType ??
                                         AlignmentValueType.PEAK_APEXES;
                parameters = parameters.ChangeAlignment(alignmentTarget.Path, alignmentValueType, regressionMethod);
            }

            parameters = parameters.ChangeManualPeakTreatment(
                comboManualPeaks.SelectedItem as ManualPeakTreatment ?? ManualPeakTreatment.SKIP);
            var scoringModel = comboScoringModel.SelectedItem as PeakScoringModelSpec;
            groupBoxCoreCriteria.Enabled = scoringModel != null;
            if (scoringModel != null)
            {
                parameters = parameters.ChangeScoringModel(scoringModel,
                    Convert.ToInt32(numericUpDownCoreResults.Value), GetDoubleValue(tbxCoreScoreCutoff),
                    GetDoubleValue(tbxStandardDeviationsCutoff));
            }

            if (true == _receiver?.TryGetProduct(parameters, out _))
            {
                OnDataAvailable();
            }
        }

        private double? GetDoubleValue(TextBox textBox)
        {
            var text = textBox.Text.Trim();
            double? value = null;
            if (!string.IsNullOrEmpty(text))
            {
                if (!double.TryParse(text, out var doubleValue))
                {
                    textBox.BackColor = Color.Red;
                    return null;
                }

                value = doubleValue;
            }
            textBox.BackColor = Color.White;
            return value;
        }

        private void btnImputeBoundaries_Click(object sender, EventArgs e)
        {
            lock (SkylineWindow.GetDocumentChangeLock())
            {
                var originalDocument = SkylineWindow.DocumentUI;
                var newDoc = originalDocument.BeginDeferSettingsChanges();
                var rows = BindingListSource.OfType<RowItem>().Select(rowItem => rowItem.Value).OfType<Row>().ToList();
                using (var longWaitDlg = new LongWaitDlg())
                {
                    int changeCount = 0;
                    longWaitDlg.PerformWork(this, 1000, () =>
                    {
                        for (int iRow = 0; iRow < rows.Count; iRow++)
                        {
                            if (longWaitDlg.IsCanceled)
                            {
                                return;
                            }

                            longWaitDlg.ProgressValue = 100 * iRow / rows.Count;
                            newDoc = ImputeBoundaries(newDoc.BeginDeferSettingsChanges(), rows[iRow], ref changeCount);
                        }
                    });
                    if (longWaitDlg.IsCanceled)
                    {
                        return;
                    }

                    if (changeCount == 0)
                    {
                        return;
                    }

                    newDoc = newDoc.EndDeferSettingsChanges(originalDocument, null);
                    SkylineWindow.ModifyDocument("Impute peak boundaries", doc =>
                    {
                        if (!ReferenceEquals(doc, originalDocument))
                        {
                            throw new InvalidOperationException(Resources.SkylineDataSchema_VerifyDocumentCurrent_The_document_was_modified_in_the_middle_of_the_operation_);
                        }

                        return newDoc;
                    }, docPair=>AuditLogEntry.CreateSimpleEntry(MessageType.applied_peak_all, docPair.NewDocumentType, MessageArgs.Create(changeCount)));
                }
            }
        }

        private SrmDocument ImputeBoundaries(SrmDocument document, Row row, ref int changeCount)
        {
            var corePeakBounds = new List<PeakBounds>();
            foreach (var corePeak in row.Peaks.Values.Where(peak => !peak.Outlier))
            {
                corePeakBounds.Add(GetPeakBounds(row.Peptide, corePeak));
            }

            if (!corePeakBounds.Any())
            {
                return document;
            }

            var meanApexTime = corePeakBounds.Select(peak => peak.ApexTime).Mean();
            var meanLeftWidth = corePeakBounds.Select(peak => peak.LeftWidth).Mean();
            var meanRightWidth = corePeakBounds.Select(peak => peak.RightWidth).Mean();
            foreach (var outlierPeak in row.Peaks.Values.Where(peak => peak.Outlier))
            {
                var resultFileInfo = outlierPeak.ResultFileInfo;
                var newStartTime = resultFileInfo.AlignmentFunction.GetX(meanApexTime - meanLeftWidth);
                var newEndTime = resultFileInfo.AlignmentFunction.GetX(meanApexTime + meanRightWidth);
                foreach (var transitionGroupDocNode in row.Peptide.DocNode.TransitionGroups)
                {
                    var identityPath =
                        new IdentityPath(row.Peptide.IdentityPath, transitionGroupDocNode.TransitionGroup);
                    var chromatogramSet =
                        document.MeasuredResults.Chromatograms[resultFileInfo.ResultFileOption.ReplicateIndex];
                    document = document.ChangePeak(identityPath, chromatogramSet.Name,
                        resultFileInfo.ResultFileOption.Path, null, newStartTime, newEndTime, UserSet.MATCHED, null,
                        false);
                    changeCount++;
                }
            }

            return document;
        }

        public PeakBounds GetPeakBounds(Model.Databinding.Entities.Peptide peptide, Peak peak)
        {
            var alignmentFunction = peak.ResultFileInfo.AlignmentFunction;
            var apexTime = peak.ApexTime.Value;
            double? minRawStartTime = null;
            double? maxRawEndTime = null;
            foreach (var transitionGroup in peptide.DocNode.TransitionGroups)
            {
                foreach (var chromInfo in transitionGroup.GetSafeChromInfo(peak.ResultFileInfo.ResultFileOption
                             .ReplicateIndex))
                {
                    if (!ReferenceEquals(peak.ResultFileInfo.ChromFileInfoId, chromInfo.FileId))
                    {
                        continue;
                    }

                    if (chromInfo.StartRetentionTime.HasValue)
                    {
                        if (minRawStartTime == null || minRawStartTime > chromInfo.StartRetentionTime)
                        {
                            minRawStartTime = chromInfo.StartRetentionTime.Value;
                        }
                    }

                    if (chromInfo.EndRetentionTime.HasValue)
                    {
                        if (maxRawEndTime == null || maxRawEndTime < chromInfo.EndRetentionTime)
                        {
                            maxRawEndTime = chromInfo.EndRetentionTime.Value;
                        }
                    }
                }
            }

            double minStartTime;
            double maxEndTime;
            if (minRawStartTime.HasValue)
            {
                minStartTime = alignmentFunction.GetY(minRawStartTime.Value);
            }
            else
            {
                minStartTime = apexTime;
            }

            if (maxRawEndTime.HasValue)
            {
                maxEndTime = alignmentFunction.GetY(maxRawEndTime.Value);
            }
            else
            {
                maxEndTime = apexTime;
            }

            return new PeakBounds(apexTime, apexTime - minStartTime, maxEndTime - apexTime);
        }

        public class PeakBounds
        {
            public PeakBounds(double apexTime, double leftWidth, double rightWidth)
            {
                ApexTime = apexTime;
                LeftWidth = leftWidth;
                RightWidth = rightWidth;
            }
            public double ApexTime { get; }
            public double LeftWidth { get; }
            public double RightWidth { get; }
        }
    }
}
