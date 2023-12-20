using System;
using System.Collections.Generic;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public struct SpectrumDigest
    {
        private ImmutableList<double> _list;
        public SpectrumDigest(IEnumerable<double> value)
        {
            _list = ImmutableList.ValueOf(value);
        }

        public ImmutableList<double> Value
        {
            get { return _list ?? ImmutableList<double>.EMPTY; }
        }
        public int Length
        {
            get { return Value.Count; }
        }

        public double? SimilarityScore(SpectrumDigest other)
        {
            if (Length != other.Length)
            {
                return null;
            }

            double sumXX = 0;
            double sumXY = 0;
            double sumYY = 0;
            for (int i = 0; i < Length; i++)
            {
                double x = Value[i];
                double y = other.Value[i];
                sumXX += x * x;
                sumXY += x * y;
                sumYY += y * y;
            }

            if (sumXX <= 0 || sumYY <= 0)
            {
                return null;
            }

            return sumXY / Math.Sqrt(sumXX * sumYY);
        }

        public static SpectrumDigest FromSpectrum(int digestLength, double scanWindowLower, double scanWindowUpper,
            IEnumerable<KeyValuePair<double, double>> mzIntensities)
        {
            int binCount = Math.Max(8192, digestLength);
            var binnedSpectrum = BinnedSpectrum.BinSpectrum(binCount, scanWindowLower, scanWindowUpper, mzIntensities);
            IList<double> vector = binnedSpectrum.Intensities;
            while (vector.Count > digestLength)
            {
                vector = HaarWaveletTransform(vector);
            }
            return new SpectrumDigest(vector);
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
    }
}
