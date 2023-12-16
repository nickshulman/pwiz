using System;
using System.Collections.Generic;
using System.Linq;

namespace pwiz.Common.Spectra
{
    public class SpectrumDigester
    {
        private const double BIN_SIZE = 0.250125;

        public IList<double> DigestSpectrum(int digestSize, IList<double> mzs, IList<double> intensities)
        {
            var binnedSpectrum = BinSpectrum(mzs, intensities);
            while (binnedSpectrum.Count > digestSize)
            {
                binnedSpectrum = DigestVector(binnedSpectrum);
            }

            if (binnedSpectrum.Count == digestSize)
            {
                return binnedSpectrum;
            }

            var result = new double[digestSize];
            binnedSpectrum.CopyTo(result, 0);
            return result;
        }

        public IList<double> DigestVector(IList<double> vector)
        {
            int n = vector.Count / 2;
            // Temporary array to store the transformed data
            double[] result = new double[n];

            for (int i = 0; i < n / 2; i++)
            {
                // Calculate the average and difference
                result[i] = (vector[2 * i] + vector[2 * i + 1]) / Math.Sqrt(2.0);
                result[n / 2 + i] = (vector[2 * i] - vector[2 * i + 1]) / Math.Sqrt(2.0);
            }

            return result;
        }

        public IList<double> BinSpectrum(IList<double> mzs, IList<double> intensities)
        {
            var maxMz = mzs.Append(0).Max();
            if (maxMz == 0)
            {
                return Array.Empty<double>();
            }

            int maxBinIndex = PowerOfTwoGreaterThan((int) (maxMz / BIN_SIZE));
            double[] result = new double[maxBinIndex];
            for (int i = 0; i < mzs.Count; i++)
            {
                double mz = mzs[i];
                if (mz < 0)
                {
                    continue;
                }

                int binNumber = (int) (mz / BIN_SIZE);
                if (binNumber < 0 || binNumber >= maxBinIndex)
                {
                    continue;
                }

                result[binNumber] += intensities[i];
            }

            return result;
        }

        private int PowerOfTwoGreaterThan(int value)
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
