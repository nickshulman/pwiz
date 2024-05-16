/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using pwiz.Common.SystemUtil;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.SeqNode;
using pwiz.Skyline.EditUI.PeakImputation;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Irt;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Controls.Graphs
{
    public sealed class RTLinearRegressionGraphPane : SummaryGraphPane, IUpdateGraphPaneController, IDisposable, ITipDisplayer
    {
        public static ReplicateDisplay ShowReplicate
        {
            get
            {
                return Helpers.ParseEnum(Settings.Default.ShowRegressionReplicateEnum, ReplicateDisplay.all);
            }
        }

        public static readonly Color COLOR_REFINED = Color.DarkBlue;
        public static readonly Color COLOR_LINE_REFINED = Color.Black;
        public static readonly Color COLOR_LINE_PREDICT = Color.DarkGray;
        public static readonly Color COLOR_OUTLIERS = Color.BlueViolet;
        public static readonly Color COLOR_LINE_ALL = Color.BlueViolet;

        private GraphData _data;
        private NodeTip _tip;
        private CancellationTokenSource _cancellationTokenSource;
        public IProgressBar _progressBar;
        private Receiver<RegressionSettings, GraphData> _receiver;
        private bool _pendingUpdate;

        public RTLinearRegressionGraphPane(GraphSummary graphSummary, bool runToRun)
            : base(graphSummary)
        {
            XAxis.Title.Text = GraphsResources.RTLinearRegressionGraphPane_RTLinearRegressionGraphPane_Score;
            RunToRun = runToRun;
            Settings.Default.RTScoreCalculatorList.ListChanged += RTScoreCalculatorList_ListChanged;
            AllowDisplayTip = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _receiver = GRAPH_DATA_PRODUCER.RegisterCustomer(GraphSummary, ProductAvailableAction);
            _receiver.ProgressChange += UpdateProgressHandler;

        }

        private void UpdateProgressHandler()
        {
            if (_receiver.IsProcessing())
            {
                ProgressBar ??= new PaneProgressBar(this);
                ProgressBar?.UpdateProgress(_receiver.GetProgressValue());
            }
            else
            {
                ProgressBar?.Dispose();
                ProgressBar = null;
            }
        }

        public PaneProgressBar ProgressBar { get; private set; }

        public void Dispose()
        {
            Cancel(false);
            AllowDisplayTip = false;
            Settings.Default.RTScoreCalculatorList.ListChanged -= RTScoreCalculatorList_ListChanged;
            _receiver.Dispose();
        }

        public bool IsCalculating
        {
            get
            {
                return _receiver.IsProcessing();
            }
        }

        public override bool HasToolbar { get { return RunToRun; } }

        public bool UpdateUIOnIndexChanged()
        {
            return true;
        }

        public bool UpdateUIOnLibraryChanged()
        {
            return ShowReplicate == ReplicateDisplay.single && !RunToRun;
        }

        private void ProductAvailableAction()
        {
            UpdateGraph(false);
        }



        private void RTScoreCalculatorList_ListChanged(object sender, EventArgs e)
        {
            // Avoid updating on every minor change to the list.
            if (_pendingUpdate)
                return;

            // Wait for the UI thread to become available again, and then update
            if (GraphSummary.IsHandleCreated)
            {
                GraphSummary.BeginInvoke(new Action(DelayedUpdate));
                _pendingUpdate = true;
            }
        }

        private void DelayedUpdate()
        {
            // Any change to the calculator list requires a full data update when in auto mode.
            if (string.IsNullOrEmpty(Settings.Default.RTCalculatorName))
                Data = null;

            UpdateGraph(true);
            _pendingUpdate = false;
        }

        public override bool HandleMouseMoveEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            var peptideIndex = PeptideIndexFromPoint(new PointF(e.X, e.Y));
            if (peptideIndex != null)
            {
                double x, y;
                if (RTGraphController.PlotType == PlotTypeRT.residuals && Data != null &&
                    Data.ResidualsRegression != null && Data.ResidualsRegression.Conversion != null)
                    y = Data.GetResidual(Data.ResidualsRegression, peptideIndex.XValue, peptideIndex.YValue);

                if (_tip == null)
                    _tip = new NodeTip(this);

                string yAxisText = peptideIndex.ReplicateFileInfo?.ToString() ?? YAxis.Title.Text;
                _tip.SetTipProvider(
                    new PeptideRegressionTipProvider(peptideIndex, XAxis.Title.Text, yAxisText),
                    new Rectangle(e.Location, new Size()),
                    e.Location);

                GraphSummary.Cursor = Cursors.Hand;
                return true;
            }
            else
            {
                if (_tip != null)
                    _tip.HideTip();

                return base.HandleMouseMoveEvent(sender, e);
            }
        }

        public override bool HandleMouseDownEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            var peptideIndex = PeptideIndexFromPoint(new PointF(e.X, e.Y));
            if (peptideIndex != null)
            {
                var pathSelect = peptideIndex.IdentityPath;
                SelectPeptide(pathSelect);
                var replicateFileInfo = peptideIndex.ReplicateFileInfo;
               
                if (replicateFileInfo != null)
                {
                    GraphSummary.StateProvider.SelectedResultsIndex = replicateFileInfo.ReplicateIndex;
                }
                return true;
            }
            return false;
        }

        public bool RunToRun { get; private set; }

        public void SelectPeptide(IdentityPath peptidePath)
        {
            GraphSummary.StateProvider.SelectedPath = peptidePath;
            if (ShowReplicate == ReplicateDisplay.best && !RunToRun)
            {
                var document = GraphSummary.DocumentUIContainer.DocumentUI;
                var nodePep = (PeptideDocNode)document.FindNode(peptidePath);
                int resultsIndex = nodePep.BestResult;
                if (resultsIndex != -1)
                    GraphSummary.StateProvider.SelectedResultsIndex = resultsIndex;
            }
        }

        public bool AllowDeletePoint(PointF point)
        {
            return PeptideIndexFromPoint(point) != null;
        }

        private GraphData Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        public bool HasOutliers
        {
            get
            {
                var data = Data;
                return data != null && data.HasOutliers;
            }
        }

        public PeptideDocNode[] Outliers
        {
            get
            { 
                GraphData data = Data;
                return data == null ? null : data.Outliers;
            }
        }

        public static PeptideDocNode[] CalcOutliers(SrmDocument document, double threshold, int? precision, bool bestResult)
        {
            var settings = new RegressionSettings(document, null, null, bestResult, threshold, false,
                RTGraphController.PointsType, RTGraphController.RegressionMethod, null, false).ChangeOutlierRtDifferenceThreshold(RTGraphController.OutlierVarianceThreshold);
            var data = new GraphData(settings, precision, null, CancellationToken.None, null, null);
            return data.Refine(CancellationToken.None).Outliers;
        }

        public RetentionTimeRegression RegressionRefined
        {
            get
            { 
                GraphData data = Data;
                return data == null ? null : data.RegressionRefined;
            }
        }

        public RetentionTimeStatistics StatisticsRefined
        {
            get
            {
                GraphData data = Data;
                return data == null ? null : data.StatisticsRefined;
            }
        }

        private static bool IsValidFor(GraphData data, SrmDocument document)
        {
            return data != null && data.IsValidFor(document);
        }

        public bool IsValidFor(SrmDocument document, ReplicateFileId targetIndex, ReplicateFileId originalIndex, bool bestResult, double threshold, bool refine, PointsTypeRT pointsType, RegressionMethodRT regressionMethod)
        {
            var data = Data;
            return data != null && data.IsValidFor(document, targetIndex, originalIndex,bestResult, threshold, refine, pointsType, regressionMethod);
        }

        public void Clear()
        {
            Data = null;
            Title.Text = string.Empty;
            CurveList.Clear();
            GraphObjList.Clear();
        }

        public void Graph(PeptideDocNode nodeSelected)
        {
            var data = Data;
            if (data != null)
                data.Graph(this, nodeSelected);
        }

        private GraphData Update(SrmDocument document, ReplicateFileInfo targetIndex, double threshold, bool refine, PointsTypeRT pointsType, RegressionMethodRT regressionMethod, ReplicateFileInfo origIndex, CancellationToken token)
        {
            var settings = new RegressionSettings(document, targetIndex?.ReplicateFileId, origIndex?.ReplicateFileId,
                (ShowReplicate == ReplicateDisplay.best), threshold, refine, pointsType, regressionMethod, null,
                RunToRun).ChangeOutlierRtDifferenceThreshold(RTGraphController.OutlierVarianceThreshold);
            return new GraphData(settings, null, Data, token, null, null);
            
        }

        private static bool IsDataRefined(GraphData data)
        {
            return data != null && data.IsRefined();
        }

        public bool IsRefined
        {
            get { return IsDataRefined(Data); }
        }

        public bool RegressionRefinedNull => Data.RegressionRefinedNull;

        private GraphData Refine(GraphData currentData, CancellationToken cancellationToken)
        {
            GraphData dataNew = currentData != null ? currentData.Refine(cancellationToken) : null;

            // No refinement happened, if data did not change
            if (ReferenceEquals(dataNew, currentData))
                return currentData;

            return dataNew;
        }

        public override void Draw(Graphics g)
        {
            var data = Data;
            if (data != null && RTGraphController.PlotType == PlotTypeRT.correlation)
            {
                // Force Axes to recalculate to ensure proper layout of labels
                AxisChange(g);
                data.AddLabels(this, g);
            }

            base.Draw(g);
        }

        public RtPoint PeptideIndexFromPoint(PointF point)
        {
            var data = Data;
            return data?.PeptideIndexFromPoint(this, point);
        }

        private const int OVER_THRESHOLD = 4;

        public bool PointIsOver(PointF point, double score, double time)
        {
            float x = XAxis.Scale.Transform(score);
            if (Math.Abs(x - point.X) > OVER_THRESHOLD)
                return false;
            float y = YAxis.Scale.Transform(time);
            if (Math.Abs(y - point.Y) > OVER_THRESHOLD)
                return false;
            return true;
        }

        private void Cancel(bool createNew = true)
        {
            if (_cancellationTokenSource == null)
                return;

            _cancellationTokenSource.Cancel();

            if (createNew)
                _cancellationTokenSource = new CancellationTokenSource();
        }

        public override void UpdateGraph(bool selectionChanged)
        {
            UpdateGraphNow();
            AxisChange();
            GraphSummary.GraphControl.Invalidate();
        }

        private void UpdateGraphNow()
        {
            GraphHelper.FormatGraphPane(this);
            SrmDocument document = GraphSummary.DocumentUIContainer.DocumentUI;
            PeptideDocNode nodeSelected = null;
            var targetIndex = (ShowReplicate == ReplicateDisplay.single || RunToRun ? GraphSummary.TargetResultsIndex : null);
            var originalIndex = RunToRun ? GraphSummary.OriginalResultsIndex : null;
            var results = document.Settings.MeasuredResults;
            bool resultsAvailable = results != null;
            if (resultsAvailable)
            {
                if (targetIndex == null)
                    resultsAvailable = results.IsLoaded;
                else
                    resultsAvailable = results.Chromatograms.Count > targetIndex.ReplicateIndex &&
                                       results.IsChromatogramSetLoaded(targetIndex.ReplicateIndex);
            }

            if (RunToRun && originalIndex == null)
            {
                resultsAvailable = false;
            }

            if (!resultsAvailable)
            {
                Clear();
                return;
            }
                GraphObjList.Clear();
                var nodeTree = GraphSummary.StateProvider.SelectedNode as SrmTreeNode;
                var nodePeptide = nodeTree as PeptideTreeNode;
                while (nodePeptide == null && nodeTree != null)
                {
                    nodeTree = nodeTree.Parent as SrmTreeNode;
                    nodePeptide = nodeTree as PeptideTreeNode;
                }
                if (nodePeptide != null)
                    nodeSelected = nodePeptide.DocNode;

                bool shouldDrawGraph = true;

                double threshold = RTGraphController.OutThreshold;
                bool refine = Settings.Default.RTRefinePeptides && RTGraphController.CanDoRefinementForRegressionMethod;

                bool bestResult = (ShowReplicate == ReplicateDisplay.best);
                    
                if ((RTGraphController.PointsType == PointsTypeRT.standards && !document.GetRetentionTimeStandards().Any()) ||
                    (RTGraphController.PointsType == PointsTypeRT.decoys &&
                        !document.PeptideGroups.Any(nodePepGroup => nodePepGroup.Children.Cast<PeptideDocNode>().Any(nodePep => nodePep.IsDecoy))) ||
                    RTGraphController.PointsType == PointsTypeRT.targets_fdr && targetIndex == null) // Replicate display is not single and this is not a run to run regression
                {
                    RTGraphController.PointsType = PointsTypeRT.targets;
                }

                PointsTypeRT pointsType = RTGraphController.PointsType;
                RegressionMethodRT regressionMethod = RTGraphController.RegressionMethod;

                var regressionSettings = new RegressionSettings(document, targetIndex?.ReplicateFileId,
                        originalIndex?.ReplicateFileId, bestResult,
                        threshold, refine, pointsType, regressionMethod, Settings.Default.RTCalculatorName, RunToRun)
                    .ChangeOutlierRtDifferenceThreshold(RTGraphController.OutlierVarianceThreshold);
            regressionSettings = regressionSettings.ChangeAlignmentTarget(GraphSummary.StateProvider
                .GetRetentionTimeTransformOperation()?.AlignmentTarget);
            if (!_receiver.TryGetProduct(regressionSettings, out var data))
            {
                UpdateProgressHandler();
                return;
            }

            Data = data;

            Graph(nodeSelected);
        }

        private class RegressionSettings : Immutable
        {
            public RegressionSettings(SrmDocument document, ReplicateFileId targetIndex, ReplicateFileId originalIndex, bool bestResult,
                double threshold, bool refine, PointsTypeRT pointsType, RegressionMethodRT regressionMethod, string calculatorName, bool isRunToRun)
            {
                Document = document;
                TargetIndex = targetIndex;
                OriginalIndex = originalIndex;
                BestResult = bestResult;
                Threshold = threshold;
                Refine = refine;
                PointsType = pointsType;
                RegressionMethod = regressionMethod;
                CalculatorName = calculatorName;
                if (!string.IsNullOrEmpty(CalculatorName))
                    Calculators = new[] {Settings.Default.GetCalculatorByName(calculatorName)};
                else
                    Calculators = Settings.Default.RTScoreCalculatorList.ToArray();
                IsRunToRun = isRunToRun;
            }

            protected bool Equals(RegressionSettings other)
            {
                return ReferenceEquals(Document, other.Document) && Equals(TargetIndex, other.TargetIndex) &&
                       Equals(OriginalIndex, other.OriginalIndex) && BestResult == other.BestResult &&
                       Threshold.Equals(other.Threshold) && Refine == other.Refine && PointsType == other.PointsType &&
                       RegressionMethod == other.RegressionMethod && CalculatorName == other.CalculatorName &&
                       ArrayUtil.EqualsDeep(Calculators, other.Calculators) && IsRunToRun == other.IsRunToRun &&
                       Equals(AlignmentTarget, other.AlignmentTarget) && Nullable.Equals(OutlierRtDifferenceThreshold, other.OutlierRtDifferenceThreshold);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((RegressionSettings)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Document != null ? RuntimeHelpers.GetHashCode(Document) : 0;
                    hashCode = (hashCode * 397) ^ (TargetIndex != null ? TargetIndex.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (OriginalIndex != null ? OriginalIndex.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ BestResult.GetHashCode();
                    hashCode = (hashCode * 397) ^ Threshold.GetHashCode();
                    hashCode = (hashCode * 397) ^ Refine.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)PointsType;
                    hashCode = (hashCode * 397) ^ (int)RegressionMethod;
                    hashCode = (hashCode * 397) ^ (CalculatorName != null ? CalculatorName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ ArrayUtil.GetHashCodeDeep(Calculators);
                    hashCode = (hashCode * 397) ^ IsRunToRun.GetHashCode();
                    hashCode = (hashCode * 397) ^ (AlignmentTarget != null ? AlignmentTarget.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ OutlierRtDifferenceThreshold.GetHashCode();
                    return hashCode;
                }
            }

            public SrmDocument Document { get; private set; }
            public ReplicateFileId TargetIndex { get; private set; }

            public ReplicateFileInfo TargetInfo
            {
                get
                {
                    return TargetIndex?.FindInfo(Document.MeasuredResults) ?? ReplicateFileInfo.All;
                }
            }

            public ReplicateFileId OriginalIndex { get; private set; }

            public ReplicateFileInfo OriginalInfo
            {
                get
                {
                    return OriginalIndex?.FindInfo(Document.MeasuredResults) ?? ReplicateFileInfo.Consensus;
                }
            }

            public bool BestResult { get; private set; }
            public double Threshold { get; private set; }
            public bool Refine { get; private set; }
            public PointsTypeRT PointsType { get; private set; }
            public RegressionMethodRT RegressionMethod { get; private set; }
            public string CalculatorName { get; private set; }
            public RetentionScoreCalculatorSpec[] Calculators { get; private set; }
            public bool IsRunToRun { get; private set; }

            public AlignmentTarget AlignmentTarget { get; private set; }

            public RegressionSettings ChangeAlignmentTarget(AlignmentTarget value)
            {
                return ChangeProp(ImClone(this), im => im.AlignmentTarget = value);
            }

            public double? OutlierRtDifferenceThreshold { get; private set; }

            public RegressionSettings ChangeOutlierRtDifferenceThreshold(double? value)
            {
                return ChangeProp(ImClone(this), im => im.OutlierRtDifferenceThreshold = value);
            }
        }

        /// <summary>
        /// Holds the data currently displayed in the graph.
        /// </summary>
        sealed class GraphData : Immutable
        {
            private readonly SrmDocument _document;
            private readonly RegressionMethodRT _regressionMethod;
            private readonly ReplicateFileInfo _targetIndex;
            private readonly ReplicateFileInfo _originalIndex; // set to null if we are using IRTs
            private readonly bool _bestResult;
            private readonly double _threshold;
            private readonly int? _thresholdPrecision;
            private readonly bool _refine;
            private readonly PointsTypeRT _pointsType;
            private readonly List<RtPoint> _points;

            private readonly RetentionTimeScoreCache _scoreCache;

            private readonly RetentionTimeRegression _regressionPredict;
            private readonly IRegressionFunction _conversionPredict;
            private readonly RetentionTimeStatistics _statisticsPredict;

            private readonly RetentionTimeRegression _regressionAll;
            private readonly RetentionTimeStatistics _statisticsAll;

            private RetentionTimeRegression _regressionRefined;
            private RetentionTimeStatistics _statisticsRefined;

            private double[] _timesRefined;
            private double[] _scoresRefined;
            private double[] _timesOutliers;
            private double[] _scoresOutliers;
            private readonly string _calculatorName;

            private readonly RetentionScoreCalculatorSpec _calculator;

            private RetentionScoreCalculatorSpec Calculator { get { return _calculator; } }

            private HashSet<int> _outlierIndexes;

            public bool IsRunToRun { get; private set; }

            public GraphData(RegressionSettings regressionSettings,
                int? thresholdPrecision,
                GraphData dataPrevious,
                CancellationToken token,
                Action<int> progressChange,
                AllAlignments allAlignments)
            {
                RegressionSettings = regressionSettings;
                _document = regressionSettings.Document;
                _targetIndex = regressionSettings.TargetInfo;
                _originalIndex = regressionSettings.OriginalInfo;
                IsRunToRun = regressionSettings.IsRunToRun;
                if(IsRunToRun && _originalIndex == null)
                    throw new ArgumentException(@"Original index cannot not be negative if we are doing run to run regression");
                _bestResult = regressionSettings.BestResult && !IsRunToRun;
                _threshold = regressionSettings.Threshold;
                _thresholdPrecision = thresholdPrecision;
                _pointsType = regressionSettings.PointsType;
                _regressionMethod = regressionSettings.RegressionMethod;
                _points = new List<RtPoint>();

                var standards = new HashSet<Target>();
                if (RTGraphController.PointsType == PointsTypeRT.standards)
                    standards = _document.GetRetentionTimeStandards();
                
                // Only used if we are comparing two runs
                IList<ReplicateFileInfo> targetInfos;
                if (_targetIndex.ReplicateFileId == null)
                {
                    targetInfos = ReplicateFileInfo.List(_document.MeasuredResults).ToArray();
                }
                else
                {
                    targetInfos = new[] { _targetIndex };
                }

                int totalMoleculeCount = _document.MoleculeCount;
                int currentMoleculeIndex = 0;
                foreach (var (nodeMoleculeGroup, nodePeptide) in _document.MoleculeGroups.SelectMany(moleculeGroup =>
                             moleculeGroup.Molecules.Select(molecule => Tuple.Create(moleculeGroup, molecule))))
                {
                    token.ThrowIfCancellationRequested();
                    progressChange?.Invoke(currentMoleculeIndex * 100 / totalMoleculeCount);
                    currentMoleculeIndex++;
                    var identityPath = new IdentityPath(nodeMoleculeGroup.PeptideGroup, nodePeptide.Peptide);
                    switch (RTGraphController.PointsType)
                    {
                        case PointsTypeRT.targets:
                            if (nodePeptide.IsDecoy)
                                continue;
                            break;
                        case PointsTypeRT.targets_fdr:
                        {
                            if (nodePeptide.IsDecoy)
                                continue;

                            if (TargetIndex != null && GetMaxQValue(nodePeptide, _targetIndex) >= 0.01 ||
                                OriginalIndex != null && GetMaxQValue(nodePeptide, _originalIndex) >= 0.01)
                                continue;
                            break;
                        }
                        case PointsTypeRT.standards:
                            if (!standards.Contains(_document.Settings.GetModifiedSequence(nodePeptide))
                                || nodePeptide.GlobalStandardType !=
                                StandardType
                                    .IRT) // In case of 15N labeled peptides, the unlabeled form may also show up
                                continue;
                            break;
                        case PointsTypeRT.decoys:
                            if (!nodePeptide.IsDecoy)
                                continue;
                            break;
                    }


                    //Only used if we are doing run to run, otherwise we use scores
                    float? rtOrig = null;
                    if (_originalIndex != null)
                        rtOrig = nodePeptide.GetMeasuredRetentionTime(_originalIndex);

                    foreach (var targetInfo in targetInfos)
                    {
                        float? rtTarget = null;

                        if (targetInfo != null)
                            rtTarget = nodePeptide.GetMeasuredRetentionTime(targetInfo);
                        else
                        {
                            int iBest = nodePeptide.BestResult;
                            if (iBest != -1)
                                rtTarget = nodePeptide.GetSchedulingTime(iBest);
                        }

                        if (!rtTarget.HasValue)
                        {
                            if (_targetIndex.ReplicateFileId == null)
                            {
                                continue;
                            }

                            rtTarget = 0;
                        }

                        if (!rtOrig.HasValue)
                        {
                            if (_targetIndex.ReplicateFileId == null)
                            {
                                continue;
                            }

                            rtOrig = 0;
                        }

                        var alignmentFunction =
                            allAlignments?.GetAlignmentFunction(targetInfo.ReplicateFileId.FileId);
                        var alignedYValue = alignmentFunction?.GetY(rtTarget.Value) ?? rtTarget.Value;
                        if (alignmentFunction != null)
                        {
                            rtTarget = (float) alignmentFunction?.GetY(rtTarget.Value);
                        }
                        _points.Add(new RtPoint(nodePeptide, identityPath, targetInfo, rtOrig.Value, alignedYValue, rtTarget.Value));
                    }
                }

                _calculatorName = Settings.Default.RTCalculatorName;

                if (IsRunToRun)
                {
                    var targetTimesDict = GetTargetTimes().ToDictionary(measuredRetentionTime=>measuredRetentionTime.PeptideSequence, measuredRetentionTime=>measuredRetentionTime.RetentionTime);
                    var origTimesDict = GetOriginalTimes().ToDictionary(measuredRetentionTime=>measuredRetentionTime.PeptideSequence, measuredRetentionTime => measuredRetentionTime.RetentionTime);
                    _calculator = new DictionaryRetentionScoreCalculator(XmlNamedElement.NAME_INTERNAL, DocumentRetentionTimes.ConvertToMeasuredRetentionTimes(origTimesDict));
                    var alignedRetentionTimes = AlignedRetentionTimes.AlignLibraryRetentionTimes(targetTimesDict,
                        origTimesDict, RegressionSettings.Refine ? RegressionSettings.Threshold : 0, _regressionMethod,
                        token);
                    if (alignedRetentionTimes != null)
                    {
                        _regressionAll = alignedRetentionTimes.Regression;
                        _statisticsAll = alignedRetentionTimes.RegressionStatistics;
                    }
                }
                else
                {
                    var calc = !string.IsNullOrEmpty(_calculatorName)
                        ? Settings.Default.GetCalculatorByName(Settings.Default.RTCalculatorName)
                        : null;
                    if (calc == null)
                    {
                        // Initialize all calculators
                        Settings.Default.RTScoreCalculatorList.Initialize(null);

                        var summary = RetentionTimeRegression.CalcBestRegressionBackground(XmlNamedElement.NAME_INTERNAL,
                            Settings.Default.RTScoreCalculatorList.ToList(), GetOriginalTimes(), _scoreCache, true,
                            _regressionMethod, token);
                        
                        _calculator = summary.Best.Calculator;
                        _statisticsAll = summary.Best.Statistics;
                        _regressionAll = summary.Best.Regression;
                    }
                    else
                    {
                        // Initialize the one calculator
                        calc = Settings.Default.RTScoreCalculatorList.Initialize(null, calc);

                        _regressionAll = RetentionTimeRegression.CalcSingleRegression(XmlNamedElement.NAME_INTERNAL,
                            calc,
                            GetTargetTimes(),
                            _scoreCache,
                            true,
                            _regressionMethod,
                            out _statisticsAll,
                            out _,
                            token);

                        token.ThrowIfCancellationRequested();
                        _calculator = calc;

                        //If _regressionAll is null, it is safe to assume that the calculator is an iRT Calc with
                        //its database disconnected.
                        if (_regressionAll == null)
                        {
                            var tryIrtCalc = calc as RCalcIrt;
                            //Only show an error message if the user specifically chooses this calculator.
                            if (dataPrevious != null && !ReferenceEquals(calc, dataPrevious.Calculator) &&
                                tryIrtCalc != null)
                            {
                                MessageDlg.Show(Program.MainWindow, string.Format(
                                    GraphsResources.GraphData_GraphData_The_database_for_the_calculator__0__could_not_be_opened__Check_that_the_file__1__was_not_moved_or_deleted_,
                                    tryIrtCalc.Name, tryIrtCalc.DatabasePath));
                                return;
                            }
                        }
                    }
                }

                if (_regressionAll != null)
                {
                    _scoreCache = new RetentionTimeScoreCache(new[] { _calculator }, GetTargetTimes(),
                                                              dataPrevious != null ? dataPrevious._scoreCache : null);

                    if (dataPrevious != null && !ReferenceEquals(_calculator, dataPrevious._calculator))
                        _scoreCache.RecalculateCalcCache(_calculator, token);

                    _scoresRefined = _statisticsAll.ListHydroScores.ToArray();
                    _timesRefined = _statisticsAll.ListRetentionTimes.ToArray();
                }

                _regressionPredict = (IsRunToRun || _regressionMethod != RegressionMethodRT.linear)  ? null : _document.Settings.PeptideSettings.Prediction.RetentionTime;
                if (_regressionPredict != null)
                {
                    if (!Equals(_calculator, _regressionPredict.Calculator))
                        _regressionPredict = null;
                    else
                    {
                        IDictionary<Target, double> scoreCache = null;
                        if (_regressionAll != null && Equals(_regressionAll.Calculator, _regressionPredict.Calculator))
                            scoreCache = _statisticsAll.ScoreCache;
                        // This is a bit of a HACK to better support the very common case of replicate graphing
                        // with a replicate that only has one file. More would need to be done for replicates
                        // composed of multiple files.
                        ChromFileInfoId fileId = null;
                        if (!RegressionSettings.BestResult && _targetIndex != null)
                        {
                            var chromatogramSet = _document.Settings.MeasuredResults.Chromatograms[_targetIndex.ReplicateIndex];
                            if (chromatogramSet.FileCount > 0)
                            {
                                _conversionPredict = _regressionPredict.GetConversion(_targetIndex.ReplicateFileId.FileId);
                            }
                        }
                        _statisticsPredict = _regressionPredict.CalcStatistics(GetTargetTimes(), scoreCache, fileId);
                    }
                }
                // Only refine, if not already exceeding the threshold
                _refine = RegressionSettings.Refine && !IsRefined();
            }

            public RegressionSettings RegressionSettings { get; }

            private float GetMaxQValue(PeptideDocNode node, ReplicateFileInfo replicateFileInfo)
            {
                var chromInfos = node.TransitionGroups
                    .Select(tr => replicateFileInfo.GetChromInfos(tr.Results).FirstOrDefault(ci => ci.OptimizationStep == 0))
                    .Where(ci => ci?.QValue != null).ToArray();

                if (chromInfos.Length == 0)
                    return 1.0f;

                return chromInfos.Max(ci => ci.QValue.Value);
            }

            public bool IsValidFor(SrmDocument document)
            {
                return ReferenceEquals(document, _document);
            }

            public bool IsValidFor(SrmDocument document, ReplicateFileId targetIndex, ReplicateFileId originalIndex, bool bestResult, double threshold, bool refine, PointsTypeRT pointsType, RegressionMethodRT regressionMethod)
            {
                string calculatorName = Settings.Default.RTCalculatorName;
                if (string.IsNullOrEmpty(calculatorName) && !IsRunToRun)
                    calculatorName = _calculator.Name;
                return IsValidFor(document) &&
                        Equals(_targetIndex.ReplicateFileId, targetIndex) &&
                        Equals(_originalIndex.ReplicateFileId, originalIndex) &&
                        _bestResult == bestResult &&
                        _threshold == threshold &&
                        _pointsType == pointsType &&
                        _regressionMethod == regressionMethod && 
                        (IsRunToRun || (_calculatorName == Settings.Default.RTCalculatorName &&
                        ReferenceEquals(_calculator, Settings.Default.GetCalculatorByName(calculatorName)))) &&
                        // Valid if refine is true, and this data requires no further refining
                        (_refine == refine || (refine && IsRefined()));
            }

            public ReplicateFileInfo TargetIndex { get { return _targetIndex; } }

            public ReplicateFileInfo OriginalIndex { get { return _originalIndex; } }

            public RetentionTimeRegression RegressionRefined
            {
                get { return _regressionRefined ?? _regressionAll; }
            }

            public RetentionTimeStatistics StatisticsRefined
            {
                get { return _statisticsRefined ?? _statisticsAll; }
            }

            public bool RegressionRefinedNull => _regressionRefined == null;

            public bool IsRefined()
            {
                // If refinement has been performed, or it doesn't need to be.
                if (_regressionRefined != null)
                    return true;
                if (_statisticsAll == null)
                    return false;
                return RetentionTimeRegression.IsAboveThreshold(_statisticsAll.R, _threshold);
            }

            public GraphData Refine(CancellationToken cancellationToken)
            {
                if (IsRefined())
                    return this;
                var result = ImClone(this).RefineCloned(_threshold, _thresholdPrecision, cancellationToken);
                if (result == null)
                    return this;
                return result;
            }

            private GraphData RefineCloned(double threshold, int? precision, CancellationToken cancellationToken)
            {
                // Create list of deltas between predicted and measured times
                _outlierIndexes = new HashSet<int>();
                // Start with anything assigned a zero retention time as outliers
                var targetTimes = GetTargetTimes();
                var originalTimes = GetOriginalTimes();
                for (int i = 0; i < targetTimes.Count; i++)
                {
                    if (targetTimes[i].RetentionTime == 0 || (originalTimes != null && originalTimes[i].RetentionTime == 0))
                        _outlierIndexes.Add(i);
                }

                // Now that we have added iRT calculators, RecalcRegression
                // cannot go and mark as outliers peptides at will anymore. It must know which peptides, if any,
                // are required by the calculator for a regression. With iRT calcs, the standard is required.
                if(!_calculator.IsUsable)
                    return null;

                HashSet<Target> standardNames;
                try
                {
                    var names = _calculator.GetStandardPeptides(targetTimes.Select(pep => pep.PeptideSequence));
                    standardNames = new HashSet<Target>(names);
                }
                catch (CalculatorException)
                {
                    standardNames = new HashSet<Target>();
                }

                //For run to run all peptides are variables. There are no standards.
                var standardPeptides = IsRunToRun ? new MeasuredRetentionTime[0] : targetTimes.Where(pep => pep.IsStandard && standardNames.Contains(pep.PeptideSequence)).ToArray();
                var variableTargetPeptides = IsRunToRun ? targetTimes.ToArray() : targetTimes.Where(pep => !pep.IsStandard || !standardNames.Contains(pep.PeptideSequence)).ToArray();
                var variableOrigPeptides = originalTimes;

                //Throws DatabaseNotConnectedException
                _regressionRefined = (_regressionAll == null
                                          ? null
                                          : _regressionAll.FindThreshold(threshold,
                                                                         precision,
                                                                         0,
                                                                         variableTargetPeptides.Length,
                                                                         standardPeptides,
                                                                         variableTargetPeptides,
                                                                         variableOrigPeptides,
                                                                         _statisticsAll,
                                                                         _calculator,
                                                                         _regressionMethod,
                                                                         _scoreCache,
                                                                         cancellationToken, 
                                                                         ref _statisticsRefined,
                                                                         ref _outlierIndexes));

                if (ReferenceEquals(_regressionRefined, _regressionAll))
                    return null;

                // Separate lists into acceptable and outliers
                var listScoresRefined = new List<double>();
                var listTimesRefined = new List<double>();
                var listScoresOutliers = new List<double>();
                var listTimesOutliers = new List<double>();
                for (int i = 0; i < _scoresRefined.Length; i++)
                {
                    if (_outlierIndexes.Contains(i))
                    {
                        listScoresOutliers.Add(_scoresRefined[i]);
                        listTimesOutliers.Add(_timesRefined[i]);
                    }
                    else
                    {
                        listScoresRefined.Add(_scoresRefined[i]);
                        listTimesRefined.Add(_timesRefined[i]);
                    }
                }
                _scoresRefined = listScoresRefined.ToArray();
                _timesRefined = listTimesRefined.ToArray();
                _scoresOutliers = listScoresOutliers.ToArray();
                _timesOutliers = listTimesOutliers.ToArray();

                return this;
            }

            public RtPoint PeptideIndexFromPoint(RTLinearRegressionGraphPane graphPane, PointF point)
            {
                var regression = ResidualsRegression;
                if (RTGraphController.PlotType == PlotTypeRT.correlation)
                    regression = null;
                if (RTGraphController.PlotType == PlotTypeRT.correlation || regression != null)
                {
                    for (int i = 0; i < _points.Count; i++)
                    {
                        if (PointIsOverEx(graphPane, point, regression, _points[i].XValue,
                                _points[i].YValue))
                        {
                            return _points[i]; 
                        }
                    }
                }
                return null;
            }

            private IEnumerable<KeyValuePair<Target, List<RtPoint>>> GetPointsByTarget()
            {
                foreach (var targetGroup in _points.GroupBy(pt => pt.Target))
                {
                    yield return new KeyValuePair<Target, List<RtPoint>>(targetGroup.Key,
                        targetGroup.ToList());
                }
            }

            public List<MeasuredRetentionTime> GetTargetTimes()
            {
                return GetPointsByTarget().Select(kvp =>
                    new MeasuredRetentionTime(kvp.Key, kvp.Value.Select(pt => pt.YValue).Average(), true)).ToList();
            }

            public List<MeasuredRetentionTime> GetOriginalTimes()
            {
                return GetPointsByTarget().Select(kvp =>
                    new MeasuredRetentionTime(kvp.Key, kvp.Value.Select(pt => pt.XValue).Average())).ToList();
            }

            private bool PointIsOverEx(RTLinearRegressionGraphPane graphPane, PointF point,
                RetentionTimeRegression regression, double x, double y)
            {
                if (regression != null && regression.IsUsable)
                    y = GetResidual(regression, x, y);
                return graphPane.PointIsOver(point, x, y);
            }

            public bool PointFromPeptide(PeptideDocNode nodePeptide, out double score, out double time)
            {
                if (nodePeptide != null && _regressionAll != null)
                {
                    int iRefined = 0, iOut = 0;
                    for (int i = 0; i < _points.Count; i++)
                    {
                        if (Equals(_points[i]))
                        if (_outlierIndexes != null && _outlierIndexes.Contains(i))
                        {
                            if (ReferenceEquals(nodePeptide, _points[i].DocNode))
                            {
                                score = _scoresOutliers[iOut];
                                time = _timesOutliers[iOut];
                                return true;
                            }
                            iOut++;
                        }
                        else
                        {
                            if (ReferenceEquals(nodePeptide, _points[i].DocNode))
                            {
                                score = _scoresRefined[iRefined];
                                time = _timesRefined[iRefined];
                                return true;
                            }
                            iRefined++;
                        }
                    }
                }
                score = 0;
                time = 0;
                return false;
            }

            public bool HasOutliers { get { return _outlierIndexes != null && _outlierIndexes.Count > 0; } }

            public PeptideDocNode[] Outliers
            {
                get
                {
                    if (!HasOutliers)
                        return new PeptideDocNode[0];

                    var listOutliers = new List<PeptideDocNode>();
                    for (int i = 0; i < _points.Count; i++)
                    {
                        if (_outlierIndexes.Contains(i))
                            listOutliers.Add(_points[i].DocNode);
                    }
                    return listOutliers.ToArray();
                }
            }

            public void Graph(GraphPane graphPane, PeptideDocNode nodeSelected)
            {
                graphPane.CurveList.Clear();
                graphPane.XAxis.Title.Text = XAxisName;
                graphPane.YAxis.Title.Text = YAxisName;
                if (RTGraphController.PlotType == PlotTypeRT.correlation)
                    GraphCorrelation(graphPane, nodeSelected);
                else
                    GraphResiduals(graphPane, nodeSelected);
            }

            private void GraphCorrelation(GraphPane graphPane, PeptideDocNode nodeSelected)
            {
                if (graphPane.YAxis.Scale.MinAuto)
                {
                    graphPane.YAxis.Scale.MinAuto = false;
                    graphPane.YAxis.Scale.Min = 0;
                }

                double scoreSelected, timeSelected;
                if (PointFromPeptide(nodeSelected, out scoreSelected, out timeSelected))
                {
                    Color colorSelected = GraphSummary.ColorSelected;
                    var curveOut = graphPane.AddCurve(null, new[] { scoreSelected }, new[] { timeSelected },
                                                      colorSelected, SymbolType.Diamond);
                    curveOut.Line.IsVisible = false;
                    curveOut.Symbol.Fill = new Fill(colorSelected);
                    curveOut.Symbol.Size = 8f;
                }

                string labelPoints = Helpers.PeptideToMoleculeTextMapper.Translate(GraphsResources.GraphData_Graph_Peptides, _document.DocumentType);
                if (!_refine)
                {
                    GraphRegression(graphPane, _statisticsAll, _regressionAll, GraphsResources.GraphData_Graph_Regression, COLOR_LINE_REFINED);
                }
                else
                {
                    labelPoints = Helpers.PeptideToMoleculeTextMapper.Translate(GraphsResources.GraphData_Graph_Peptides_Refined, _document.DocumentType);
                    GraphRegression(graphPane, _statisticsRefined, _regressionAll, GraphsResources.GraphData_Graph_Regression_Refined, COLOR_LINE_REFINED);
                    GraphRegression(graphPane, _statisticsAll, _regressionAll, GraphsResources.GraphData_Graph_Regression, COLOR_LINE_ALL);
                }

                if (_regressionPredict != null && Settings.Default.RTPredictorVisible)
                {
                    GraphRegression(graphPane, _statisticsPredict, _regressionAll, GraphsResources.GraphData_Graph_Predictor, COLOR_LINE_PREDICT);
                }
                var outlierPointList = MakePointList(_points.Where(IsOutlier));
                if (outlierPointList.Any() || Program.MainWindow.ShowOnlyOutliers)
                {
                    var curveOut = graphPane.AddCurve(GraphsResources.GraphData_Graph_Outliers, outlierPointList,
                        Color.Black, SymbolType.Diamond);
                    curveOut.Line.IsVisible = false;
                    curveOut.Symbol.Border.IsVisible = false;
                    curveOut.Symbol.Fill = new Fill(COLOR_OUTLIERS);
                }

                if (!Program.MainWindow.ShowOnlyOutliers) {
                    var curve = graphPane.AddCurve(labelPoints, MakePointList(_points.Where(pt=>!IsOutlier(pt))),
                                               Color.Black, SymbolType.Diamond);

                    curve.Line.IsVisible = false;
                    curve.Symbol.Border.IsVisible = false;
                    curve.Symbol.Fill = new Fill(COLOR_REFINED);
                }
            }

            private PointPairList MakePointList(IEnumerable<RtPoint> points)
            {
                return new PointPairList(points.Select(pt => new PointPair(pt.XValue, pt.YValue)
                {
                    Tag = pt
                }).ToList());
            }

            private void GraphResiduals(GraphPane graphPane, PeptideDocNode nodeSelected)
            {
                if (!graphPane.YAxis.Scale.MinAuto && graphPane.ZoomStack.Count == 0)
                {
                    graphPane.YAxis.Scale.MinAuto = true;
                    graphPane.YAxis.Scale.MaxAuto = true;
                }

                var regression = ResidualsRegression;
                if (regression == null || regression.Conversion == null)
                    return;

                double scoreSelected, timeSelected;
                if (PointFromPeptide(nodeSelected, out scoreSelected, out timeSelected))
                {
                    timeSelected = GetResidual(regression, scoreSelected, timeSelected);

                    Color colorSelected = GraphSummary.ColorSelected;
                    var curveOut = graphPane.AddCurve(null, new[] { scoreSelected }, new[] { timeSelected },
                                                      colorSelected, SymbolType.Diamond);
                    curveOut.Line.IsVisible = false;
                    curveOut.Symbol.Fill = new Fill(colorSelected);
                    curveOut.Symbol.Size = 8f;
                }

                string labelPoints =
                    Helpers.PeptideToMoleculeTextMapper.Translate(_refine ? GraphsResources.GraphData_Graph_Peptides_Refined : GraphsResources.GraphData_Graph_Peptides, _document.DocumentType); 
                var curve = graphPane.AddCurve(labelPoints, _scoresRefined, GetResiduals(regression, _scoresRefined, _timesRefined),
                                               Color.Black, SymbolType.Diamond);
                curve.Line.IsVisible = false;
                curve.Symbol.Border.IsVisible = false;
                curve.Symbol.Fill = new Fill(COLOR_REFINED);

                if (_scoresOutliers != null)
                {
                    var curveOut = graphPane.AddCurve(GraphsResources.GraphData_Graph_Outliers, _scoresOutliers, 
                                                      GetResiduals(regression, _scoresOutliers, _timesOutliers),
                                                      Color.Black, SymbolType.Diamond);
                    curveOut.Line.IsVisible = false;
                    curveOut.Symbol.Border.IsVisible = false;
                    curveOut.Symbol.Fill = new Fill(COLOR_OUTLIERS);
                }
            }

            public bool IsOutlier(RtPoint point)
            {
                return Math.Abs(point.YValue - point.XValue) >
                       RegressionSettings.OutlierRtDifferenceThreshold;
            }

            public RetentionTimeRegression ResidualsRegression
            {
                get { return _regressionPredict ?? _regressionRefined ?? _regressionAll; }
            }

            private string ResidualsLabel
            {
                get
                {
                    if (IsRunToRun)
                    {
                        return string.Format(GraphsResources.GraphData_ResidualsLabel_Time_from_Regression___0__,
                            _targetIndex);
                    }
                    else
                    {
                        return _regressionPredict != null
                            ? GraphsResources.GraphData_GraphResiduals_Time_from_Prediction
                            : Resources.GraphData_GraphResiduals_Time_from_Regression;
                    }
                }
            }

            private string CorrelationLabel
            {
                get
                {
                    if (IsRunToRun)
                    {
                        if (Equals(_targetIndex, ReplicateFileInfo.All))
                        {
                            return "Measured Time";
                        }
                        return string.Format(GraphsResources.GraphData_CorrelationLabel_Measured_Time___0__,
                            _targetIndex);
                    }
                    else
                    {
                        return Resources.RTLinearRegressionGraphPane_RTLinearRegressionGraphPane_Measured_Time;
                    }
                }
            }

            private double[] GetResiduals(RetentionTimeRegression regression, double[] scores, double[] times)
            {
                var residualsRefined = new double[times.Length];
                for (int i = 0; i < residualsRefined.Length; i++)
                    residualsRefined[i] = GetResidual(regression, scores[i], times[i]);
                return residualsRefined;
            }

            public double GetResidual(RetentionTimeRegression regression, double score, double time)
            {
                //We round this for numerical error.
                return Math.Round(time - GetConversion(regression).GetY(score), 6);
            }

            private IRegressionFunction GetConversion(RetentionTimeRegression regression)
            {
                if (regression == null)
                    return null;
                if (ReferenceEquals(regression, _regressionPredict) && _conversionPredict != null)
                    return _conversionPredict;
                return regression.Conversion;
            }

            private static void GraphRegression(GraphPane graphPane,
                                                RetentionTimeStatistics statistics, RetentionTimeRegression regression, string name, Color color)
            {
                double[] lineScores, lineTimes;
                if (statistics == null || regression == null)
                {
                    lineScores = new double[0];
                    lineTimes = new double[0];
                }
                else
                {
                    regression.Conversion.GetCurve(statistics, out lineScores, out lineTimes);    
                }
                var curve = graphPane.AddCurve(name, lineScores, lineTimes, color, SymbolType.None);
                if (lineScores.Length > 0 && lineTimes.Length > 0)
                {
                    graphPane.AddCurve(string.Empty, new[] { lineScores[0] }, new[] { lineTimes[0] }, color, SymbolType.Square);
                    graphPane.AddCurve(string.Empty, new[] { lineScores.Last() }, new[] { lineTimes.Last() }, color, SymbolType.Square);
                }

                curve.Line.IsAntiAlias = true;
                curve.Line.IsOptimizedDraw = true;
            }

            public void AddLabels(GraphPane graphPane, Graphics g)
            {
                RectangleF rectChart = graphPane.Chart.Rect;
                PointF ptTop = rectChart.Location;

                // Setup axes scales to enable the ReverseTransform method
                var xAxis = graphPane.XAxis;
                xAxis.Scale.SetupScaleData(graphPane, xAxis);
                var yAxis = graphPane.YAxis;
                yAxis.Scale.SetupScaleData(graphPane, yAxis);

                float yNext = ptTop.Y;
                double scoreLeft = xAxis.Scale.ReverseTransform(ptTop.X + 8);
                double timeTop = yAxis.Scale.ReverseTransform(yNext);

                graphPane.GraphObjList.RemoveAll(o => o is TextObj);

                if (!_refine)
                {
                    yNext += AddRegressionLabel(graphPane, g, scoreLeft, timeTop,
                        _regressionAll, _statisticsAll, COLOR_LINE_REFINED);
                }
                else
                {
                    yNext += AddRegressionLabel(graphPane, g, scoreLeft, timeTop,
                        _regressionRefined, _statisticsRefined, COLOR_LINE_REFINED);
                    timeTop = yAxis.Scale.ReverseTransform(yNext);
                    yNext += AddRegressionLabel(graphPane, g, scoreLeft, timeTop,
                        _regressionAll, _statisticsAll, COLOR_LINE_ALL);
                }

                if (_regressionPredict != null &&
                    _regressionPredict.Conversion != null &&
                    Settings.Default.RTPredictorVisible)
                {
                    timeTop = yAxis.Scale.ReverseTransform(yNext);
                    AddRegressionLabel(graphPane, g, scoreLeft, timeTop,
                                       _regressionPredict, _statisticsPredict, COLOR_LINE_PREDICT);
                }
            }

            private float AddRegressionLabel(PaneBase graphPane, Graphics g, double score, double time,
                                                    RetentionTimeRegression regression, RetentionTimeStatistics statistics, Color color)
            {
                string label;
                var conversion = GetConversion(regression);
                if (conversion == null || statistics == null)
                {
                    // ReSharper disable LocalizableElement
                    label = String.Format("{0} = ?, {1} = ?\n" + "{2} = ?\n" + "r = ?",
                                          Resources.Regression_slope,
                                          Resources.Regression_intercept,
                                          Resources.GraphData_AddRegressionLabel_window);
                    // ReSharper restore LocalizableElement
                }
                else
                {
                    label = regression.Conversion.GetRegressionDescription(statistics.R, regression.TimeWindow);
                }


                TextObj text = new TextObj(label, score, time,
                                           CoordType.AxisXYScale, AlignH.Left, AlignV.Top)
                                   {
                                       IsClippedToChartRect = true,
                                       ZOrder = ZOrder.E_BehindCurves,
                                       FontSpec = GraphSummary.CreateFontSpec(color),
                                   };
                graphPane.GraphObjList.Add(text);

                // Measure the text just added, and return its height
                SizeF sizeLabel = text.FontSpec.MeasureString(g, label, graphPane.CalcScaleFactor());
                return sizeLabel.Height + 3;
            }

            private string XAxisName
            {
                get
                {
                    if (IsRunToRun)
                    {
                        if (_document.MeasuredResults != null && _originalIndex != null)
                        {
                            return string.Format(GraphsResources.GraphData_CorrelationLabel_Measured_Time___0__,
                                _originalIndex);
                        }
                        return string.Empty;
                    }
                    return Calculator.Name;
                }
            }

            private string YAxisName
            {
                get
                {
                    string axisName;
                    if (RTGraphController.PlotType == PlotTypeRT.correlation)
                        axisName = CorrelationLabel;
                    else
                        axisName = ResidualsLabel;
                    return RegressionSettings.AlignmentTarget?.AnnotateAxisName(_document, axisName) ?? axisName;
                }
            }
        }

        public Rectangle ScreenRect { get { return Screen.GetBounds(GraphSummary); } }
        private bool _allowDisplayTip;
        public bool AllowDisplayTip
        {
            get { return !GraphSummary.IsDisposed && _allowDisplayTip; }
            private set { _allowDisplayTip = value; }
        }

        public Rectangle RectToScreen(Rectangle r)
        {
            return GraphSummary.RectangleToScreen(r);
        }

        public class RtPoint : Immutable
        {
            public RtPoint(PeptideDocNode peptideDocNode, IdentityPath identityPath, ReplicateFileInfo replicateFileInfo, double xValue, double yValue, double rawYValue)
            {
                DocNode = peptideDocNode;
                IdentityPath = identityPath;
                ReplicateFileInfo = replicateFileInfo;
                XValue = xValue;
                YValue = yValue;
                RawYValue = rawYValue;
            }
            public PeptideDocNode DocNode { get; }
            public IdentityPath IdentityPath { get; }
            public ReplicateFileInfo ReplicateFileInfo { get; }
            public Target Target
            {
                get { return DocNode.SourceModifiedTarget; }
            }
            public double XValue { get; }
            public double YValue { get; }
            public double RawYValue { get; }
        }

        private static Producer<RegressionSettings, GraphData> GRAPH_DATA_PRODUCER = new GraphDataProducer();

        private class GraphDataProducer : Producer<RegressionSettings, GraphData>
        {
            public override GraphData ProduceResult(ProductionMonitor productionMonitor, RegressionSettings parameter, IDictionary<WorkOrder, object> inputs)
            {
                var allAlignments = inputs.Values.OfType<AllAlignments>().FirstOrDefault();
                return new GraphData(parameter, null, null, productionMonitor.CancellationToken, productionMonitor.SetProgress, allAlignments);
            }

            public override IEnumerable<WorkOrder> GetInputs(RegressionSettings parameter)
            {
                yield return AllAlignments.PRODUCER.MakeWorkOrder(new AllAlignments.Parameter(parameter.Document,
                    parameter.AlignmentTarget));
            }
        }
    }
}
