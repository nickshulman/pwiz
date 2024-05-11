using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
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
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using Statistics = pwiz.Skyline.Util.Statistics;

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
            _rowSource.RowStatisticsAvailable += RowStatisticsAvailable;
            var viewContext = new SkylineViewContext(rootColumn, _rowSource);
            BindingListSource.SetViewContext(viewContext);
            alignmentControl.DocumentUiContainer = skylineWindow;
            comboManualPeaks.Items.AddRange(new object[]
            {
                ManualPeakTreatment.SKIP,
                ManualPeakTreatment.ACCEPT,
                ManualPeakTreatment.OVERWRITE
            });
            comboManualPeaks.SelectedIndex = 0;
            ComboHelper.AutoSizeDropDown(comboManualPeaks);
            comboImputeBoundariesFrom.SelectedIndex = 0;
            ComboHelper.AutoSizeDropDown(comboImputeBoundariesFrom);

            _receiver = DataProducer.Instance.RegisterCustomer(this, OnDataAvailable);
        }

        private void RowStatisticsAvailable(RowStatistics obj)
        {
            CommonActionUtil.SafeBeginInvoke(this, () =>
            {
                tbxMeanStandardDeviation.Text = obj.MeanStandardDeviation.ToString(CultureInfo.CurrentCulture);
            });
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
            ComboHelper.ReplaceItems(comboScoringModel, GetScoringModels(document).Prepend(null), 1);
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
            public Row(Model.Databinding.Entities.Peptide peptide, IEnumerable<Peak> peaks)
            {
                Peptide = peptide;
                Peaks = new Dictionary<ResultFileOption, Peak>();
                var allPeakBounds = new List<PeakBounds>();
                var acceptedPeakBounds = new List<PeakBounds>();
                var exemplaryPeakBounds = new List<PeakBounds>();
                foreach (var peak in peaks)
                {
                    Peaks[peak.ResultFileInfo.ResultFileOption] = peak;
                    var peakBounds = peak.AlignedPeakBounds;
                    if (peakBounds != null)
                    {
                        allPeakBounds.Add(peakBounds);
                        if (peak.Accepted)
                        {
                            acceptedPeakBounds.Add(peakBounds);
                        }

                        if (peak.Exemplary)
                        {
                            exemplaryPeakBounds.Add(peakBounds);
                        }
                    }
                }

                if (allPeakBounds.Any())
                {
                    AllPeakBoundaries = new PeakSummary(allPeakBounds);
                }

                if (acceptedPeakBounds.Any())
                {
                    AcceptedPeakBoundaries = new PeakSummary(acceptedPeakBounds);
                }

                if (exemplaryPeakBounds.Any())
                {
                    ExemplaryPeakBoundaries = new PeakSummary(exemplaryPeakBounds);
                }
            }

            protected override SkylineDataSchema GetDataSchema()
            {
                return Peptide.DataSchema;
            }

            [InvariantDisplayName("Molecule", ExceptInUiMode = UiModes.PROTEOMIC)]
            public Model.Databinding.Entities.Peptide Peptide { get; }

            public PeakSummary AllPeakBoundaries { get; }
            public PeakSummary AcceptedPeakBoundaries { get; }
            public PeakSummary ExemplaryPeakBoundaries { get; }
            public Dictionary<ResultFileOption, Peak> Peaks { get; }
        }

        public class Peak : Immutable
        {
            public Peak(ResultFileInfo resultFileInfo, PeakBounds rawPeakBounds, double? score, bool manuallyIntegrated)
            {
                ResultFileInfo = resultFileInfo;
                RawPeakBounds = rawPeakBounds;
                AlignedPeakBounds = rawPeakBounds?.Align(resultFileInfo.AlignmentFunction);
                ManuallyIntegrated = manuallyIntegrated;
                Score = score;
            }
            public ResultFileInfo ResultFileInfo { get; }
            public PeakBounds RawPeakBounds { get; }

            public PeakBounds AlignedPeakBounds
            {
                get; private set;
            }

            public double? Score { get; }
            public bool ManuallyIntegrated { get; }
            public bool Accepted { get; private set; }
            public Peak ChangeAccepted(bool accepted)
            {
                if (accepted == Accepted)
                {
                    return this;
                }
                return ChangeProp(ImClone(this), im => im.Accepted = accepted);
            }

            public bool Exemplary { get; private set; }

            public Peak ChangeExemplary(bool value)
            {
                return ChangeProp(ImClone(this), im => im.Exemplary = value);
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

                var standardDeviations = new List<double>();
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
                            if (!resultFileInfos.TryGetValue(peptideResult.ResultFile.ChromFileInfo.FilePath,
                                    out var peakResultFile))
                            {
                                // Shouldn't happen
                                continue;
                            }

                            bool manuallyIntegrated = IsManualIntegrated(molecule, peptideResult.ResultFile);
                            if (data.Parameters.ManualPeakTreatment == ManualPeakTreatment.SKIP && manuallyIntegrated)
                            {
                                continue;
                            }

                            var rawPeakBounds = GetRawPeakBounds(molecule,
                                peptideResult.ResultFile.Replicate.ReplicateIndex,
                                peptideResult.ResultFile.ChromFileInfoId);

                            var peakFeatureStatistics = data.ResultsHandler?.GetPeakFeatureStatistics(molecule.Peptide,
                                peptideResult.ResultFile.ChromFileInfoId);
                            var peak = new Peak(peakResultFile, rawPeakBounds, peakFeatureStatistics?.BestScore,
                                manuallyIntegrated);
                            peaks.Add(peak);
                        }

                        var row = MakeRow(data.Parameters, peptide, peaks);
                        var standardDeviation = row.Peaks.Select(peak => peak.Value.AlignedPeakBounds.ApexTime).StandardDeviation();
                        if (!double.IsNaN(standardDeviation))
                        {
                            standardDeviations.Add(standardDeviation);
                        }
                        yield return row;
                    }
                }

                if (standardDeviations.Count > 0)
                {
                    RowStatisticsAvailable?.Invoke(new RowStatistics(standardDeviations.Mean()));
                }
            }

            public Action<RowStatistics> RowStatisticsAvailable;
        }
 
        private static Row MakeRow(Parameters parameters, Model.Databinding.Entities.Peptide peptide, List<Peak> peaks)
        {
            var outliers = new List<Peak>();
            var candidates = new List<Peak>();
            var core = new List<Peak>();
            foreach (var peak in peaks)
            {
                if (peak.AlignedPeakBounds == null)
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
                    var retentionTimes = new Statistics(core.Select(peak => peak.AlignedPeakBounds.ApexTime));
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
                        if (Math.Abs(peak.AlignedPeakBounds.ApexTime - meanRetentionTime) < stdDevRetentionTime * parameters.StandardDeviationsCutoff)
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
            var allPeaks = new List<Peak>();
            for (int i = 0; i < core.Count; i++)
            {
                var peak = core[i];
                peak = peak.ChangeAccepted(true);
                if (i == 0 || !parameters.ImputeFromBestPeakOnly)
                {
                    peak = peak.ChangeExemplary(true);
                }

                allPeaks.Add(peak);
            }
            allPeaks.AddRange(outliers);
            
            return new Row(peptide, allPeaks);
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
            public bool ImputeFromBestPeakOnly { get; private set; }

            public Parameters ChangeImputFromBestPeakOnly(bool value)
            {
                return ChangeProp(ImClone(this), im => im.ImputeFromBestPeakOnly = value);
            }
            public AlignmentTarget AlignmentTarget { get; private set; }
            public Parameters ChangeAlignmentTarget(AlignmentTarget alignmentTarget)
            {
                return ChangeProp(ImClone(this), im =>
                {
                    im.AlignmentTarget = alignmentTarget;
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
                       ImputeFromBestPeakOnly == other.ImputeFromBestPeakOnly &&
                       Equals(AlignmentTarget, other.AlignmentTarget) &&
                       Equals(PeakScoringModel, other.PeakScoringModel) &&
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
                    hashCode = (hashCode * 397) ^ ImputeFromBestPeakOnly.GetHashCode();
                    hashCode = (hashCode * 397) ^ ManualPeakTreatment.GetHashCode();
                    hashCode = (hashCode * 397) ^ (AlignmentTarget != null ? AlignmentTarget.GetHashCode() : 0);
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
            private AllAlignments _allAlignments;

            public Data(Parameters parameters, MProphetResultsHandler resultsHandler, AllAlignments allAlignments)
            {
                Parameters = parameters;
                ResultsHandler = resultsHandler;
                _allAlignments = allAlignments;
            }

            public Parameters Parameters { get; }

            public AlignmentFunction GetAlignmentFunction(MsDataFileUri msDataFileUri)
            {
                return _allAlignments?.GetAlignmentFunction(msDataFileUri) ?? AlignmentFunction.IDENTITY;
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
                MProphetResultsHandler resultsHandler = ScoringProducer.Instance.GetResult(inputs, new ScoringProducer.Parameters(parameter.Document, parameter.PeakScoringModel));
                AllAlignments allAlignments = AllAlignmentsProducer.INSTANCE.GetResult(inputs,
                    new AllAlignmentsProducer.Parameter(parameter.Document, parameter.AlignmentTarget));
                return new Data(parameter, resultsHandler, allAlignments);
            }

            public override IEnumerable<WorkOrder> GetInputs(Parameters parameter)
            {
                SrmDocument document = parameter.Document;
                if (document.MeasuredResults == null)
                {
                    yield break;
                }

                if (parameter.AlignmentTarget != null)
                {
                    yield return AllAlignmentsProducer.INSTANCE.MakeWorkOrder(
                        new AllAlignmentsProducer.Parameter(document, parameter.AlignmentTarget));
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
            var parameters =
                new Parameters(SkylineWindow.Document).ChangeAlignmentTarget(alignmentControl.AlignmentTarget);
            SkylineWindow.AlignmentTarget = alignmentControl.AlignmentTarget;

            parameters = parameters.ChangeManualPeakTreatment(
                comboManualPeaks.SelectedItem as ManualPeakTreatment ?? ManualPeakTreatment.SKIP);
            parameters = parameters.ChangeImputFromBestPeakOnly(comboImputeBoundariesFrom.SelectedIndex == 0);
            var scoringModel = comboScoringModel.SelectedItem as PeakScoringModelSpec;
            numericUpDownCoreResults.Enabled = tbxCoreScoreCutoff.Enabled = tbxStandardDeviationsCutoff.Enabled = scoringModel != null;

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
            foreach (var corePeak in row.Peaks.Values.Where(peak => peak.Exemplary))
            {
                corePeakBounds.Add(corePeak.AlignedPeakBounds);
            }

            if (!corePeakBounds.Any())
            {
                return document;
            }

            var meanStartTime = corePeakBounds.Select(peak => peak.StartTime).Mean();
            var meanEndTime = corePeakBounds.Select(peak => peak.EndTime).Mean();
            foreach (var outlierPeak in row.Peaks.Values.Where(peak => !peak.Accepted))
            {
                var resultFileInfo = outlierPeak.ResultFileInfo;
                var newStartTime = resultFileInfo.AlignmentFunction.GetX(meanStartTime);
                var newEndTime = resultFileInfo.AlignmentFunction.GetX(meanEndTime);
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

        public static PeakBounds GetPeakBounds(Model.Databinding.Entities.Peptide peptide, Peak peak)
        {
            var alignmentFunction = peak.ResultFileInfo.AlignmentFunction;
            var rawPeakBounds = GetRawPeakBounds(peptide.DocNode, peak.ResultFileInfo.ResultFileOption.ReplicateIndex,
                peak.ResultFileInfo.ChromFileInfoId).Align(alignmentFunction);
            var apexTime = peak.AlignedPeakBounds.ApexTime;
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

        public static PeakBounds GetRawPeakBounds(PeptideDocNode peptideDocNode, int replicateIndex,
            ChromFileInfoId chromFileInfoId)
        {
            var peptideChromInfo = peptideDocNode.GetSafeChromInfo(replicateIndex)
                .FirstOrDefault(chromInfo => ReferenceEquals(chromInfo.FileId, chromFileInfoId));
            if (peptideChromInfo?.RetentionTime == null)
            {
                return null;
            }

            double apexTime = peptideChromInfo.RetentionTime.Value;
            double startTime = apexTime;
            double endTime = apexTime;
            foreach (var transitionGroup in peptideDocNode.TransitionGroups)
            {
                foreach (var chromInfo in transitionGroup.GetSafeChromInfo(replicateIndex))
                {
                    if (!ReferenceEquals(chromFileInfoId, chromInfo.FileId))
                    {
                        continue;
                    }

                    if (chromInfo.StartRetentionTime.HasValue)
                    {
                        startTime = Math.Min(startTime, chromInfo.StartRetentionTime.Value);
                    }

                    if (chromInfo.EndRetentionTime.HasValue)
                    {
                        endTime = Math.Min(endTime, chromInfo.EndRetentionTime.Value);
                    }
                }
            }

            return new PeakBounds(apexTime, startTime, endTime);
        }

        public class PeakBounds
        {
            public PeakBounds(double apexTime, double startTime, double endTime)
            {
                ApexTime = apexTime;
                StartTime = startTime;
                EndTime = endTime;
            }
            public double ApexTime { get; }
            public double StartTime { get; }
            public double EndTime { get; }

            public PeakBounds Align(AlignmentFunction alignmentFunction)
            {
                return new PeakBounds(alignmentFunction.GetY(ApexTime), alignmentFunction.GetY(StartTime),
                    alignmentFunction.GetY(EndTime));
            }

            public PeakBounds ReverseAlign(AlignmentFunction alignmentFunction)
            {
                return new PeakBounds(alignmentFunction.GetX(ApexTime), alignmentFunction.GetX(StartTime),
                    alignmentFunction.GetX(EndTime));
            }

            public static PeakBounds Average(IEnumerable<PeakBounds> peakBounds)
            {
                var startTimes = new List<double>();
                var endTimes = new List<double>();
                var apexes = new List<double>();
                foreach (var bounds in peakBounds)
                {
                    startTimes.Add(bounds.StartTime);
                    endTimes.Add(bounds.EndTime);
                    apexes.Add(bounds.ApexTime);
                }

                if (startTimes.Count == 0)
                {
                    return null;
                }

                return new PeakBounds(apexes.Mean(), startTimes.Mean(), endTimes.Mean());
            }

            public override string ToString()
            {
                return string.Format("[{0},{1}]", StartTime.ToString(Formats.RETENTION_TIME),
                    EndTime.ToString(Formats.RETENTION_TIME));
            }
        }

        public class PeakSummary
        {
            public PeakSummary(IEnumerable<PeakBounds> peaks)
            {
                var startTimes = new List<double>();
                var endTimes = new List<double>();
                var apexes = new List<double>();
                foreach (var bounds in peaks)
                {
                    startTimes.Add(bounds.StartTime);
                    endTimes.Add(bounds.EndTime);
                    apexes.Add(bounds.ApexTime);
                }

                Count = startTimes.Count;
                StartTimes = new RetentionTimeSummary(new Statistics(startTimes));
                ApexTimes = new RetentionTimeSummary(new Statistics(apexes));
                EndTimes = new RetentionTimeSummary(new Statistics(endTimes));
            }
            public int Count { get; }
            public RetentionTimeSummary StartTimes
            {
                get;
            }
            public RetentionTimeSummary EndTimes { get; }
            public RetentionTimeSummary ApexTimes { get; }
            public override string ToString()
            {
                if (Count == 1)
                {
                    return string.Format("[{0},{1}]", StartTimes, EndTimes);
                }
                return string.Format("{0} peaks [{1},{2}]", Count, StartTimes, EndTimes);
            }
        }

        public class RowStatistics
        {
            public RowStatistics(double meanStandardDeviation)
            {
                MeanStandardDeviation = meanStandardDeviation;
            }
            public double MeanStandardDeviation { get; private set; }
        }
    }
}
