using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Common.SystemUtil.Caching;
using pwiz.Skyline.EditUI.PeakImputation;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;
using static pwiz.Skyline.EditUI.PeakImputation.PeakImputationForm;

namespace pwiz.Skyline.Model.PeakImputation
{
    public class PeakImputationData
    {
        public static readonly Producer<Parameters, PeakImputationData> PRODUCER = new DataProducer();


        private AllAlignments _allAlignments;
        private ScoreQValueMap _scoreQValueMap;

        public PeakImputationData(Parameters parameters, ScoringResults scoringResults, AllAlignments allAlignments, IEnumerable<MoleculePeaks> rows)
        {
            Params = parameters;
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
            MoleculePeaks = ImmutableList.ValueOf(rowList.Select(FillInScores));
        }

        public Parameters Params { get; }

        public AlignmentFunction GetAlignmentFunction(MsDataFileUri msDataFileUri)
        {
            return _allAlignments?.GetAlignmentFunction(msDataFileUri) ?? AlignmentFunction.IDENTITY;
        }

        public ScoringResults ScoringResults { get; }

        public ImmutableList<MoleculePeaks> MoleculePeaks { get; }

        public double? GetMeanStandardDeviation()
        {
            var standardDeviations = new List<double>();
            foreach (var row in MoleculePeaks)
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

        public MoleculePeaks FillInScores(MoleculePeaks row)
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
                if (!Equals(Params.PeakScoringModel, LegacyScoringModel.DEFAULT_MODEL))
                {
                    peak = peak.ChangePValue(ZScoreToPValue(peak.Score.Value));
                }
                newPeaks[iPeak] = peak;
            }

            if (ArrayUtil.ReferencesEqual(newPeaks, row.Peaks))
            {
                return row;
            }

            return new MoleculePeaks(row.PeptideIdentityPath, newPeaks);
        }


        private class DataProducer : Producer<Parameters, PeakImputationData>
        {
            public override PeakImputationData ProduceResult(ProductionMonitor productionMonitor, Parameters parameter, IDictionary<WorkOrder, object> inputs)
            {
                ScoringResults scoringResults = ScoringResults.PRODUCER.GetResult(inputs, new ScoringResults.Parameters(parameter.Document, parameter.PeakScoringModel, parameter.ManualPeakTreatment == ManualPeakTreatment.OVERWRITE));
                AllAlignments allAlignments = AllAlignments.PRODUCER.GetResult(inputs,
                    new AllAlignments.Parameter(parameter.Document, parameter.AlignmentTarget));
                var rows = ImmutableList.ValueOf(GetRows(productionMonitor.CancellationToken, parameter, scoringResults,
                    GetResultFileInfos(parameter.Document.Value, allAlignments)));
                return new PeakImputationData(parameter, scoringResults, allAlignments, rows);
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
                    yield return AllAlignments.PRODUCER.MakeWorkOrder(
                        new AllAlignments.Parameter(document, parameter.AlignmentTarget));
                }

                if (parameter.PeakScoringModel != null)
                {
                    yield return ScoringResults.PRODUCER.MakeWorkOrder(
                        new ScoringResults.Parameters(parameter.Document, parameter.PeakScoringModel, parameter.ManualPeakTreatment == ManualPeakTreatment.OVERWRITE));
                }
            }

            private IEnumerable<MoleculePeaks> GetRows(CancellationToken cancellationToken, Parameters parameters, ScoringResults scoringResults, IEnumerable<ResultFileInfo> resultFileInfos)
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
                        var peaks = new List<AlignedPeak>();
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
                                var peak = new AlignedPeak(peakResultFile, rawPeakBounds, peakFeatureStatistics?.BestScore,
                                    manuallyIntegrated);
                                peaks.Add(peak);
                            }

                        }

                        yield return new MoleculePeaks(peptideIdentityPath, peaks);
                    }
                }
            }
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
                return ChangeProp(ImClone(this), im => { im.AlignmentTarget = alignmentTarget; });
            }

            public PeakScoringModelSpec PeakScoringModel { get; private set; }
            public int MinCoreCount { get; private set; }
            public CutoffTypeEnum CutoffType { get; private set; }
            public double? ScoreCutoff { get; private set; }
            public double? RetentionTimeDeviationCutoff { get; private set; }

            public Parameters ChangeScoringModel(PeakScoringModelSpec model, int minCoreCount,
                CutoffTypeEnum cutoffType, double? scoreCutoff,
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
        public static double ZScoreToPValue(double zScore)
        {
            return 1 - Normal.CDF(0, 1, zScore);
        }

        public static double PValueToZScore(double pValue)
        {
            return Normal.InvCDF(0, 1, 1 - pValue);
        }

        public static IEnumerable<ResultFileInfo> GetResultFileInfos(SrmDocument document,
            AllAlignments allAlignments)
        {
            var measuredResults = document.MeasuredResults;
            if (measuredResults == null)
            {
                yield break;
            }

            for (int i = 0; i < measuredResults.Chromatograms.Count; i++)
            {
                var chromatogramSet = measuredResults.Chromatograms[i];
                foreach (var chromFileInfo in chromatogramSet.MSDataFileInfos)
                {
                    var alignmentFunction = allAlignments?.GetAlignmentFunction(chromFileInfo.FilePath) ??
                                            AlignmentFunction.IDENTITY;

                    yield return new ResultFileInfo(i, new ReplicateFileId(chromatogramSet.Id, chromFileInfo.FileId), chromFileInfo.FilePath, alignmentFunction);
                }
            }
        }

        public static bool IsManualIntegrated(PeptideDocNode peptideDocNode, int replicateIndex,
            ChromFileInfoId fileId)
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
    }
}