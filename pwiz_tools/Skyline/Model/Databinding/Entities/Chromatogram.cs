﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2017 University of Washington - Seattle, WA
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
using System.ComponentModel;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class Chromatogram : AbstractChromatogram
    {
        private Lazy<ChromatogramInfo> _chromatogramInfo;
        public Chromatogram(PrecursorChromatogramGroup chromatogramGroup, Transition transition) : base(chromatogramGroup.DataSchema)
        {
            ChromatogramGroup = chromatogramGroup;
            Transition = transition;
            _chromatogramInfo = new Lazy<ChromatogramInfo>(GetChromatogramInfo);
        }

        protected override ChromatogramInfo ChromatogramInfo {get { return _chromatogramInfo.Value; }}
        
        [Browsable(false)]
        public PrecursorChromatogramGroup ChromatogramGroup { get; private set; }
        [Browsable(false)]
        public Transition Transition { get; private set; }

        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramStartTime
        {
            get { return _chromatogramInfo.Value?.Header.StartTime; }
        }
        [Format(Formats.RETENTION_TIME, NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramEndTime
        {
            get { return _chromatogramInfo.Value?.Header.EndTime; }
        }

        [Expensive]
        [ChildDisplayName("Raw{0}")]
        public Data RawData
        {
            get
            {
                var timeIntensitiesGroup = ChromatogramGroup.ReadTimeIntensitiesGroup();
                if (timeIntensitiesGroup is RawTimeIntensities)
                {
                    return new Data(timeIntensitiesGroup.TransitionTimeIntensities[_chromatogramInfo.Value.TransitionIndex], GetLazyMsDataFileScanIds());
                }
                return null;
            }
        }

        [Expensive]
        [ChildDisplayName("Interpolated{0}")]
        public Data InterpolatedData
        {
            get
            {
                var timeIntensitiesGroup = ChromatogramGroup.ReadTimeIntensitiesGroup();
                if (null == timeIntensitiesGroup)
                {
                    return null;
                }
                var rawTimeIntensities = timeIntensitiesGroup as RawTimeIntensities;
                if (null != rawTimeIntensities)
                {
                    var interpolatedTimeIntensities = rawTimeIntensities
                        .TransitionTimeIntensities[_chromatogramInfo.Value.TransitionIndex]
                        .Interpolate(rawTimeIntensities.GetInterpolatedTimes(), rawTimeIntensities.InferZeroes);
                    return new Data(interpolatedTimeIntensities, GetLazyMsDataFileScanIds());
                }
                var chromInfo = _chromatogramInfo.Value;
                if (null == chromInfo)
                {
                    return null;
                }
                return new Data(timeIntensitiesGroup.TransitionTimeIntensities[chromInfo.TransitionIndex], GetLazyMsDataFileScanIds());
            }
        }

        private ChromatogramInfo GetChromatogramInfo()
        {
            var chromatogramGroupInfo = ChromatogramGroup.ChromatogramGroupInfo;
            if (null == chromatogramGroupInfo)
            {
                return null;
            }
            float tolerance = (float) Transition.DataSchema.Document.Settings.TransitionSettings.Instrument.MzMatchTolerance;
            var chromatogramInfos = chromatogramGroupInfo.GetAllTransitionInfo(Transition.DocNode, tolerance,
                ChromatogramGroup.PrecursorResult.GetResultFile().Replicate.ChromatogramSet.OptimizationFunction, TransformChrom.raw);
            return chromatogramInfos.GetChromatogramForStep(0);
        }

        protected Lazy<MsDataFileScanIds> GetLazyMsDataFileScanIds()
        {
            return new Lazy<MsDataFileScanIds>(ChromatogramGroup.ReadMsDataFileScanIds);
        }

        public class Data
        {
            private TimeIntensities _timeIntensities;
            private Lazy<MsDataFileScanIds> _scanIds;
            public Data(TimeIntensities timeIntensities, Lazy<MsDataFileScanIds> scanIds)
            {
                _timeIntensities = timeIntensities;
                _scanIds = scanIds;
            }
            [Format(NullValue = TextUtil.EXCEL_NA)]
            public int NumberOfPoints { get { return _timeIntensities.NumPoints; } }
            [Format(Formats.RETENTION_TIME)]
            public FormattableList<float> Times { get { return new FormattableList<float>(_timeIntensities.Times); } }
            [Format(Formats.PEAK_AREA)]
            public FormattableList<float> Intensities { get { return new FormattableList<float>(_timeIntensities.Intensities); } }
            [Format(Formats.MASS_ERROR)]
            public FormattableList<float> MassErrors { get { return new FormattableList<float>(_timeIntensities.MassErrors); }}

            public FormattableList<string> SpectrumIds
            {
                get
                {
                    if (_timeIntensities.ScanIds == null || _scanIds == null)
                    {
                        return null;
                    }

                    var scanIds = _scanIds.Value;
                    if (scanIds == null)
                    {
                        return null;
                    }

                    return new FormattableList<string>(_timeIntensities.ScanIds
                        .Select(index => scanIds.GetMsDataFileSpectrumId(index)).ToArray());
                }
            }

            public override string ToString()
            {
                return string.Format(EntitiesResources.Data_ToString__0__points, NumberOfPoints);
            }
        }

        [Format(Formats.Mz, NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramPrecursorMz
        {
            get { return ChromatogramInfo == null ? (double?)null : ChromatogramInfo.PrecursorMz; }
        }

#if false         // TODO(nicksh): peaks
        public class Peak
        {
            public double RetentionTime { get; private set; }
            public double StartTime { get; private set; }
            public double EndTime { get; private set; }
            public double Area { get; private set; }
            public double BackgroundArea { get; private set; }
            public double Height { get; private set; }
            public double Fwhm { get; private set; }
            public int? PointsAcross { get; private set; }
            public bool FwhmDegenerate { get; private set; }
            public bool ForcedIntegration { get; private set; }
            public PeakIdentification PeakIdentification { get; private set; }
        }
#endif

    }
}
