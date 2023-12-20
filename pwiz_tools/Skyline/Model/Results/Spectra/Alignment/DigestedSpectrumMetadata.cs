using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;
using pwiz.ProteowizardWrapper;
using ZedGraph;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public class DigestedSpectrumMetadata
    {
        private const int DIGEST_SIZE = 128;
        public DigestedSpectrumMetadata(SpectrumMetadata spectrumMetadata, IList<double> digest)
        {
            SpectrumMetadata = spectrumMetadata;
            Digest = ImmutableList.ValueOf(digest);
        }

        public static DigestedSpectrumMetadata FromSpectrum(MsDataSpectrum spectrum)
        {
            if (spectrum == null)
            {
                return null;
            }

            return FromSpectrum(spectrum.Metadata, spectrum.Mzs.Zip(spectrum.Intensities, (mz, intensity)=>new KeyValuePair<double, double>(mz, intensity)),DIGEST_SIZE);
        }

        public static DigestedSpectrumMetadata FromSpectrum(SpectrumMetadata metadata,
            IEnumerable<KeyValuePair<double, double>> mzIntensities, int digestSize)
        {
            IList<double> digest = null;
            if (metadata.ScanWindowLowerLimit.HasValue && metadata.ScanWindowUpperLimit.HasValue)
            {
                var binnedSpectrum = BinnedSpectrum.BinSpectrum(8192, metadata.ScanWindowLowerLimit.Value,
                    metadata.ScanWindowUpperLimit.Value, mzIntensities);
                digest = binnedSpectrum.Intensities;
                while (digest.Count > digestSize)
                {
                    digest = HaarWaveletTransform(digest);
                }

            }

            return new DigestedSpectrumMetadata(metadata, digest);
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
        public ImmutableList<double> Digest { get; }

        private IEnumerable<PointPair> GetSimilarityPoints(IEnumerable<DigestedSpectrumMetadata> other)
        {
            if (Digest == null)
            {
                yield break;
            }
            foreach (var spectrumMetadata in other)
            {
                double? score = GetSimilarityScore(Digest, spectrumMetadata.Digest);
                if (score.HasValue)
                {
                    yield return new PointPair(RetentionTime, spectrumMetadata.RetentionTime, score.Value);
                }
            }
        }

        private Tuple<double, double, ImmutableList<ImmutableList<SpectrumPrecursor>>> GetSpectrumDigestKey()
        {
            if (Digest == null)
            {
                return null;
            }

            var precursorsByMsLevel = ImmutableList.ValueOf(Enumerable.Range(1, SpectrumMetadata.MsLevel - 1)
                .Select(level => SpectrumMetadata.GetPrecursors(level)));
            return Tuple.Create(SpectrumMetadata.ScanWindowLowerLimit.Value,
                SpectrumMetadata.ScanWindowUpperLimit.Value, precursorsByMsLevel);
        }

        public static SimilarityMatrix GetSimilarityMatrix(
            IProgressMonitor progressMonitor,
            IProgressStatus status,
            IList<DigestedSpectrumMetadata> list1,
            IList<DigestedSpectrumMetadata> list2)
        {
            var byDigestKey = list2.ToLookup(metadata => metadata.GetSpectrumDigestKey());
            int completedCount = 0;
            var lists = new IList<PointPair>[list1.Count];
            ParallelEx.For(0, list1.Count, index =>
            {
                var spectrum = list1[index];
                var key = spectrum.GetSpectrumDigestKey();
                if (key != null)
                {
                    var list = new List<PointPair>();
                    foreach (var point in spectrum.GetSimilarityPoints(byDigestKey[key]))
                    {
                        if (true == progressMonitor?.IsCanceled)
                        {
                            break;
                        }

                        list.Add(point);
                    }

                    lists[index] = list;
                }

                if (progressMonitor != null)
                {
                    lock (progressMonitor)
                    {
                        completedCount++;
                        int progressValue = completedCount * 100 / lists.Length;
                        progressMonitor.UpdateProgress(status = status.ChangePercentComplete(progressValue));
                    }
                }
            });
            return new SimilarityMatrix(lists.SelectMany(list => list ?? Array.Empty<PointPair>()));
        }

        public static double? GetSimilarityScore(IList<double> xList, IList<double> yList)
        {
            if (xList.Count != yList.Count)
            {
                return null;
            }
            double sumXX = 0;
            double sumXY = 0;
            double sumYY = 0;
            for (int i = 0; i < xList.Count; i++)
            {
                double x = xList[i];
                double y = yList[i];
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
    }
}
