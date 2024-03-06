﻿/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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
using pwiz.Common.Collections;
using ZedGraph;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Controls.Graphs
{
    /// <summary>
    /// Classes and methods for transforming values that are displayed in graphs based on the 
    /// current settings.
    /// </summary>
    public static class GraphValues
    {
        /// <summary>
        /// Prepend the string "Log " to an axis title.
        /// </summary>
        [Localizable(true)]
        public static string AnnotateLogAxisTitle(string title)
        {
            return string.Format(GraphsResources.GraphValues_Log_AxisTitle, title);
        }
        
        [Localizable(true)]
        public static string ToLocalizedString(RTPeptideValue rtPeptideValue)
        {
            switch (rtPeptideValue)
            {
                case RTPeptideValue.All:
                case RTPeptideValue.Retention:
                    return GraphsResources.RtGraphValue_Retention_Time;
                case RTPeptideValue.FWB:
                    return GraphsResources.RtGraphValue_FWB_Time;
                case RTPeptideValue.FWHM:
                    return GraphsResources.RtGraphValue_FWHM_Time;
            }
            throw new ArgumentException(rtPeptideValue.ToString());
        }

        /// <summary>
        /// Operations for combining values from multiple replicates that are displayed 
        /// at a single point on a graph.
        /// </summary>
        public class AggregateOp
        {
            public static readonly AggregateOp MEAN = new AggregateOp(false, false);
            public static readonly AggregateOp CV = new AggregateOp(true, false);
            public static readonly AggregateOp CV_DECIMAL = new AggregateOp(true, true);

            public static AggregateOp FromCurrentSettings()
            {
                if (!Settings.Default.ShowPeptideCV)
                {
                    return MEAN;
                }
                return Settings.Default.PeakDecimalCv ? CV_DECIMAL : CV;
            }

            private AggregateOp(bool cv, bool cvDecimal)
            {
                Cv = cv;
                CvDecimal = cvDecimal;
            }

            public bool Cv { get; private set; }
            public bool CvDecimal { get; private set; }

            public PointPair MakeBarValue(double xValue, IEnumerable<double> values)
            {
                var statValues = new Statistics(values);
                if (statValues.Length == 0)
                    return MeanErrorBarItem.MakePointPair(xValue, PointPairBase.Missing, PointPairBase.Missing);
                if (Cv)
                {
                    var cv = statValues.StdDev()/statValues.Mean();
                    return MeanErrorBarItem.MakePointPair(xValue, CvDecimal ? cv : cv*100, 0);
                }
                return MeanErrorBarItem.MakePointPair(xValue, statValues.Mean(), statValues.StdDev());
            }

            [Localizable(true)]
            public string AnnotateTitle(string title)
            {
                if (Cv)
                {
                    if (CvDecimal)
                    {
                        title = string.Format(GraphsResources.AggregateOp_AxisTitleCv, title);
                    }
                    else
                    {
                        title = string.Format(GraphsResources.AggregateOp_AxisTitleCvPercent, title);
                    }
                }
                return title;
            }
        }

        /// <summary>
        /// A scaling of a retention time
        /// </summary>
        public interface IRetentionTimeTransformOp
        {
            /// <summary>
            /// Returns the localized text to display on a graph's axis for the transformed value
            /// </summary>
            string GetAxisTitle(RTPeptideValue rtPeptideValue);
            /// <summary>
            /// Try to get the regression function to use for the specified file.
            /// If the file is not supposed to be transformed (for instance, if this
            /// transform is a retention time alignment to that particular file), then
            /// regressionFunction will be set to null, and this method will return true.
            /// If the regressionFunction cannot be found for some other reason, then this method
            /// returns false.
            /// If successful, then this method returns true, and the regressionFunction is set 
            /// appropriately.
            /// </summary>
            bool TryGetRegressionFunction(ChromFileInfoId chromFileInfoId, out AlignmentFunction regressionFunction);
        }

        public class RegressionUnconversion : IRetentionTimeTransformOp
        {
            private readonly RetentionTimeRegression _retentionTimeRegression;
            public RegressionUnconversion(RetentionTimeRegression retentionTimeRegression)
            {
                _retentionTimeRegression = retentionTimeRegression;
            }

            public string GetAxisTitle(RTPeptideValue rtPeptideValue)
            {
                string calculatorName = _retentionTimeRegression.Calculator.Name;
                if (rtPeptideValue == RTPeptideValue.Retention || rtPeptideValue == RTPeptideValue.All)
                {
                    return string.Format(GraphsResources.RegressionUnconversion_CalculatorScoreFormat, calculatorName);
                }
                return string.Format(GraphsResources.RegressionUnconversion_CalculatorScoreValueFormat, calculatorName, ToLocalizedString(rtPeptideValue));
            }

            public bool TryGetRegressionFunction(ChromFileInfoId chromFileInfoId, out AlignmentFunction regressionFunction)
            {
                var unconversion = _retentionTimeRegression.GetUnconversion(chromFileInfoId);
                if (unconversion != null)
                {
                    regressionFunction = AlignmentFunction.Define(unconversion.GetY, unconversion.GetX);
                    return true;
                }

                regressionFunction = null;
                return false;
            }
        }

        /// <summary>
        /// Holds information about how to align retention times before displaying them in a graph.
        /// </summary>
        public class AlignToFileOp : IRetentionTimeTransformOp
        {
            public static AlignToFileOp GetAlignmentToFile(ChromFileInfoId chromFileInfoId, SrmSettings settings)
            {
                if (!settings.HasResults)
                {
                    return null;
                }
                var chromSetInfos = GetChromSetInfos(settings.MeasuredResults);
                Tuple<ChromatogramSet, ChromFileInfo> chromSetInfo;
                if (!chromSetInfos.TryGetValue(chromFileInfoId, out chromSetInfo))
                {
                    return null;
                }
                var fileRetentionTimeAlignments = settings.DocumentRetentionTimes.FileAlignments.Find(chromSetInfo.Item2);
                if (null == fileRetentionTimeAlignments)
                {
                    return null;
                }
                return new AlignToFileOp(chromSetInfo.Item1, chromSetInfo.Item2, settings.DocumentRetentionTimes, fileRetentionTimeAlignments.Name, chromSetInfos);
            }

            private static IDictionary<ReferenceValue<ChromFileInfoId>, Tuple<ChromatogramSet, ChromFileInfo>> GetChromSetInfos(
                MeasuredResults measuredResults)
            {
                var dict =
                    new Dictionary<ReferenceValue<ChromFileInfoId>, Tuple<ChromatogramSet, ChromFileInfo>>();
                foreach (var chromatogramSet in measuredResults.Chromatograms)
                {
                    foreach (var chromFileInfo in chromatogramSet.MSDataFileInfos)
                    {
                        dict.Add(chromFileInfo.FileId, new Tuple<ChromatogramSet, ChromFileInfo>(chromatogramSet, chromFileInfo));
                    }
                }
                return dict;
            }

            private readonly IDictionary<ReferenceValue<ChromFileInfoId>, Tuple<ChromatogramSet, ChromFileInfo>> _chromSetInfos;
            private AlignToFileOp(ChromatogramSet chromatogramSet, ChromFileInfo chromFileInfo, 
                DocumentRetentionTimes documentRetentionTimes,
                string alignTo,
                IDictionary<ReferenceValue<ChromFileInfoId>, Tuple<ChromatogramSet, ChromFileInfo>> chromSetInfos)
            {
                ChromatogramSet = chromatogramSet;
                ChromFileInfo = chromFileInfo;
                DocumentRetentionTimes = documentRetentionTimes;
                AlignTo = alignTo;
                _chromSetInfos = chromSetInfos;
            }

            public ChromatogramSet ChromatogramSet { get; private set; }
            public ChromFileInfo ChromFileInfo { get; private set; }
            public DocumentRetentionTimes DocumentRetentionTimes { get; private set; }
            public string AlignTo { get; private set; }
            private static readonly int MAX_STOPOVERS = 3;
            public bool TryGetRegressionFunction(ChromFileInfoId chromFileInfoId, out AlignmentFunction regressionFunction)
            {
                if (ReferenceEquals(chromFileInfoId, ChromFileInfo.Id))
                {
                    regressionFunction = null;
                    return true;
                }

                regressionFunction = null;
                if (!_chromSetInfos.TryGetValue(chromFileInfoId, out var chromSetInfo))
                {
                    return false;
                }

                var alignFromName = DocumentRetentionTimes.FileAlignments.Find(chromSetInfo.Item2)?.Name;
                if (alignFromName == null)
                {
                    return false;
                }

                regressionFunction = DocumentRetentionTimes.GetMappingFunction(AlignTo, alignFromName, MAX_STOPOVERS);
                return regressionFunction != null;
            }

            /// <summary>
            /// If retention time alignment is being performed, append "aligned to {ReplicateName}" to the title.
            /// </summary>
            [Localizable(true)]
            public string GetAxisTitle(RTPeptideValue rtPeptideValue)
            {
                return string.Format(GraphsResources.RtAlignment_AxisTitleAlignedTo, ToLocalizedString(rtPeptideValue), ChromatogramSet.Name);
            }
        }

        public class RetentionTimeTransform
        {
            public RetentionTimeTransform(RTPeptideValue rtPeptideValue, IRetentionTimeTransformOp rtAlignment, AggregateOp aggregateOp)
            {
                RtPeptideValue = rtPeptideValue;
                RtTransformOp = rtAlignment;
                AggregateOp = aggregateOp;
            }

            public RTPeptideValue RtPeptideValue { get; private set; }
            public IRetentionTimeTransformOp RtTransformOp { get; private set; }
            public AggregateOp AggregateOp { get; private set; }

            [Localizable(true)]
            public string GetAxisTitle()
            {
                string title;
                if (null != RtTransformOp)
                {
                    title = RtTransformOp.GetAxisTitle(RtPeptideValue);
                }
                else
                {
                    title = ToLocalizedString(RtPeptideValue);
                }
                title = AggregateOp.AnnotateTitle(title);
                return title;
            }
        }
    }
}
