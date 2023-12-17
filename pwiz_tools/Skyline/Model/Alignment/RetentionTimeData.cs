using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Alignment
{
    public class RetentionTimeData : Immutable
    {
        private LibKeyMap<double> _libKeyMap;
        public RetentionTimeData(IEnumerable<MeasuredRetentionTime> measuredRetentionTimes, SpectrumMetadataList spectra, IRetentionScoreSource calculator)
        {
            Spectra = spectra;
            SetMeasuredRetentionTimes(measuredRetentionTimes);
            Calculator = calculator;
        }

        public IRetentionScoreSource Calculator { get; private set; }

        public RetentionTimeData ChangeCalculator(IRetentionScoreSource calculator)
        {
            return ChangeProp(ImClone(this), im => im.Calculator = calculator);
        }

        public IEnumerable<double> GetRetentionTimes(Target target)
        {
            var calculatedTime = Calculator?.GetScore(target);
            if (calculatedTime.HasValue)
            {
                return ImmutableList.Singleton(calculatedTime.Value);
            }

            return _libKeyMap.ItemsMatching(target.GetLibKey(Adduct.EMPTY), false);
        }
        public ImmutableList<MeasuredRetentionTime> MeasuredRetentionTimes { get; private set; }

        public RetentionTimeData ChangeMeasuredRetentionTimes(IEnumerable<MeasuredRetentionTime> rts)
        {
            return ChangeProp(ImClone(this), im => im.SetMeasuredRetentionTimes(rts));
        }

        private void SetMeasuredRetentionTimes(IEnumerable<MeasuredRetentionTime> retentionTimes)
        {
            MeasuredRetentionTimes = ImmutableList.ValueOfOrEmpty(retentionTimes);
            _libKeyMap = new LibKeyMap<double>(ImmutableList.ValueOf(MeasuredRetentionTimes.Select(rt => rt.RetentionTime)), MeasuredRetentionTimes.Select(rt => rt.PeptideSequence.GetLibKey(Adduct.EMPTY).LibraryKey));
        }

        public SpectrumMetadataList Spectra { get; private set; }

        public RetentionTimeData ChangeSpectra(SpectrumMetadataList spectra)
        {
            return ChangeProp(ImClone(this), im => im.Spectra = spectra);
        }

        public IEnumerable<KeyValuePair<double, double>> GetTargetAlignmentPoints(RetentionTimeData other)
        {
            foreach (var target in MeasuredRetentionTimes.Concat(other.MeasuredRetentionTimes)
                         .Select(rt => rt.PeptideSequence).Distinct())
            {
                foreach (var thisTime in GetRetentionTimes(target))
                {
                    foreach (var thatTime in other.GetRetentionTimes(target))
                    {
                        yield return new KeyValuePair<double, double>(thisTime, thatTime);
                    }
                }
            }
        }
    }
}
