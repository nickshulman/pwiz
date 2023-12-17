using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Alignment
{
    public class SpectrumDigest : IReadOnlyList<double>
    {
        private readonly ImmutableList<double> _values;

        public SpectrumDigest(IEnumerable<double> values)
        {
            _values = ImmutableList.ValueOf(values);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<double> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public int Count
        {
            get { return _values.Count; }
        }

        public double this[int index] => _values[index];

        public SpectrumDigest ShortenTo(int length)
        {
            if (length >= Count)
            {
                return this;
            }

            IList<double> vector = _values;
            if (0 != (vector.Count & (vector.Count - 1)))
            {
                var paddedLength = PowerOfTwoGreaterThan(vector.Count);
                vector = vector.Concat(Enumerable.Repeat(0.0, paddedLength - vector.Count)).ToList();
            }
            while (vector.Count > length)
            {
                vector = DigestVector(vector);
            }
            return new SpectrumDigest(vector);
        }

        public static IList<double> DigestVector(IList<double> vector)
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

        public double? SimilarityScore(SpectrumDigest other)
        {
            SpectrumDigest x = this;
            SpectrumDigest y = other;
            int count = Math.Min(x.Count, y.Count);
            x = x.ShortenTo(count);
            y = y.ShortenTo(count);
            double sumXY = 0;
            double sumXX = 0;
            double sumYY = 0;
            for (int i = 0; i < count; i++)
            {
                sumXY += x[i] * y[i];
                sumXX += x[i] * x[i];
                sumYY += y[i] * y[i];
            }

            if (sumXX == 0 || sumYY == 0)
            {
                return null;
            }
            return sumXY / Math.Sqrt(sumXX * sumYY);
        }

        public static SpectrumDigest DigestSpectrum(int digestSize, IList<double> binnedIntensities)
        {
            while (binnedIntensities.Count > digestSize)
            {
                binnedIntensities = DigestVector(binnedIntensities);
            }

            if (binnedIntensities.Count == digestSize)
            {
                return new SpectrumDigest(binnedIntensities);
            }

            var result = new double[digestSize];
            binnedIntensities.CopyTo(result, 0);
            return new SpectrumDigest(result);
        }
    }
}
