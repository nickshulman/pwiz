using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public abstract class AbstractChromatogram : RootSkylineObject
    {
        private CachedValue<IList<CandidatePeak>> _candidatePeaks;
        protected AbstractChromatogram(SkylineDataSchema dataSchema) : base(dataSchema)
        {
            _candidatePeaks = new CachedValue<IList<CandidatePeak>>(dataSchema, GetCandidatePeaks);
        }


        [Browsable(false)]
        protected abstract ChromatogramInfo ChromatogramInfo { get; }


        [Format(Formats.Mz, NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramProductMz
        {
            get { return ChromatogramInfo == null ? (double?)null : ChromatogramInfo.ProductMz.RawValue; }
        }

        [Format(Formats.Mz, NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramExtractionWidth
        {
            get
            {
                return ChromatogramInfo == null ? null : ChromatogramInfo.ExtractionWidth;
            }
        }
        [Format(NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramIonMobility { get { return ChromatogramInfo == null ? null : ChromatogramInfo.IonMobility; } }
        [Format(NullValue = TextUtil.EXCEL_NA)]
        public double? ChromatogramIonMobilityExtractionWidth { get { return ChromatogramInfo == null ? null : ChromatogramInfo.IonMobilityExtractionWidth; } }
        [Format(NullValue = TextUtil.EXCEL_NA)]
        public string ChromatogramIonMobilityUnits
        {
            get
            {
                if (ChromatogramInfo == null)
                    return null;
                return IonMobilityFilter.IonMobilityUnitsL10NString(ChromatogramInfo.IonMobilityUnits);
            }
        }
        [Format(NullValue = TextUtil.EXCEL_NA)]
        public ChromSource? ChromatogramSource { get { return ChromatogramInfo == null ? (ChromSource?)null : ChromatogramInfo.Source; } }

        private IList<CandidatePeak> GetCandidatePeaks()
        {
            if (ChromatogramInfo == null)
            {
                return null;
            }
            return ChromatogramInfo.Peaks.Select(peak => new CandidatePeak(peak)).ToArray();
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
            public FormattableList<float> MassErrors { get { return new FormattableList<float>(_timeIntensities.MassErrors); } }
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
                return string.Format(Resources.Data_ToString__0__points, NumberOfPoints);
            }
        }

        public override string ToString()
        {
            if (ChromatogramInfo == null)
            {
                return TextUtil.EXCEL_NA;
            }

            return string.Format("{0}: {1}/{2}", ChromatogramSource, // Not L10N
                ChromatogramInfo.PrecursorMz.Value.ToString(Formats.Mz, CultureInfo.CurrentCulture),
                ChromatogramProductMz.Value.ToString(Formats.Mz, CultureInfo.CurrentCulture));
        }

    }
}
