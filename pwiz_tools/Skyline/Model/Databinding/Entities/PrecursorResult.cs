﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2013 University of Washington - Seattle, WA
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
using System.ComponentModel;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.PeakFinding;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    [InvariantDisplayName(nameof(PrecursorResult))]
    [AnnotationTarget(AnnotationDef.AnnotationTarget.precursor_result)]
    public class PrecursorResult : Result
    {
        private readonly CachedValue<TransitionGroupChromInfo> _chromInfo;
        private readonly CachedValue<PrecursorQuantificationResult> _quantificationResult;
        private readonly CachedValue<IList<CandidatePeakGroup>> _candidatePeaks;
        private readonly CachedValue<PeakGroupScore> _peakScores;
        public PrecursorResult(Precursor precursor, ResultFile file) : base(precursor, file)
        {
            _chromInfo = CachedValue.Create(DataSchema, ()=>GetResultFile().FindChromInfo(precursor.DocNode.Results));
            _quantificationResult = CachedValue.Create(DataSchema, GetQuantification);
            _candidatePeaks = CachedValue.Create(DataSchema, GetCandidatePeakGroups);
            _peakScores = CachedValue.Create(DataSchema, GetMyPeakScore);
        }

        [HideWhen(AncestorOfType = typeof(Precursor))]
        public Precursor Precursor { get { return SkylineDocNode as Precursor; } }
        [Browsable(false)]
        public TransitionGroupChromInfo ChromInfo { get { return _chromInfo.Value; } }
        public void ChangeChromInfo(EditDescription editDescription, Func<TransitionGroupChromInfo, TransitionGroupChromInfo> newChromInfo)
        {
            Precursor.ChangeDocNode(editDescription, docNode => docNode.ChangeResults(GetResultFile()
                .ChangeChromInfo(docNode.Results, newChromInfo)));
        }
        [Format(Formats.PValue, NullValue = TextUtil.EXCEL_NA)]
        public double? DetectionQValue { get { return ChromInfo.QValue; } }
        [Format(Formats.STANDARD_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double? DetectionZScore { get { return ChromInfo.ZScore; } }
        [Format(Formats.PEAK_FOUND_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double PrecursorPeakFoundRatio { get { return ChromInfo.PeakCountRatio; } }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? BestRetentionTime { get { return ChromInfo.RetentionTime; } }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? MaxFwhm { get { return ChromInfo.Fwhm; } }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? MinStartTime { get { return ChromInfo.StartRetentionTime; } }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? MaxEndTime { get { return ChromInfo.EndRetentionTime; } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalArea { get { return ChromInfo.Area; } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalAreaMs1 { get { return ChromInfo.AreaMs1; } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalAreaFragment { get { return ChromInfo.AreaFragment; } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalBackground { get { return ChromInfo.BackgroundArea; } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalBackgroundMs1 { get { return ChromInfo.BackgroundAreaMs1; } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalBackgroundFragment { get { return ChromInfo.BackgroundAreaFragment; } }
        [Format(Formats.STANDARD_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalAreaRatio { get { return ChromInfo.Ratio; } }
        [Format(Formats.STANDARD_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double? RatioDotProduct { get { return RatioValue.GetDotProduct(ChromInfo.Ratios.FirstOrDefault()); } }
        [Format(Formats.PEAK_AREA_NORMALIZED, NullValue = TextUtil.EXCEL_NA)]
        public double? TotalAreaNormalized { get { return TotalArea / GetResultFile().GetTotalArea(Precursor.IsotopeLabelType); } }
        [Format(Formats.PEAK_AREA, NullValue = TextUtil.EXCEL_NA)]
        public double? MaxHeight {get { return ChromInfo.Height; }}
        [Format(Formats.MASS_ERROR, NullValue = TextUtil.EXCEL_NA)]
        public double? AverageMassErrorPPM { get { return ChromInfo.MassError; } }
        [Format(NullValue = TextUtil.EXCEL_NA)]
        public int? CountTruncated { get { return ChromInfo.Truncated; } }
        public PeakIdentification Identified { get { return ChromInfo.Identified; } }
        [Format(Formats.STANDARD_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double? LibraryDotProduct { get { return ChromInfo.LibraryDotProduct; } }
        [Format(Formats.STANDARD_RATIO, NullValue = TextUtil.EXCEL_NA)]
        public double? IsotopeDotProduct { get { return ChromInfo.IsotopeDotProduct; } }
        public UserSet UserSetTotal { get { return ChromInfo.UserSet; } }
        [Format(NullValue = TextUtil.EXCEL_NA)]
        public int OptStep { get { return ChromInfo.OptimizationStep; } }
        [Format(Formats.OPT_PARAMETER, NullValue = TextUtil.EXCEL_NA)]
        public double? OptCollisionEnergy
        {
            get
            {
                var ceRegression = GetResultFile().Replicate.ChromatogramSet.OptimizationFunction as CollisionEnergyRegression;
                if (null == ceRegression)
                {
                    return null;
                }
                return ceRegression.GetCollisionEnergy(Precursor.DocNode.PrecursorAdduct, Precursor.GetRegressionMz(), ChromInfo.OptimizationStep);
            }
        }
        [Format(Formats.OPT_PARAMETER, NullValue = TextUtil.EXCEL_NA)]
        public double? OptDeclusteringPotential 
        { 
            get 
            {
                var dpRegression = GetResultFile().Replicate.ChromatogramSet.OptimizationFunction as DeclusteringPotentialRegression;
                if (null == dpRegression)
                {
                    return null;
                }
                return dpRegression.GetDeclustringPotential(Precursor.GetRegressionMz(), ChromInfo.OptimizationStep);
            }
        }
        [Format(Formats.OPT_PARAMETER, NullValue = TextUtil.EXCEL_NA)]
        public double? OptCompensationVoltage
        {
            get
            {
                var optimizationFunction = GetResultFile().Replicate.ChromatogramSet.OptimizationFunction;
                var covRegression = optimizationFunction as CompensationVoltageParameters;
                if (null == covRegression)
                {
                    return null;
                }
                return SrmDocument.GetCompensationVoltage(Precursor.Peptide.DocNode, Precursor.DocNode, null, ChromInfo.OptimizationStep, covRegression.TuneLevel);
            }
        }

        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? CollisionalCrossSection { get { return ChromInfo.IonMobilityInfo.CollisionalCrossSection; } }

        [Obsolete("use IonMobilityMS1 instead")] 
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? DriftTimeMS1 { get { return ChromInfo.IonMobilityInfo.DriftTimeMS1; } }

        [Obsolete("use IonMobilityFragment instead")] 
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? DriftTimeFragment { get { return ChromInfo.IonMobilityInfo.DriftTimeFragment; } }

        [Obsolete("use IonMobilityWindow instead")]
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? DriftTimeWindow { get { return ChromInfo.IonMobilityInfo.DriftTimeWindow; } }

        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? IonMobilityMS1 { get { return ChromInfo.IonMobilityInfo.IonMobilityMS1; } }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? IonMobilityFragment { get { return ChromInfo.IonMobilityInfo.IonMobilityFragment; } }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? IonMobilityWindow { get { return ChromInfo.IonMobilityInfo.IonMobilityWindow; } }

        [Format(NullValue = TextUtil.EXCEL_NA)]
        public string IonMobilityUnits { get { return IonMobilityFilter.IonMobilityUnitsL10NString(ChromInfo.IonMobilityInfo.IonMobilityUnits); } }

        public LinkValue<PrecursorQuantificationResult> PrecursorQuantification
        {
            get
            {
                return new LinkValue<PrecursorQuantificationResult>(_quantificationResult.Value, (sender, args) =>
                {
                    SkylineWindow skylineWindow = DataSchema.SkylineWindow;
                    if (skylineWindow != null)
                    {
                        skylineWindow.ShowCalibrationForm();
                        skylineWindow.SelectedResultsIndex = GetResultFile().Replicate.ReplicateIndex;
                        skylineWindow.SelectedPath = Precursor.IdentityPath;
                        Properties.Settings.Default.CalibrationCurveOptions.SingleBatch = true;
                    }
                });
            }
        }


        [InvariantDisplayName("PrecursorReplicateNote")]
        [Importable]
        public string Note 
        { 
            get { return ChromInfo.Annotations.Note; } 
            set
            {
                ChangeChromInfo(EditColumnDescription(nameof(Note), value),
                    chromInfo=>chromInfo.ChangeAnnotations(chromInfo.Annotations.ChangeNote(value)));
            }
        }

        public override void SetAnnotation(AnnotationDef annotationDef, object value)
        {
            // Ignore setting of the old q value and mProphet score annoations. They are
            // displayed for backward compatibility. Setting them manually never made and sense.
            if (Equals(annotationDef.Name, MProphetResultsHandler.AnnotationName) ||
                Equals(annotationDef.Name, MProphetResultsHandler.MAnnotationName))
                return;

            ChangeChromInfo(EditDescription.SetAnnotation(annotationDef, value), 
                chromInfo=>chromInfo.ChangeAnnotations(chromInfo.Annotations.ChangeAnnotation(annotationDef, value)));
        }

        public override object GetAnnotation(AnnotationDef annotationDef)
        {
            // Return q value and mProphet scores from their new locations
            if (Equals(annotationDef.Name, MProphetResultsHandler.AnnotationName))
                return DetectionQValue;
            else if (Equals(annotationDef.Name, MProphetResultsHandler.MAnnotationName))
                return DetectionZScore;

            return DataSchema.AnnotationCalculator.GetAnnotation(annotationDef, this, ChromInfo.Annotations);
        }

        public override string ToString()
        {
            return string.Format(@"{0:0}", ChromInfo.Area);
        }

        private PeptideResult _peptideResult;
        [HideWhen(AncestorOfType = typeof(Precursor))]
        public PeptideResult PeptideResult 
        {
            get { return _peptideResult = _peptideResult ?? new PeptideResult(Precursor.Peptide, GetResultFile()); }
        }

        [InvariantDisplayName("PrecursorResultLocator")]
        public string Locator { get { return GetLocator(); } }

        public override ElementRef GetElementRef()
        {
            return PrecursorResultRef.PROTOTYPE.ChangeChromInfo(GetResultFile().Replicate.ChromatogramSet, ChromInfo)
                .ChangeParent(Precursor.GetElementRef());
        }

        public override bool IsEmpty()
        {
            return !ChromInfo.RetentionTime.HasValue;
        }
        private PrecursorQuantificationResult GetQuantification()
        {
            var calibrationCurveFitter = PeptideResult.GetCalibrationCurveFitter();
            return calibrationCurveFitter.GetPrecursorQuantificationResult(GetResultFile().Replicate.ReplicateIndex,
                Precursor.DocNode);
        }

        [OneToMany(ItemDisplayName = nameof(CandidatePeakGroup))]
        public IList<CandidatePeakGroup> CandidatePeakGroups
        {
            get
            {
                return _candidatePeaks.Value;
            }
        }

        public PeakGroupScore PeakScore
        {
            get
            {
                return _peakScores.Value;
            }
        }

        public int GetCandidatePeakGroupCount()
        {
            return new ChromatogramGroup(this).ChromatogramGroupInfo?.NumPeaks ?? 0;
        }

        private IList<CandidatePeakGroup> GetCandidatePeakGroups()
        {
            var chromatogramGroup = new ChromatogramGroup(this);
            var peakGroups = new List<CandidatePeakGroup>();
            var onDemandScoreCalculator = new OnDemandFeatureCalculator(FeatureCalculators.ALL, SrmDocument,
                Precursor.Peptide.DocNode, GetResultFile().Replicate.ReplicateIndex, GetResultFile().ChromFileInfo);
            foreach (var peakScores in onDemandScoreCalculator.CalculateCandidatePeakScores(Precursor.DocNode,
                chromatogramGroup.ChromatogramGroupInfo))
            {
                var peakGroup = new CandidatePeakGroup(chromatogramGroup, peakGroups.Count, GetPeakScores(peakScores));
                peakGroup.UpdateChosen(this);
                peakGroups.Add(peakGroup);

            }
            return ImmutableList.ValueOf(peakGroups);
        }

        public PeakGroupScore GetPeakScores(FeatureValues featureValues)
        {
            var context = new PeakScoringContext(SrmDocument);
            var nodePep = Precursor.Peptide.DocNode;
            var comparableGroup = FindComparableGroup();
            var model = SrmDocument.Settings.PeptideSettings.Integration.PeakScoringModel;
            if (model == null || !model.IsTrained)
            {
                model = LegacyScoringModel.DEFAULT_MODEL;
            }
            return PeakGroupScore.MakePeakScores(featureValues, model);
        }

        private PeakGroupScore GetMyPeakScore()
        {
            var onDemandScoreCalculator = new OnDemandFeatureCalculator(FeatureCalculators.ALL, SrmDocument,
                Precursor.Peptide.DocNode, GetResultFile().Replicate.ReplicateIndex, GetResultFile().ChromFileInfo);
            var featureScores = onDemandScoreCalculator.CalculateTransitionGroupFeatureValues(Precursor.DocNode.TransitionGroup);
            return GetPeakScores(featureScores);
        }

        private PeakBounds FindPeakBounds(TransitionGroup transitionGroup, Model.Transition transition)
        {
            var peptideDocNode = Precursor.Peptide.DocNode;
            var transitionDocNode = (TransitionDocNode) peptideDocNode.FindNode(new IdentityPath(transitionGroup, transition));
            var transitionChromInfo = GetResultFile().FindChromInfo(transitionDocNode?.Results);
            if (transitionChromInfo == null || transitionChromInfo.IsEmpty)
            {
                return null;
            }

            return new PeakBounds(transitionChromInfo.StartRetentionTime, transitionChromInfo.EndRetentionTime);
        }

        /// <summary>
        /// Returns the set of TransitionGroups under the peptide that participate with each other in peak finding.
        /// </summary>
        private ImmutableList<TransitionGroupDocNode> FindComparableGroup()
        {
            var transitionGroup = Precursor.DocNode.TransitionGroup;
            foreach (var groupDocNodes in PeakFeatureEnumerator.ComparableGroups(Precursor.Peptide.DocNode))
            {
                var groupList = ImmutableList.ValueOf(groupDocNodes);
                if (groupList.Any(docNode => ReferenceEquals(transitionGroup, docNode.TransitionGroup)))
                {
                    return groupList;
                }
            }

            Assume.Fail(@"Comparable group not found");
            return null;
        }
    }
}
