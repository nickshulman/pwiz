using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.Alignment;

namespace pwiz.Skyline.Model.Results
{
    public class DigestedSpectrumMetadata
    {
        private const int DIGEST_SIZE = 1024;
        public DigestedSpectrumMetadata(SpectrumMetadata spectrumMetadata, SpectrumDigest digest)
        {
            SpectrumMetadata = spectrumMetadata;
            Digest = digest;
        }

        public static DigestedSpectrumMetadata FromSpectrum(MsDataSpectrum spectrum)
        {
            if (spectrum == null)
            {
                return null;
            }

            return new DigestedSpectrumMetadata(spectrum.Metadata,
                SpectrumDigest.DigestSpectrum(DIGEST_SIZE, BinSpectrum(spectrum.Mzs, spectrum.Intensities)));
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
        public SpectrumDigest Digest { get; }
        private const double BIN_SIZE = 0.250125;

        public static IList<double> BinSpectrum(IList<double> mzs, IList<double> intensities)
        {
            var maxMz = mzs.Append(0).Max();
            if (maxMz == 0)
            {
                return Array.Empty<double>();
            }

            int maxBinIndex = PowerOfTwoGreaterThan((int)(maxMz / BIN_SIZE));
            double[] result = new double[maxBinIndex];
            for (int i = 0; i < mzs.Count; i++)
            {
                double mz = mzs[i];
                if (mz < 0)
                {
                    continue;
                }

                int binNumber = (int)(mz / BIN_SIZE);
                if (binNumber < 0 || binNumber >= maxBinIndex)
                {
                    continue;
                }

                result[binNumber] += intensities[i];
            }

            return result;
        }

        private static int PowerOfTwoGreaterThan(int value)
        {
            const int maxValue = 1 << 30;
            for (int power = 1; power < maxValue; power *= 2)
            {
                if (power > value)
                {
                    return power;
                }
            }

            return maxValue;
        }
    }
}
