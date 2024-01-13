using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using Microsoft.VisualBasic.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Mapping.ByCode;
using pwiz.Common.Spectra;
using pwiz.MSGraph;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.GroupComparison;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Model.Results.Spectra.Alignment;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;
using ZedGraph;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class SpectrumSummaryTest : AbstractUnitTest
    {

        enum Weighting
        {
            none,
            dotProduct,
            normalizedContrastAngle,
        }
        private const int MAX_DIGEST_LENGTH = 4096;
        [TestMethod]
        public void GenerateResultFileMetadatas()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTestFullData.zip");
            foreach (var baseFileName in new[] { "S_1", "S_13" })
            {
                using var msDataFile = new MsDataFileImpl(TestFilesDir.GetTestPath(baseFileName + ".raw"));
                foreach (bool log in new[] { false, true })
                {
                    var suffix = "_" + MAX_DIGEST_LENGTH + ".resultfilemetadata";
                    if (log)
                    {
                        suffix = "_log" + suffix;
                    }
                    File.WriteAllBytes(TestFilesDir.GetTestPath(baseFileName + suffix),
                        GetResultFileMetadata(TestFilesDir.GetTestPath(baseFileName + ".raw"), MAX_DIGEST_LENGTH, log).ToByteArray());
                }
            }
        }

        private ResultFileMetaData GetResultFileMetadata(string path, int summaryLength, bool log)
        {
            using var msDataFile = new MsDataFileImpl(TestFilesDir.GetTestPath(path));
            var spectrumSummaries = new List<SpectrumSummary>();
            for (int spectrumIndex = 0; spectrumIndex < msDataFile.SpectrumCount; spectrumIndex++)
            {
                var spectrum = msDataFile.GetSpectrum(spectrumIndex);
                var summaryValue =
                    GetSummaryValue(spectrum.Metadata, spectrum.Mzs, spectrum.Intensities, log, 4096);
                spectrumSummaries.Add(new SpectrumSummary(spectrum.Metadata, summaryValue));
            }

            return new ResultFileMetaData(spectrumSummaries);
        }

        [TestMethod]
        public void GenerateMs1Files()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            foreach (var baseFileName in new[] { "S_1", "S_13" })
            {
                foreach (Weighting weighting in Enum.GetValues(typeof(Weighting)))
                {
                    var resultFileMetadata = ResultFileMetaData.FromByteArray(
                        File.ReadAllBytes(TestFilesDir.GetTestPath(baseFileName + Qualifier(weighting, MAX_DIGEST_LENGTH) + ".resultfilemetadata")));
                    File.WriteAllText(TestFilesDir.GetTestPath(baseFileName + Qualifier(weighting, MAX_DIGEST_LENGTH) + ".ms1"), ToMs1File(resultFileMetadata));
                }
            }
        }

        [TestMethod]
        public void GenerateSimilarityMatrices()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var fullMetadata1 = GetResultFileMetadata(TestFilesDir.GetTestPath("S_1.raw"), MAX_DIGEST_LENGTH, false);
            var fullMetadata2 = GetResultFileMetadata(TestFilesDir.GetTestPath("S_13.raw"), MAX_DIGEST_LENGTH, false);
            var goldStandardSimilarityMatrix =
                fullMetadata1.SpectrumSummaries.GetSimilarityMatrix(null, null, fullMetadata2.SpectrumSummaries);
            var goldAlignments = new Dictionary<Weighting, KdeAligner>();
            foreach (Weighting weighting in Enum.GetValues(typeof(Weighting)))
            {
                var alignment = Align(goldStandardSimilarityMatrix, weighting);
                goldAlignments.Add(weighting, alignment.Item1);
            }
            
            for (var digestLength = 4096; digestLength >= 4; digestLength /= 2)
            {
                var spectrumSummaries1 = SetDigestLength(fullMetadata1.SpectrumSummaries, digestLength);
                var spectrumSummaries2 = SetDigestLength(fullMetadata2.SpectrumSummaries, digestLength);
                var similarityMatrix =
                    spectrumSummaries1.GetSimilarityMatrix(null, null, spectrumSummaries2);
                File.WriteAllText(TestFilesDir.GetTestPath("S_1_vs_S_13_" + digestLength + "_similaritymatrix.tsv"),
                    similarityMatrix.ToTsv());
                foreach (Weighting weighting in Enum.GetValues(typeof(Weighting)))
                {
                    var alignment = Align(similarityMatrix, weighting);
                    GetAlignmentBitmap(alignment.Item1).Save(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(weighting, digestLength) + "_alignment.png"));
                    DrawHeatMap(alignment.Item2)
                        .Save(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(weighting, digestLength) +
                                                       "_heatmap.png"));
                    foreach (var goldEntry in goldAlignments)
                    {
                        var score = ScoreAlignment(goldEntry.Value, alignment.Item1);
                        Console.Out.WriteLine("DigestLength: {0} Weighting: {1} GoldWeighting: {2} Score: {3}", digestLength, weighting, goldEntry.Key, score);
                    }
                }
                // var bitmap = ToHeatMap(similarityMatrix);
                // bitmap.Save(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(log, digestLength) + "_heatmap.png"), ImageFormat.Png);

                
            }
        }

        private double ScoreAlignment(KdeAligner goldStandard, KdeAligner aligner)
        {
            double totalDifference = 0;
            for (int i = 0; i < goldStandard.Resolution; i++)
            {
                var goldValue = goldStandard.GetValue(goldStandard.GetScaledX(i));
                var value = aligner.GetValue(aligner.GetScaledX(i));
                totalDifference += Math.Abs(goldValue - value);
            }
            return totalDifference;
        }

        [TestMethod]
        public void TestKdeAligner()
        {
            var kdeAligner = new KdeAligner(100);
            var points = Enumerable.Range(0, 100).Select(i => new PointPair(i, i / 2.0, 1)).ToList();
            var result = kdeAligner.TrainPoints(points,
                CancellationToken.None);
            Assert.AreEqual(100, result.GetLength(0));
            Assert.AreEqual(100, result.GetLength(1));
            DrawHeatMap(result).Save(TestContext.GetTestPath("Histogram.png"), ImageFormat.Png);
            //GetAlignmentBitmap(kdeAligner).Save(TestContext.GetTestPath("Alignment.png"), ImageFormat.Png);
        }

        public Bitmap DrawHeatMap(float[,] histogram)
        {
            var bitmap = new Bitmap(histogram.GetLength(0), histogram.GetLength(1));
            if (0 == histogram.Length)
            {
                return bitmap;
            }

            double min = histogram.Cast<float>().Min();
            double max = histogram.Cast<float>().Max();
            if (min == max)
            {
                return bitmap;
            }

            for (int x = 0; x < histogram.GetLength(0); x++)
            {
                for (int y = 0; y < histogram.GetLength(1); y++)
                {
                    double value = histogram[x,y];
                    int colorValue = (int)((value - min) / (max - min) * 255);
                    bitmap.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return bitmap;
        }

        private Tuple<KdeAligner, float[,]> Align(SimilarityMatrix similarityMatrix, Weighting weighting)
        {
            var resolution = 1000;
            var kdeAligner = new KdeAligner(resolution);
            var bestPath = similarityMatrix.FindBestPath(false).Select(point =>
            {
                switch (weighting)
                {
                    case Weighting.none:
                        point = new PointPair(point.X, point.Y, 1.0);
                        break;
                    case Weighting.normalizedContrastAngle:
                        point = new PointPair(point.X, point.Y, Statistics.AngleToNormalizedContrastAngle(point.Z));
                        break;
                    case Weighting.dotProduct:
                        break;
                }
                return point;
            }).ToList();
            double minX = similarityMatrix.Points.Min(pt => pt.X);
            double maxX = similarityMatrix.Points.Max(pt => pt.X);
            double minY = similarityMatrix.Points.Min(pt => pt.Y);
            double maxY = similarityMatrix.Points.Max(pt => pt.Y);
            bestPath.Add(new PointPair(minX, minY, 0));
            bestPath.Add(new PointPair(maxX, maxY, 0));
            
            var histogram = kdeAligner.TrainPoints(bestPath, CancellationToken.None);
            // var normalizedPoints = WeightedKdeAligner.NormalizePoints(similarityMatrix.Points, resolution)
            //     .Take(10000000)
            //     .ToList();
            return Tuple.Create(kdeAligner, histogram);
        }

        private Bitmap GetAlignmentBitmap(KdeAligner aligner)
        {
            var resolution = aligner.Resolution;
            var bitmap = new Bitmap(resolution, resolution);
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    bitmap.SetPixel(x, y, Color.White);
                }
            }
            for (int x = 0; x < resolution; x++)
            {
                var y = aligner.GetValue(aligner.GetScaledX(x));
                bitmap.SetPixel(x, aligner.GetYCoordinate(y), Color.Black);
            }

            return bitmap;

        }

        private string Qualifier(Weighting weighting, int digestLength)
        {
            return "_" + weighting + "_" + digestLength;
        }

        private SpectrumSummaryList SetDigestLength(SpectrumSummaryList summaryList, int digestLength)
        {
            var list = new List<SpectrumSummary>(); 
            foreach (var summary in summaryList)
            {
                if (summary.SpectrumMetadata.MsLevel != 1)
                {
                    continue;
                }

                IList<double> digest = summary.SummaryValue;
                while (digest.Count > digestLength)
                {
                    digest = SpectrumSummary.HaarWaveletTransform(digest);
                }
                list.Add(new SpectrumSummary(summary.SpectrumMetadata, digest));
            }

            return new SpectrumSummaryList(list);
        }

        private Bitmap ToHeatMap(SimilarityMatrix similarityMatrix)
        {
            var points = similarityMatrix.AsTuples.ToList();
            var xValues = points.Select(pt => pt.x).Distinct().OrderBy(x => x).ToList();
            var minZ = points.Min(pt => pt.z);
            var maxZ = points.Max(pt => pt.z);
            
            var rows = points.GroupBy(pt => pt.y).OrderBy(group => group.Key).ToList();
            var bitmap = new Bitmap(xValues.Count, rows.Count);
            var heatMapColors = HeatMapGraphPane._heatMapColors;
            for (int iRow = 0; iRow < rows.Count; iRow++)
            {
                var row = rows[iRow].ToDictionary(pt => pt.x, pt => pt.z);
                for (int iCol = 0; iCol < xValues.Count; iCol++)
                {
                    var x = xValues[iCol];
                    if (row.TryGetValue(x, out var z))
                    {
                        int index = (int) ((z - minZ) * (heatMapColors.Length - 1) / (maxZ - minZ));
                        var color = heatMapColors[index];
                        bitmap.SetPixel(iCol, iRow, color);
                    }
                    else
                    {
                        bitmap.SetPixel(iRow, iCol, Color.Transparent);
                    }
                }
            }

            return bitmap;
        }

        public static IEnumerable<double> GetSummaryValue(SpectrumMetadata spectrumMetadata,
            IList<double> mzs, IList<double> intensities, bool log, int summaryLength)
        {
            if (log)
            {
                spectrumMetadata = spectrumMetadata.ChangeScanWindow(
                    Math.Log(spectrumMetadata.ScanWindowLowerLimit.Value),
                    Math.Log(spectrumMetadata.ScanWindowUpperLimit.Value));
                mzs = mzs.Select(mz => Math.Log(mz)).ToList();
            }

            return SpectrumSummary.FromSpectrum(spectrumMetadata,
                mzs.Zip(intensities, (mz, intensity) => new KeyValuePair<double, double>(mz, intensity)),
                summaryLength).SummaryValue;
        }

        public static string ToMs1File(ResultFileMetaData resultFileMetadata)
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);
            for (int spectrumIndex = 0; spectrumIndex < resultFileMetadata.SpectrumSummaries.Count; spectrumIndex++)
            {
                var spectrumSummary = resultFileMetadata.SpectrumSummaries[spectrumIndex];
                writer.WriteLine("S\t{0}\t{1}", spectrumIndex, spectrumSummary.RetentionTime);
                writer.WriteLine("I\tNativeID\t{0}", spectrumSummary.SpectrumMetadata.Id);
                writer.WriteLine("I\tRTime\t{0}", spectrumSummary.RetentionTime);
                for (int i = 0; i < spectrumSummary.SummaryValue.Count; i++)
                {
                    writer.WriteLine("{0}\t{1}", i + 1, (float)spectrumSummary.SummaryValue[i]);
                }
            }

            return writer.ToString();
        }
    }
}
