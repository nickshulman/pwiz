/*
 * Original author: Dario Amodei <damodei .at. standard.edu>,
 *                  Mallick Lab, Department of Radiology, Stanford
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
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.Extensions;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Model.Results.Scoring.Tric;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model
{

    /// <summary>
    /// Computes mProphet scores for current document and model
    /// and handles exporting them to a file or using them to 
    /// adjust peak boundaries.
    /// </summary>
    public class MProphetResultsHandler
    {
        private readonly FeatureCalculators _calcs;
        private PeakTransitionGroupFeatureSet _features;

        private FeatureStatisticDictionary _featureDictionary;

        private const string Q_VALUE_ANNOTATION = "QValue"; // : for now, we are not localizing column headers

        public static string AnnotationName { get { return AnnotationDef.ANNOTATION_PREFIX + Q_VALUE_ANNOTATION; } }

        public static string MAnnotationName { get { return AnnotationDef.ANNOTATION_PREFIX + @"Score"; } }

        public MProphetResultsHandler(SrmDocument document, PeakScoringModelSpec scoringModel,
            PeakTransitionGroupFeatureSet features = null)
        {
            Document = document;
            ScoringModel = scoringModel;
            _calcs = ScoringModel != null
                ? ScoringModel.PeakFeatureCalculators
                : FeatureCalculators.ALL;
            _features = features;
            _featureDictionary = FeatureStatisticDictionary.EMPTY;

            // Legacy defaults
            QValueCutoff = double.MaxValue;
            OverrideManual = true;
            IncludeDecoys = true;
        }

        public SrmDocument Document { get; private set; }

        public string DocumentPath { get; set; }

        public PeakScoringModelSpec ScoringModel { get; private set; }

        public double QValueCutoff { get; set; }
        public bool OverrideManual { get; set; }
        public bool IncludeDecoys { get; set; }
        public bool UseTric { get; set; }

        /// <summary>
        /// Forces the release of memory by breaking immutability to avoid doubling
        /// the document size in memory during command-line processing
        /// </summary>
        public bool FreeImmutableMemory { get; set; }

        public PeakFeatureStatistics GetPeakFeatureStatistics(Peptide peptide, ChromFileInfoId fileId)
        {
            var key = new PeakTransitionGroupIdKey(peptide, fileId);
            PeakFeatureStatistics peakStatistics;
            if (_featureDictionary.TryGetValue(key, out peakStatistics))
            {
                return peakStatistics;
            }
            return null;
        }

        public void ScoreFeatures(IProgressMonitor progressMonitor = null, bool releaseRawFeatures = false, TextWriter output = null)
        {
            if (_features == null)
            {
                _features = Document.GetPeakFeatures(_calcs, progressMonitor);
            }
            if (ScoringModel == null)
                return;
            _featureDictionary =
                FeatureStatisticDictionary.MakeFeatureDictionary(ChromFileInfoIndex.FromChromatogramSets(Document.MeasuredResults.Chromatograms), ScoringModel, _features,
                    releaseRawFeatures && !UseTric);
            if (UseTric)
            {
                var replacements = Tric.Rescore(ScoringModel,
                    _featureDictionary,
                    _features,
                    progressMonitor,
                    DocumentPath,
                    output);
                _featureDictionary = _featureDictionary.ReplaceValues(replacements);
            }
            if (releaseRawFeatures)
                _features = null;   // Done with this memory
        }

        public bool IsMissingScores()
        {
            return _featureDictionary.MissingScores;
        }

        public SrmDocument ChangePeaks(IProgressMonitor progressMonitor = null)
        {
            var settingsChangeMonitor = progressMonitor != null
                ? new SrmSettingsChangeMonitor(progressMonitor, Resources.MProphetResultsHandler_ChangePeaks_Adjusting_peak_boundaries)
                : null;
            using (settingsChangeMonitor)
            {
                var settingsNew = Document.Settings.ChangePeptideIntegration(integration =>
                    integration.ChangeResultsHandler(this));
                // Only update the document if anything has changed
                var docNew = Document.ChangeSettings(settingsNew, settingsChangeMonitor);
                if (!Equals(docNew.Settings.PeptideSettings.Integration, Document.Settings.PeptideSettings.Integration) ||
                    !ArrayUtil.ReferencesEqual(docNew.Children, Document.Children))
                {
                    Document = docNew;
                }
                return Document;
            }
        }

        public void WriteScores(TextWriter writer,
            CultureInfo cultureInfo,
            FeatureCalculators calcs = null,
            bool bestOnly = true,
            bool includeDecoys = true,
            IProgressMonitor progressMonitor = null)
        {
            PeakTransitionGroupFeatureSet features;
            if (calcs == null)
            {
                calcs = _calcs;
                features = _features;
            }
            else
            {
                features = Document.GetPeakFeatures(calcs, progressMonitor, true);
            }
            WriteHeaderRow(writer, calcs, cultureInfo);
            foreach (var peakTransitionGroupFeatures in features.Features)
            {
                WriteTransitionGroup(writer,
                                     peakTransitionGroupFeatures,
                                     cultureInfo,
                                     bestOnly,
                                     includeDecoys);
            }
        }

        private static void WriteHeaderRow(TextWriter writer, IEnumerable<IPeakFeatureCalculator> calcs, CultureInfo cultureInfo)
        {
            char separator = TextUtil.GetCsvSeparator(cultureInfo);
            // ReSharper disable LocalizableElement
            var namesArray = new List<string>
                {
                    "transition_group_id",
                    "run_id",
                    "FileName",
                    "RT",
                    "MinStartTime",
                    "MaxEndTime",
                    "Sequence",
                    "PeptideModifiedSequence",
                    "ProteinName",
                    "PrecursorIsDecoy",
                    "mProphetScore",
                    "pValue",
                    "qValue"
                };
            // ReSharper restore LocalizableElement

            foreach (var name in namesArray)
            {
                writer.WriteDsvField(name, separator);
                writer.Write(separator);
            }
            bool first = true;
            foreach (var peakFeatureCalculator in calcs)
            {
                if (!first)
                    writer.Write(separator);
                writer.Write(first ? @"main_var_{0}" : @"var_{0}", peakFeatureCalculator.HeaderName.Replace(@" ", @"_"));
                first = false;
            }
            writer.WriteLine();
        }

        private void WriteTransitionGroup(TextWriter writer,
                                          PeakTransitionGroupFeatures features,
                                          CultureInfo cultureInfo,
                                          bool bestOnly,
                                          bool includeDecoys)
        {
            var id = features.Key;
            int bestScoresIndex = -1;
            IList<float> mProphetScores = null;
            IList<float> pValues = null;
            double qValue = double.NaN;
            PeakFeatureStatistics peakFeatureStatistics;
            if (_featureDictionary.TryGetValue(id, out peakFeatureStatistics))
            {
                bestScoresIndex = peakFeatureStatistics.BestScoreIndex;
                mProphetScores = peakFeatureStatistics.MprophetScores;
                pValues = peakFeatureStatistics.PValues;
                qValue = peakFeatureStatistics.QValue ?? double.NaN;
            }
            if (features.IsDecoy && !includeDecoys)
                return;
            int j = 0;
            foreach (var peakGroupFeatures in features.PeakGroupFeatures)
            {
                if (!bestOnly || j == bestScoresIndex)
                {
                    double mProphetScore = mProphetScores != null ? mProphetScores[j] : double.NaN;
                    double pValue = pValues != null ? pValues[j] : double.NaN;
                    WriteRow(writer, features, peakGroupFeatures, cultureInfo, mProphetScore,
                             pValue, qValue);
                }
                ++j;
            }
        }

        private static void WriteRow(TextWriter writer,
                                     PeakTransitionGroupFeatures features,
                                     PeakGroupFeatures peakGroupFeatures,
                                     CultureInfo cultureInfo,
                                     double mProphetScore,
                                     double pValue,
                                     double qValue)
        {
            char separator = TextUtil.GetCsvSeparator(cultureInfo);
            string fileName = SampleHelp.GetFileName(features.Id.FilePath);
            var fieldsArray = new List<string>
                {
                    Convert.ToString(features.Id, cultureInfo),
                    Convert.ToString(features.Id.Run, cultureInfo),
                    fileName,
                    // CONSIDER: This impacts memory consumption for large-scale DIA, and it is not clear anyone uses these
                    Convert.ToString(peakGroupFeatures.RetentionTime, cultureInfo),
                    Convert.ToString(peakGroupFeatures.StartTime, cultureInfo),
                    Convert.ToString(peakGroupFeatures.EndTime, cultureInfo),
                    features.Id.RawUnmodifiedTextId, // Unmodified sequence, or custom ion name
                    features.Id.RawTextId, // Modified sequence, or custom ion name
                    features.Id.NodePepGroup.Name,
                    Convert.ToString(features.IsDecoy ? 1 : 0, cultureInfo),
                    ToFieldString((float) mProphetScore, cultureInfo),
                    ToFieldString((float) pValue, cultureInfo),
                    ToFieldString((float) qValue, cultureInfo)
                };

            foreach (var name in fieldsArray)
            {
                writer.WriteDsvField(name, separator);
                writer.Write(separator);
            }
            bool first = true;
            foreach (float featureColumn in peakGroupFeatures.Features)
            {
                if (!first)
                    writer.Write(separator);
                writer.WriteDsvField(ToFieldString(featureColumn, cultureInfo), separator);
                first = false;
            }
            writer.WriteLine();
        }

        private static string ToFieldString(float f, IFormatProvider cultureInfo)
        {
            return float.IsNaN(f) ? TextUtil.EXCEL_NA : Convert.ToString(f, cultureInfo);
        }
    }

    public class PeakFeatureStatistics : Immutable
    {
        public PeakFeatureStatistics(PeakTransitionGroupFeatures features, IList<float> mprophetScores, IList<float> pvalues,
            int bestScoreIndex, float bestScore, float? qValue)
        {
            MprophetScores = ImmutableList.ValueOf(mprophetScores);    // May only be present for writing features
            PValues = ImmutableList.ValueOf(pvalues);
            BestPeakIndex = features.PeakGroupFeatures[bestScoreIndex].OriginalPeakIndex;
            BestScoreIndex = bestScoreIndex;
            BestScore = bestScore;
            QValue = qValue;
            BestFeatureScores = features.PeakGroupFeatures[bestScoreIndex].FeatureScores;
        }

        public ImmutableList<float> MprophetScores { get; private set; }
        public ImmutableList<float> PValues { get; private set; }
        public int BestPeakIndex { get; private set; }
        public int BestScoreIndex { get; private set; }
        public float BestScore { get; private set; }
        public float? QValue { get; private set; }

        [Pure]
        public PeakFeatureStatistics ChangeQValue(float? qValue)
        {
            return ChangeProp(ImClone(this), im => im.QValue = qValue);
        }

        [Pure]
        public PeakFeatureStatistics ResetBestScores(float score, double qValue)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.QValue = (float)qValue;
                im.BestScore = score;
                im.MprophetScores = im.MprophetScores.ReplaceAt(BestScoreIndex, score);
            });
        }

        [Pure]
        public PeakFeatureStatistics ResetBestPeak(int bestIndex, PeakTransitionGroupFeatures features)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.BestScoreIndex = bestIndex;
                im.BestPeakIndex = features.PeakGroupFeatures[bestIndex].OriginalPeakIndex;
                im.MprophetScores = ImmutableList.ValueOf(Enumerable.Range(0, MprophetScores.Count)
                    .Select(i => i == bestIndex ? MprophetScores[i] : 0));
            });
        }

        [Pure]
        public PeakFeatureStatistics SetNoBestPeak()
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.BestScoreIndex = -1;
                im.BestPeakIndex = -1;
                im.MprophetScores = ImmutableList.ValueOf(new float[MprophetScores.Count]);
                im.QValue = 1;
            });
        }
        public FeatureScores BestFeatureScores { get; }
    }
}
