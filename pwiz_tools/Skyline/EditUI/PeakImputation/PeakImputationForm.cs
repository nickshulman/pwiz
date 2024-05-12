using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MathNet.Numerics.Distributions;
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

        private void OnDataAvailable()
        {
            if (_receiver.TryGetCurrentProduct(out var data))
            {
                Console.Out.WriteLine("OnDataAvailable: Not Available");
                _rowSource.Data = data;
                tbxMeanStandardDeviation.Text = data.GetMeanStandardDeviation()?.ToString() ?? string.Empty;
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
            var scoringModel = comboScoringModel.SelectedItem as PeakScoringModelSpec;
            if (scoringModel == null)
            {
                groupBoxCutoff.Enabled = false;
            }
            else
            {
                groupBoxCutoff.Enabled = true;
                radioQValue.Enabled = radioPValue.Enabled = !Equals(scoringModel, LegacyScoringModel.DEFAULT_MODEL);
            }
        }

        private static IEnumerable<ResultFileInfo> GetResultFileInfos(SrmDocument document,
            AllAlignments allAlignments)
        {
            var measuredResults = document.MeasuredResults;
            if (measuredResults == null)
            {
                yield break;
            }
            foreach (var resultFileOption in GetResultFileOptions(document))
            {
                var chromFileInfoId = measuredResults.Chromatograms[resultFileOption.ReplicateIndex]
                    .FindFile(resultFileOption.Path);
                var alignmentFunction = allAlignments?.GetAlignmentFunction(resultFileOption.Path) ?? AlignmentFunction.IDENTITY;
                yield return new ResultFileInfo(resultFileOption, chromFileInfoId, alignmentFunction);
            }

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
            public Row(SkylineDataSchema dataSchema, RowData rowData, IEnumerable<RatedPeak> ratedPeaks)
            {
                Peptide = new Model.Databinding.Entities.Peptide(dataSchema, rowData.PeptideIdentityPath);
                Peaks = new Dictionary<ResultFileOption, RatedPeak>();
                var allPeakBounds = new List<PeakBounds>();
                var acceptedPeakBounds = new List<PeakBounds>();
                var exemplaryPeakBounds = new List<PeakBounds>();
                foreach (var peak in ratedPeaks)
                {
                    Peaks[peak.ResultFileInfo.ResultFileOption] = peak;
                    var peakBounds = peak.AlignedPeakBounds;
                    if (peakBounds != null)
                    {
                        allPeakBounds.Add(peakBounds);
                        if (peak.Verdict >= PeakVerdict.accepted)
                        {
                            acceptedPeakBounds.Add(peakBounds);
                        }

                        if (peak.Verdict >= PeakVerdict.exemplary)
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
            public Dictionary<ResultFileOption, RatedPeak> Peaks { get; }
        }

        public class RowData
        {
            public RowData(IdentityPath identityPath, IEnumerable<Peak> peaks)
            {
                PeptideIdentityPath = identityPath;
                Peaks = ImmutableList.ValueOf(peaks);
            }

            public IdentityPath PeptideIdentityPath { get; }

            public ImmutableList<Peak> Peaks { get; }
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
            public double? Percentile { get; private set; }

            public Peak ChangePercentile(double? value)
            {
                return ChangeProp(ImClone(this), im => im.Percentile = value);
            }

            public double? PValue { get; private set; }

            public Peak ChangePValue(double? value)
            {
                return ChangeProp(ImClone(this), im => im.PValue = value);
            }

            public double? QValue { get; private set; }

            public Peak ChangeQValue(double? value)
            {
                return ChangeProp(ImClone(this), im => im.QValue = value);
            }
        }

        public class RatedPeak
        {
            private Peak _peak;
            public RatedPeak(Peak peak, PeakVerdict verdict)
            {
                _peak = peak;
                Verdict = verdict;
            }

            public PeakVerdict Verdict { get; }

            public ResultFileInfo ResultFileInfo
            {
                get { return _peak.ResultFileInfo; }
            }
            public PeakBounds RawPeakBounds
            {
                get { return _peak.RawPeakBounds; }
            }

            public PeakBounds AlignedPeakBounds
            {
                get { return _peak.AlignedPeakBounds; }
            }

            public double? Score
            {
                get
                {
                    return _peak.Score;
                }
            }

            public bool ManuallyIntegrated
            {
                get
                {
                    return _peak.ManuallyIntegrated;
                }
            }

            public bool Exemplary
            {
                get { return Verdict >= PeakVerdict.exemplary; }
            }

            public bool Accepted
            {
                get { return Verdict >= PeakVerdict.accepted; }
            }

            [Format(Formats.Percent)]
            public double? Percentile
            {
                get { return _peak.Percentile; }
            }

            public double? PValue
            {
                get { return _peak.PValue; }
            }
            public double? QValue
            {
                get { return _peak.QValue; }
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
                if (Data == null)
                {
                    yield break;
                }

                foreach (var rowData in Data.Rows)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    yield return RatePeak(Data, DataSchema, rowData);
                }
            }

            public Action<RowStatistics> RowStatisticsAvailable;
        }
 
        private static RowData MakeRow(IdentityPath peptideIdentityPath, List<Peak> peaks)
        {
            return new RowData(peptideIdentityPath, peaks);
        }

        private static Row RatePeak(Data data, SkylineDataSchema dataSchema, RowData rowData)
        {
            var parameters = data.Parameters;
            var outliers = new List<Peak>();
            var candidates = new List<Peak>();
            var core = new List<Peak>();
            foreach (var peak in rowData.Peaks)
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
                    if (peak.Score.HasValue && core.Count < parameters.MinCoreCount)
                    {
                        core.Add(peak);
                        continue;
                    }

                    if (parameters.ScoreCutoff.HasValue)
                    {
                        bool accepted = false;
                        switch (parameters.CutoffType)
                        {
                            case CutoffTypeEnum.score:
                                accepted = peak.Score >= parameters.ScoreCutoff;
                                break;
                            case CutoffTypeEnum.pValue:
                                accepted = peak.PValue <= parameters.ScoreCutoff;
                                break;
                            case CutoffTypeEnum.percentile:
                                accepted = peak.Percentile >= parameters.ScoreCutoff;
                                break;
                            case CutoffTypeEnum.qValue:
                                accepted = peak.QValue <= parameters.ScoreCutoff;
                                break;
                        }

                        if (accepted)
                        {
                            core.Add(peak);
                            continue;
                        }
                    }

                    newCandidates.Add(peak);
                }

                candidates = newCandidates;
            }

            if (parameters.RetentionTimeDeviationCutoff.HasValue)
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
                        if (Math.Abs(peak.AlignedPeakBounds.ApexTime - meanRetentionTime) < parameters.RetentionTimeDeviationCutoff)
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
            var ratedPeaks = new List<RatedPeak>();
            for (int i = 0; i < core.Count; i++)
            {
                var peak = core[i];
                var rating = PeakVerdict.accepted;
                if (i == 0 || !parameters.ImputeFromBestPeakOnly)
                {
                    rating = PeakVerdict.exemplary;
                }
                ratedPeaks.Add(new RatedPeak(peak, rating));
            }
            ratedPeaks.AddRange(outliers.Select(outlier=>new RatedPeak(outlier, PeakVerdict.rejected)));

            return new Row(dataSchema, rowData, ratedPeaks);
        }



        private static bool IsManualIntegrated(PeptideDocNode peptideDocNode, int replicateIndex, ChromFileInfoId fileId)
        {
            foreach (var transitionGroupDocNode in peptideDocNode.TransitionGroups)
            {
                foreach (var transitionGroupChromInfo in transitionGroupDocNode.GetSafeChromInfo(replicateIndex))
                {
                    if (ReferenceEquals(fileId, transitionGroupChromInfo.FileId) &&
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
            public CutoffTypeEnum CutoffType { get; private set; }
            public double? ScoreCutoff { get; private set; }
            public double? RetentionTimeDeviationCutoff { get; private set; }

            public Parameters ChangeScoringModel(PeakScoringModelSpec model, int minCoreCount, CutoffTypeEnum cutoffType, double? scoreCutoff,
                double? retentionTimeDeviationCutoff)
            {
                return ChangeProp(ImClone(this), im =>
                {
                    im.PeakScoringModel = model;
                    im.MinCoreCount = minCoreCount;
                    im.CutoffType = cutoffType;
                    im.ScoreCutoff = scoreCutoff;
                    im.RetentionTimeDeviationCutoff = retentionTimeDeviationCutoff;
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
                       Nullable.Equals(RetentionTimeDeviationCutoff, other.RetentionTimeDeviationCutoff);
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
                    hashCode = (hashCode * 397) ^ RetentionTimeDeviationCutoff.GetHashCode();
                    return hashCode;
                }
            }
        }

        public class Data
        {
            private AllAlignments _allAlignments;
            private ScoreQValueMap _scoreQValueMap;

            public Data(Parameters parameters, ScoringResults scoringResults, AllAlignments allAlignments, IEnumerable<RowData> rows)
            {
                Parameters = parameters;
                ScoringResults = scoringResults;
                _allAlignments = allAlignments;
                var rowList = rows.ToList();
                if (Equals(parameters.Document.Value.Settings.PeptideSettings.Integration.PeakScoringModel,
                        parameters.PeakScoringModel))
                {
                    if (!Equals(LegacyScoringModel.DEFAULT_MODEL, parameters.PeakScoringModel))
                    {
                        _scoreQValueMap = scoringResults?.ReintegratedDocument.Settings.PeptideSettings.Integration
                            .ScoreQValueMap;
                    }
                }
                SortedScores = ImmutableList.ValueOf(rowList.SelectMany(row =>
                    row.Peaks.Select(peak => peak.Score).OfType<double>()).OrderBy(score => score));
                Rows = ImmutableList.ValueOf(rowList.Select(FillInScores));
            }

            public Parameters Parameters { get; }

            public AlignmentFunction GetAlignmentFunction(MsDataFileUri msDataFileUri)
            {
                return _allAlignments?.GetAlignmentFunction(msDataFileUri) ?? AlignmentFunction.IDENTITY;
            }

            public ScoringResults ScoringResults { get; }

            public ImmutableList<RowData> Rows { get; }

            public double? GetMeanStandardDeviation()
            {
                var standardDeviations = new List<double>();
                foreach (var row in Rows)
                {
                    var peakApexes = row.Peaks.Select(peak => peak.AlignedPeakBounds?.ApexTime).OfType<double>()
                        .ToList();
                    if (peakApexes.Count > 1)
                    {
                        standardDeviations.Add(peakApexes.StandardDeviation());
                    }
                }

                if (standardDeviations.Count == 0)
                {
                    return null;
                }
                return standardDeviations.Mean();
            }

            public ImmutableList<double> SortedScores { get; }

            public double? GetPercentileOfScore(double score)
            {
                if (SortedScores.Count == 0)
                {
                    return null;
                }
                var index = CollectionUtil.BinarySearch(SortedScores, score);
                if (index >= 0)
                {
                    return (double)index / SortedScores.Count;
                }
                index = ~index;

                if (index <= 0)
                {
                    return SortedScores[0];
                }

                if (index >= SortedScores.Count - 1)
                {
                    return SortedScores[SortedScores.Count - 1];
                }

                double prev = SortedScores[index];
                double next = SortedScores[index + 1];
                return (index + (score - prev) / (next - prev)) / SortedScores.Count;
            }

            public double? GetScoreAtPercentile(double percentile)
            {
                if (SortedScores.Count == 0)
                {
                    return null;
                }

                double doubleIndex = percentile * SortedScores.Count;
                if (doubleIndex <= 0)
                {
                    return SortedScores[0];
                }

                if (doubleIndex >= SortedScores.Count - 1)
                {
                    return SortedScores[SortedScores.Count - 1];
                }

                int prevIndex = (int)Math.Floor(doubleIndex);
                int nextIndex = (int)Math.Ceiling(doubleIndex);
                var prevValue = SortedScores[prevIndex];
                if (prevIndex == nextIndex)
                {
                    return prevValue;
                }
                var nextValue = SortedScores[nextIndex];
                return prevValue * (nextIndex - doubleIndex) + nextValue * (doubleIndex - prevIndex);
            }

            public RowData FillInScores(RowData row)
            {
                var newPeaks = row.Peaks.ToList();
                for (int iPeak = 0; iPeak < newPeaks.Count; iPeak++)
                {
                    var peak = newPeaks[iPeak];
                    if (!peak.Score.HasValue)
                    {
                        continue;
                    }

                    peak = peak.ChangePercentile(GetPercentileOfScore(peak.Score.Value));
                    peak = peak.ChangeQValue(_scoreQValueMap?.GetQValue(peak.Score));
                    if (!Equals(Parameters.PeakScoringModel, LegacyScoringModel.DEFAULT_MODEL))
                    {
                        peak = peak.ChangePValue(ZScoreToPValue(peak.Score.Value));
                    }
                    newPeaks[iPeak] = peak;
                }

                if (ArrayUtil.ReferencesEqual(newPeaks, row.Peaks))
                {
                    return row;
                }

                return new RowData(row.PeptideIdentityPath, newPeaks);
            }
        }

        private class DataProducer : Producer<Parameters, Data>
        {
            public static readonly DataProducer Instance = new DataProducer();
            public override Data ProduceResult(ProductionMonitor productionMonitor, Parameters parameter, IDictionary<WorkOrder, object> inputs)
            {
                ScoringResults scoringResults = ScoringProducer.Instance.GetResult(inputs, new ScoringProducer.Parameters(parameter.Document, parameter.PeakScoringModel, parameter.ManualPeakTreatment == ManualPeakTreatment.OVERWRITE));
                AllAlignments allAlignments = AllAlignmentsProducer.INSTANCE.GetResult(inputs,
                    new AllAlignmentsProducer.Parameter(parameter.Document, parameter.AlignmentTarget));
                var rows = ImmutableList.ValueOf(GetRows(productionMonitor.CancellationToken, parameter, scoringResults,
                    GetResultFileInfos(parameter.Document.Value, allAlignments)));
                return new Data(parameter, scoringResults, allAlignments, rows);
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
                        new ScoringProducer.Parameters(parameter.Document, parameter.PeakScoringModel, parameter.ManualPeakTreatment == ManualPeakTreatment.OVERWRITE));
                }
            }

            private IEnumerable<RowData> GetRows(CancellationToken cancellationToken, Parameters parameters, ScoringResults scoringResults, IEnumerable<ResultFileInfo> resultFileInfos)
            {
                var document = scoringResults.ReintegratedDocument ?? parameters.Document.Value;
                var measuredResults = document.MeasuredResults;
                if (measuredResults == null)
                {
                    yield break;
                }

                var resultFileInfoDict =
                    resultFileInfos.ToDictionary(resultFileInfo => ReferenceValue.Of(resultFileInfo.ChromFileInfoId));
                foreach (var moleculeGroup in document.MoleculeGroups)
                {
                    foreach (var molecule in moleculeGroup.Molecules)
                    {
                        if (molecule.GlobalStandardType != null)
                        {
                            continue;
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        var peptideIdentityPath = new IdentityPath(moleculeGroup.PeptideGroup, molecule.Peptide);
                        var peaks = new List<Peak>();
                        for (int replicateIndex = 0; replicateIndex < measuredResults.Chromatograms.Count; replicateIndex++) 
                        {
                            foreach (var peptideChromInfo in molecule.GetSafeChromInfo(replicateIndex))
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (!resultFileInfoDict.TryGetValue(peptideChromInfo.FileId
                                        ,
                                        out var peakResultFile))
                                {
                                    // Shouldn't happen
                                    continue;
                                }
                                bool manuallyIntegrated = IsManualIntegrated(molecule, replicateIndex, peptideChromInfo.FileId);

                                if (manuallyIntegrated)
                                {
                                    if (parameters.ManualPeakTreatment == ManualPeakTreatment.SKIP)
                                    {
                                        continue;
                                    }
                                }

                                var rawPeakBounds = GetRawPeakBounds(molecule,
                                    replicateIndex,
                                    peptideChromInfo.FileId);

                                var peakFeatureStatistics = scoringResults.ResultsHandler?.GetPeakFeatureStatistics(molecule.Peptide,
                                    peptideChromInfo.FileId);
                                var peak = new Peak(peakResultFile, rawPeakBounds, peakFeatureStatistics?.BestScore,
                                    manuallyIntegrated);
                                peaks.Add(peak);
                            }

                        }
                        var row = MakeRow(peptideIdentityPath, peaks);
                        yield return row;
                    }
                }
            }
        }

        private void SettingsControlChanged(object sender, EventArgs e)
        {
            OnDocumentChanged();
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
            numericUpDownCoreResults.Enabled = tbxCoreScoreCutoff.Enabled = tbxRtDeviationCutoff.Enabled = scoringModel != null;

            if (scoringModel != null)
            {
                parameters = parameters.ChangeScoringModel(scoringModel,
                    Convert.ToInt32(numericUpDownCoreResults.Value), CutoffType, GetDoubleValue(tbxCoreScoreCutoff),
                    GetDoubleValue(tbxRtDeviationCutoff));
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

        private CutoffTypeEnum _cutoffType;

        public CutoffTypeEnum CutoffType 
        {
            get
            {
                if (radioPercentile.Checked)
                {
                    return CutoffTypeEnum.percentile;
                }

                if (radioQValue.Checked)
                {
                    return CutoffTypeEnum.qValue;
                }

                if (radioPValue.Checked)
                {
                    return CutoffTypeEnum.pValue;
                }

                return CutoffTypeEnum.score;
            }
            set
            {
                switch (value)
                {
                    case CutoffTypeEnum.percentile:
                        radioPercentile.Checked = true;
                        break;
                    case CutoffTypeEnum.qValue:
                        radioQValue.Checked = true;
                        break;
                    case CutoffTypeEnum.pValue:
                        radioPValue.Checked = true;
                        break;
                    default:
                        radioScore.Checked = true;
                        break;
                }
            }
        }

        private void CutoffTypeChanged(object sender, EventArgs e)
        {
            var newCutoffType = CutoffType;
            if (newCutoffType == _cutoffType)
            {
                return;
            }
            if (_inChange)
            {
                return;
            }

            try
            {
                _inChange = true;
                var cutoffValue = GetDoubleValue(tbxCoreScoreCutoff);
                if (cutoffValue.HasValue)
                {
                    var score = ConvertToScore(cutoffValue.Value, _cutoffType);
                    if (score.HasValue && !double.IsNaN(score.Value))
                    {
                        var newCutoff = ConvertFromScore(score.Value, newCutoffType);
                        if (newCutoff.HasValue && !double.IsNaN(newCutoff.Value))
                        {
                            tbxCoreScoreCutoff.Text = newCutoff.ToString();
                        }
                    }
                }
                _cutoffType = newCutoffType;
                OnDocumentChanged();
            }
            finally
            {
                _inChange = false;
            }

        }

        public enum CutoffTypeEnum
        {
            score,
            pValue,
            qValue,
            percentile,
        }

        private double? ConvertToScore(double value, CutoffTypeEnum cutoffType)
        {
            switch (cutoffType)
            {
                case CutoffTypeEnum.pValue:
                    return PValueToZScore(value);
                case CutoffTypeEnum.percentile:
                    return _rowSource.Data?.GetScoreAtPercentile(value);
                case CutoffTypeEnum.qValue:
                    var integration = SkylineWindow.DocumentUI.Settings.PeptideSettings.Integration;
                    return integration.ScoreQValueMap.GetZScore(value);
                case CutoffTypeEnum.score:
                    return value;
            }

            return null;
        }

        private double? ConvertFromScore(double score, CutoffTypeEnum cutoffType)
        {
            switch (cutoffType)
            {
                case CutoffTypeEnum.pValue:
                    return ZScoreToPValue(score);
                case CutoffTypeEnum.percentile:
                    return _rowSource.Data?.GetPercentileOfScore(score);
                case CutoffTypeEnum.qValue:
                    var integration = SkylineWindow.DocumentUI.Settings.PeptideSettings.Integration;
                    return integration.ScoreQValueMap.GetQValue(score);
                case CutoffTypeEnum.score:
                    return score;
            }

            return null;
        }

        private static double ZScoreToPValue(double zScore)
        {
            return 1 - Normal.CDF(0, 1, zScore);
        }

        private static double PValueToZScore(double pValue)
        {
            return Normal.InvCDF(0, 1, 1 - pValue);
        }

        private IEnumerable<double> GetAllScores()
        {

            var data = _rowSource.Data;
            if (data == null)
            {
                return Array.Empty<double>();
            }

            return data.Rows.SelectMany(row => row.Peaks.Select(peak => peak.Score)).OfType<double>();
        }

        public enum PeakVerdict
        {
            rejected,
            accepted,
            exemplary
        }
    }
}
