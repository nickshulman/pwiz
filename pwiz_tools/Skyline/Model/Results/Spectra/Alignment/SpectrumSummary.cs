using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.ProteowizardWrapper;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public class SpectrumSummary
    {
        private const int DIGEST_SIZE = 128;
        public SpectrumSummary(SpectrumMetadata spectrumMetadata, IList<double> digest)
        {
            SpectrumMetadata = spectrumMetadata;
            SummaryValue = ImmutableList.ValueOf(digest);
        }

        public static SpectrumSummary FromSpectrum(MsDataSpectrum spectrum)
        {
            if (spectrum == null)
            {
                return null;
            }

            return FromSpectrum(spectrum.Metadata, spectrum.Mzs.Zip(spectrum.Intensities, (mz, intensity)=>new KeyValuePair<double, double>(mz, intensity)),DIGEST_SIZE);
        }

        public static SpectrumSummary FromSpectrum(SpectrumMetadata metadata,
            IEnumerable<KeyValuePair<double, double>> mzIntensities, int summaryLength)
        {
            IList<double> summaryValue = null;
            if (metadata.ScanWindowLowerLimit.HasValue && metadata.ScanWindowUpperLimit.HasValue)
            {
                var binnedSpectrum = BinnedSpectrum.BinSpectrum(8192, metadata.ScanWindowLowerLimit.Value,
                    metadata.ScanWindowUpperLimit.Value, mzIntensities);
                summaryValue = binnedSpectrum.Intensities;
                while (summaryValue.Count > summaryLength)
                {
                    summaryValue = HaarWaveletTransform(summaryValue);
                }
            }

            return new SpectrumSummary(metadata, summaryValue);
        }

        public static double[] HaarWaveletTransform(IList<double> vector)
        {
            int n = vector.Count / 2;
            double[] result = new double[n];

            for (int i = 0; i < n / 2; i++)
            {
                // Calculate the average and difference
                result[i] = (vector[2 * i] + vector[2 * i + 1]) / Math.Sqrt(2.0);
                result[n / 2 + i] = (vector[2 * i] - vector[2 * i + 1]) / Math.Sqrt(2.0);
            }

            return result;
        }

        public string Id
        {
            get { return SpectrumMetadata.Id; }
        }
        public double RetentionTime
        {
            get { return SpectrumMetadata.RetentionTime; }
        }
        public SpectrumMetadata SpectrumMetadata { get; }
        public ImmutableList<double> SummaryValue { get; }

    }
}
