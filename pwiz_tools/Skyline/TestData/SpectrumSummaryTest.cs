using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.UI.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Spectra;
using pwiz.MSGraph;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Model.Results.Spectra.Alignment;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.SkylineTestUtil;
using ZedGraph;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class SpectrumSummaryTest : AbstractUnitTest
    {
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
                    var spectrumSummaries = new List<SpectrumSummary>();
                    for (int spectrumIndex = 0; spectrumIndex < msDataFile.SpectrumCount; spectrumIndex++)
                    {
                        var spectrum = msDataFile.GetSpectrum(spectrumIndex);
                        var summaryValue =
                            GetSummaryValue(spectrum.Metadata, spectrum.Mzs, spectrum.Intensities, log, 1024);
                        spectrumSummaries.Add(new SpectrumSummary(spectrum.Metadata, summaryValue));
                    }

                    var resultFileMetadata = new ResultFileMetaData(spectrumSummaries);
                    var suffix = "_" + MAX_DIGEST_LENGTH + ".resultfilemetadata";
                    if (log)
                    {
                        suffix = "_log" + suffix;
                    }
                    File.WriteAllBytes(TestFilesDir.GetTestPath(baseFileName + suffix),
                        resultFileMetadata.ToByteArray());
                }
            }
        }

        [TestMethod]
        public void GenerateMs1Files()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            foreach (var baseFileName in new[] { "S_1", "S_13" })
            {
                foreach (bool log in new[] { false, true })
                {
                    var resultFileMetadata = ResultFileMetaData.FromByteArray(
                        File.ReadAllBytes(TestFilesDir.GetTestPath(baseFileName + Qualifier(log, MAX_DIGEST_LENGTH) + ".resultfilemetadata")));
                    File.WriteAllText(TestFilesDir.GetTestPath(baseFileName + Qualifier(log, MAX_DIGEST_LENGTH) + ".ms1"), ToMs1File(resultFileMetadata));
                }
            }
        }

        [TestMethod]
        public void GenerateSimilarityMatrices()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            foreach (bool log in new[] { false })
            {
                var fullMetadata1 = ResultFileMetaData.FromByteArray(
                    File.ReadAllBytes(TestFilesDir.GetTestPath("S_1" + Qualifier(log, MAX_DIGEST_LENGTH) + ".resultfilemetadata")));
                var fullMetadata2 = ResultFileMetaData.FromByteArray(
                    File.ReadAllBytes(TestFilesDir.GetTestPath("S_13" + Qualifier(log, MAX_DIGEST_LENGTH) + ".resultfilemetadata")));
                for (var digestLength = MAX_DIGEST_LENGTH; digestLength >= MAX_DIGEST_LENGTH; digestLength /= 2)
                {
                    var spectrumSummaries1 = SetDigestLength(fullMetadata1.SpectrumSummaries, digestLength);
                    var spectrumSummaries2 = SetDigestLength(fullMetadata2.SpectrumSummaries, digestLength);
                    var similarityMatrix =
                        spectrumSummaries1.GetSimilarityMatrix(null, null, spectrumSummaries2);
                    File.WriteAllText(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(log, digestLength) + "_similaritymatrix.tsv"),
                        similarityMatrix.ToTsv());
                    // var bitmap = ToHeatMap(similarityMatrix);
                    // bitmap.Save(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(log, digestLength) + "_heatmap.png"), ImageFormat.Png);

                    var alignment = Align(similarityMatrix);
                    GetAlignmentBitmap(alignment).Save(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(log, digestLength) + "_alignment.png"));
                    HistogramToBitmap(alignment.Histogram)
                        .Save(TestFilesDir.GetTestPath("S_1_vs_S_13" + Qualifier(log, digestLength) +
                                                       "_histogram.png"));
                }
            }
        }

        [TestMethod]
        public void TestKdeAligner()
        {
            var kdeAligner = new WeightedKdeAligner(100);
            var points = Enumerable.Range(0, 100).Select(i => new PointPair(i, i / 2.0, 1)).ToList();
            var result = kdeAligner.Train(points,
                CancellationToken.None);
            Assert.AreEqual(100, result.Length);
            HistogramToBitmap(kdeAligner.Histogram).Save(TestContext.GetTestPath("Histogram.png"), ImageFormat.Png);
            GetAlignmentBitmap(kdeAligner).Save(TestContext.GetTestPath("Alignment.png"), ImageFormat.Png);
        }

        public Bitmap HistogramToBitmap(double[][] histogram)
        {
            var bitmap = new Bitmap(histogram.Length, histogram.Length);
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (var pt in histogram.SelectMany(col => col))
            {
                min = Math.Min(min, pt);
                max = Math.Max(max, pt);
            }

            for (int x = 0; x < histogram.Length; x++)
            {
                var col = histogram[x];
                Assert.AreEqual(histogram.Length, col.Length);
                for (int y = 0; y < col.Length; y++)
                {
                    double value = col[y];
                    int colorValue = (int)((value - min) / (max - min) * 255);
                    bitmap.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return bitmap;
        }

        private WeightedKdeAligner Align(SimilarityMatrix similarityMatrix)
        {
            var resolution = 1000;
            var weightedKdeAligner = new WeightedKdeAligner(resolution);
            var normalizedPoints = WeightedKdeAligner.NormalizePoints(similarityMatrix.Points, resolution)
                .Take(10000000)
                .ToList();
            weightedKdeAligner.Train(normalizedPoints, CancellationToken.None);
            return weightedKdeAligner;
        }

        private Bitmap GetAlignmentBitmap(WeightedKdeAligner aligner)
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
                var y = aligner.GetYValue(x);
                bitmap.SetPixel(x, y, Color.Black);
            }

            return bitmap;

        }

        private string Qualifier(bool log, int digestLength)
        {
            return "_" + (log ? "log_" : "") + digestLength;
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
