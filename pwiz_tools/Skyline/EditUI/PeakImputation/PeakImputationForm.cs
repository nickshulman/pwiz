using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MathNet.Numerics.Statistics;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.PeakImputation;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using Statistics = pwiz.Skyline.Util.Statistics;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public partial class PeakImputationForm : DataboundGridForm
    {
        private PeakRowSource _rowSource;
        private readonly Receiver<PeakImputationData.Parameters, PeakImputationData> _receiver;

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

            _receiver = PeakImputationData.PRODUCER.RegisterCustomer(this, OnDataAvailable);
        }

        private void OnDataAvailable()
        {
            if (_receiver.TryGetCurrentProduct(out var data))
            {
                Console.Out.WriteLine("OnDataAvailable: Available");
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
            public Row(SkylineDataSchema dataSchema, MoleculePeaks rowData, IEnumerable<RatedPeak> ratedPeaks)
            {
                Peptide = new Model.Databinding.Entities.Peptide(dataSchema, rowData.PeptideIdentityPath);
                Peaks = new Dictionary<string, RatedPeak>();
                var allPeakBounds = new List<ApexPeakBounds>();
                var acceptedPeakBounds = new List<ApexPeakBounds>();
                var exemplaryPeakBounds = new List<ApexPeakBounds>();
                foreach (var peak in ratedPeaks)
                {
                    Peaks[peak.ResultFileInfo.Path.GetFileNameWithoutExtension()] = peak;
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
            public Dictionary<string, RatedPeak> Peaks { get; }
        }


        public class RatedPeak
        {
            private AlignedPeak _peak;

            public RatedPeak(AlignedPeak peak, PeakVerdict verdict)
            {
                _peak = peak;
                Verdict = verdict;
            }

            public PeakVerdict Verdict { get; }

            public ResultFileInfo ResultFileInfo
            {
                get { return _peak.ResultFileInfo; }
            }

            public ApexPeakBounds RawPeakBounds
            {
                get { return _peak.RawPeakBounds; }
            }

            public ApexPeakBounds AlignedPeakBounds
            {
                get { return _peak.AlignedPeakBounds; }
            }

            public double? Score
            {
                get { return _peak.Score; }
            }

            public bool ManuallyIntegrated
            {
                get { return _peak.ManuallyIntegrated; }
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

        class PeakRowSource : SkylineObjectList<Row>
        {
            private PeakImputationData _data;

            public PeakRowSource(SkylineDataSchema dataSchema) : base(dataSchema)
            {
            }

            public PeakImputationData Data
            {
                get { return _data; }
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

                foreach (var rowData in Data.MoleculePeaks)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    yield return RatePeak(Data, DataSchema, rowData);
                }
            }
        }

        private static Row RatePeak(PeakImputationData data, SkylineDataSchema dataSchema, MoleculePeaks rowData)
        {
            var parameters = data.Params;
            var outliers = new List<AlignedPeak>();
            var candidates = new List<AlignedPeak>();
            var core = new List<AlignedPeak>();
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
                var newCandidates = new List<AlignedPeak>();
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

                    var newCandidates = new List<AlignedPeak>();
                    foreach (var peak in candidates)
                    {
                        if (Math.Abs(peak.AlignedPeakBounds.ApexTime - meanRetentionTime) <
                            parameters.RetentionTimeDeviationCutoff)
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

            ratedPeaks.AddRange(outliers.Select(outlier => new RatedPeak(outlier, PeakVerdict.rejected)));

            return new Row(dataSchema, rowData, ratedPeaks);
        }





private void SettingsControlChanged(object sender, EventArgs e)
        {
            OnDocumentChanged();
        }

        private void UpdateData()
        {
            var parameters =
                new PeakImputationData.Parameters(SkylineWindow.Document).ChangeAlignmentTarget(alignmentControl.AlignmentTarget);
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
            var corePeakBounds = new List<ApexPeakBounds>();
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
                    var chromatogramSet = document.MeasuredResults.FindChromatogramSet(resultFileInfo.ReplicateFileId.ChromatogramSetId);
                    document = document.ChangePeak(identityPath, chromatogramSet.Name,
                        resultFileInfo.Path, null, newStartTime, newEndTime, UserSet.MATCHED, null,
                        false);
                    changeCount++;
                }
            }

            return document;
        }

        public static ApexPeakBounds GetPeakBounds(Model.Databinding.Entities.Peptide peptide, AlignedPeak peak)
        {
            var alignmentFunction = peak.ResultFileInfo.AlignmentFunction;

            var rawPeakBounds = GetRawPeakBounds(peptide.DocNode, peak.ResultFileInfo.ReplicateIndex,
                peak.ResultFileInfo.ChromFileInfoId).Align(alignmentFunction);
            var apexTime = peak.AlignedPeakBounds.ApexTime;
            double? minRawStartTime = null;
            double? maxRawEndTime = null;
            foreach (var transitionGroup in peptide.DocNode.TransitionGroups)
            {
                foreach (var chromInfo in transitionGroup.GetSafeChromInfo(peak.ResultFileInfo.ReplicateIndex))
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

            return new ApexPeakBounds(apexTime, apexTime - minStartTime, maxEndTime - apexTime);
        }

        public static ApexPeakBounds GetRawPeakBounds(PeptideDocNode peptideDocNode, int replicateIndex,
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

            return new ApexPeakBounds(apexTime, startTime, endTime);
        }

        public class PeakSummary
        {
            public PeakSummary(IEnumerable<ApexPeakBounds> peaks)
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
                    return PeakImputationData.PValueToZScore(value);
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
                    return PeakImputationData.ZScoreToPValue(score);
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

        public enum PeakVerdict
        {
            rejected,
            accepted,
            exemplary
        }
    }
}
