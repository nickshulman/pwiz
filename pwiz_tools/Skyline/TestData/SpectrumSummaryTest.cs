/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2024 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.Results.Spectra.Alignment;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class SpectrumSummaryTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestGenerateData()
        {
            var relativePathZip = @"TestData\bin\SpectrumSummaryTestFullData.zip";
            var fullPath = TestContext.GetProjectDirectory(relativePathZip);
            if (!File.Exists(fullPath))
            {
                return;
            }
            TestFilesDir = new TestFilesDir(TestContext, relativePathZip);
            var outputFolder = TestFilesDir.GetTestPath("SpectrumSummaryTest");
            Directory.CreateDirectory(outputFolder);
            ParallelEx.ForEach(new[]
            {
                Tuple.Create("S_1.raw", 128), Tuple.Create("S_12.raw", 128), Tuple.Create("2021_01_20_coraleggs_10B_23.raw", 128), Tuple.Create("2021_01_20_coraleggs_10NB_13.raw", 128)
            }, tuple =>
            {
                var rawFileName = tuple.Item1;
                var summaryLength = tuple.Item2;
                var summaryList = GetSpectrumSummaryList(TestFilesDir.GetTestPath(rawFileName), summaryLength);
                summaryList = summaryList.RemoveRareSpectra();
                var outputPath = Path.Combine(outputFolder,
                    Path.GetFileNameWithoutExtension(rawFileName) + ".spectrumsummaries.tsv");
                File.WriteAllText(outputPath, ToTsv(summaryList));
            });
        }


        [TestMethod]
        public void TestGetBestPath()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var spectrumSummaryList = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("S_1.spectrumSummaries.tsv"));
            var evenSpectra = new SpectrumSummaryList(Enumerable.Range(0, spectrumSummaryList.Count / 2)
                .Select(i =>
                {
                    var spectrum = spectrumSummaryList[i * 2];
                    return ChangeRetentionTime(spectrum, spectrum.RetentionTime * 3);
                }));
            var similarityMatrix = spectrumSummaryList.GetSimilarityMatrix(null, null, evenSpectra);
            var bestPath = similarityMatrix.FindBestPath();
            AssertEx.IsNotNull(bestPath);
            foreach (var point in bestPath)
            {
                AssertEx.AreEqual(point.Value, point.Key * 3, .01);
            }

            var kdeAlignment = new KdeAligner();
            kdeAlignment.Train(bestPath.Select(kvp=>kvp.Key).ToArray(), bestPath.Select(kvp=>kvp.Value).ToArray(), CancellationToken.None);
            AssertEx.AreEqual(30, kdeAlignment.GetValue(10), .1);
        }

        [TestMethod]
        public void TestSimilarityGrid()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var spectrumSummaryList = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("S_1.spectrumSummaries.tsv"));
            var evenSpectra = new SpectrumSummaryList(Enumerable.Range(0, spectrumSummaryList.Count / 2)
                .Select(i =>
                {
                    var spectrum = spectrumSummaryList[i * 2];
                    return ChangeRetentionTime(spectrum, spectrum.RetentionTime * 3);
                }));
            var similarityGrid = spectrumSummaryList.GetSimilarityGrid(evenSpectra);
            var bestPath = similarityGrid.FindBestPoints().Select(kvp=>Tuple.Create(kvp.Item1.RetentionTime, kvp.Item2.RetentionTime)).ToList();
            Assert.AreNotEqual(0, bestPath.Count);
            double maxDifference = bestPath.Max(pt => Math.Abs(pt.Item2 - pt.Item1 * 3));
            Assert.AreNotEqual(0, maxDifference);
            foreach (var point in bestPath)
            {
                Assert.AreEqual(point.Item2, point.Item1 * 3, 5);
            }

            var kdeAligner = new KdeAligner();
            kdeAligner.Train(bestPath.Select(pt=>pt.Item1).ToArray(), bestPath.Select(pt=>pt.Item2).ToArray(), CancellationToken.None);
            kdeAligner.GetSmoothedValues(out var xArr, out var yArr);
            double maxDifference2 = xArr.Zip(yArr, (x, y) => Math.Abs(y - x * 3)).Max();
            Assert.AreNotEqual(0, maxDifference2);
            var bitmap = DrawSimilarityGrid(similarityGrid);
            bitmap.Save(TestFilesDir.GetTestPath("SimilarityGrid.png"));
        }

        [TestMethod]
        public void TestBigAlignment()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var spectrumSummaryList1 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10B_23.spectrumsummaries.tsv"));
            var spectrumSummaryList2 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.spectrumsummaries.tsv"));
            var similarityMatrix = spectrumSummaryList1.GetSimilarityMatrix(null, null, spectrumSummaryList2);
            var bestPath = similarityMatrix.FindBestPath();
            AssertEx.IsNotNull(bestPath);
        }

        [TestMethod]
        public void TestBigAlignment2()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var spectrumSummaryList1 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10B_23.spectrumsummaries.tsv"));
            var spectrumSummaryList2 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.spectrumsummaries.tsv"));
            var similarityGrid = spectrumSummaryList1.GetSimilarityGrid(spectrumSummaryList2);
            var bestQuadrants = similarityGrid.ToQuadrant().FindBestQuadrants().ToList();
            DrawPath(similarityGrid, bestQuadrants).Save(TestFilesDir.GetTestPath("coral.png"));
            var kdeAligner = new KdeAligner();
            kdeAligner.Train(bestQuadrants.Select(q => q.Grid.XEntries[q.XStart].RetentionTime).ToArray(),
                bestQuadrants.Select(q => q.Grid.YEntries[q.YStart].RetentionTime).ToArray(), CancellationToken.None);
            DrawAligner(similarityGrid, kdeAligner).Save(TestFilesDir.GetTestPath("coral.kde.png"));
        }

        [TestMethod]
        public void TestHalfRun()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var spectrumSummaryList1 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10B_23.spectrumsummaries.tsv"));
            var spectrumSummaryList2 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.spectrumsummaries.tsv"));
            var firstHalf = new SpectrumSummaryList(spectrumSummaryList2.Take(spectrumSummaryList2.Count / 2));
            var firstHalfGrid = spectrumSummaryList1.GetSimilarityGrid(firstHalf);
            DrawGrid("coralFirstHalf", firstHalfGrid);
            var secondHalf = new SpectrumSummaryList(spectrumSummaryList2.Skip(spectrumSummaryList2.Count / 2));
            var secondHalfGrid = spectrumSummaryList1.GetSimilarityGrid(secondHalf);
            DrawGrid("coralSecondHalf", secondHalfGrid);
        }

        [TestMethod]
        public void TestHalfRunBigSummary()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\bin\SpectrumSummaryTestFullData.zip");
            var spectrumSummaryList1 =
                GetSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10B_23.raw"), 8192);
            var spectrumSummaryList2 = GetSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.raw"), 8192);
            var firstHalf = new SpectrumSummaryList(spectrumSummaryList2.Take(spectrumSummaryList2.Count / 2));
            var firstHalfGrid = spectrumSummaryList1.GetSimilarityGrid(firstHalf);
            DrawGrid("coralFirstHalf", firstHalfGrid);
            var secondHalf = new SpectrumSummaryList(spectrumSummaryList2.Skip(spectrumSummaryList2.Count / 2));
            var secondHalfGrid = spectrumSummaryList1.GetSimilarityGrid(secondHalf);
            DrawGrid("coralSecondHalf", secondHalfGrid);
        }

        [TestMethod]
        public void TestCompareDigests()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\bin\SpectrumSummaryTestFullData.zip");
            using var msDataFile = new MsDataFileImpl(TestFilesDir.GetTestPath("S_12.raw"));
            var spectra = new List<MsDataSpectrum>();
            for (int iSpectrum = 0; iSpectrum < msDataFile.SpectrumCount; iSpectrum++)
            {
                var spectrum = msDataFile.GetSpectrum(iSpectrum);
                if (spectrum.Metadata.MsLevel == 1)
                {
                    spectra.Add(spectrum);
                }
            }
            Console.Out.WriteLine("BinCount\tDigestLength\tMissCount");
            for (int binCount = 8192; binCount > 8; binCount /= 2)
            {
                var binnedSpectra = spectra.Select(spectrum => BinnedSpectrum.BinSpectrum(binCount,
                    spectrum.Metadata.ScanWindowLowerLimit.Value, spectrum.Metadata.ScanWindowUpperLimit.Value,
                    spectrum.Mzs.Zip(spectrum.Intensities,
                        (mz, intensity) => new KeyValuePair<double, double>(mz, intensity)))).ToList();
                var digests = binnedSpectra.Select(spectrum => spectrum.Intensities).ToList();
                while (digests[0].Count > 4)
                {
                    var spectrumSummaryList = spectra.Zip(digests,
                        (spectrum, digest) => new SpectrumSummary(spectrum.Metadata, digest.Select(v=>(float)v))).ToList();
                    int missCount = CountMisses(spectrumSummaryList);
                    Console.Out.WriteLine("{0}\t{1}\t{2}", binCount, digests[0].Count,
                        missCount);
                    digests = digests.Select(digest => ImmutableList.ValueOf(SpectrumSummary.HaarWaveletTransform(digest))).ToList();
                }
            }
        }
        [TestMethod]
        public void TestStretched()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var list1 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10B_23.spectrumsummaries.tsv"));

            int count1 = list1.Count;
            var sublist1 = Enumerable.Range(0, count1 / 8).Select(i => i * 2)
                .Concat(Enumerable.Range(count1 / 4, count1 / 2))
                .Concat(Enumerable.Range(0, count1 / 8).Select(i => 3 * count1 / 4 + i * 2))
                .Select(i => list1[i]).ToList();

            var list2 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.spectrumsummaries.tsv"));
            int count2 = list2.Count;
            var sublist2 = Enumerable.Range(0, count2 / 4)
                .Concat(Enumerable.Range(0, count2 / 4).Select(i => i * 2 + count2 / 4))
                .Concat(Enumerable.Range(count2 * 3 / 4, count2 / 4))
                .Select(i => list2[i]).ToList();
            var grid = new SpectrumSummaryList(sublist1).GetSimilarityGrid(new SpectrumSummaryList(sublist2));
            DrawGrid("streched", grid);
        }

        [TestMethod]
        public void TestS1vS12()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var list1 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("S_1.spectrumsummaries.tsv"));
            var list2 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("S_12.spectrumsummaries.tsv"));
            var grid = list1.GetSimilarityGrid(list2);
            DrawGrid("s1_vs_s12", grid);
        }


        private void EvaluateDigestParams(string path)
        {
            using var msDataFile = new MsDataFileImpl(path);
            var spectra = new List<MsDataSpectrum>();
            for (int iSpectrum = 0; iSpectrum < msDataFile.SpectrumCount; iSpectrum++)
            {
                var spectrum = msDataFile.GetSpectrum(iSpectrum);
                if (spectrum.Metadata.MsLevel == 1)
                {
                    spectra.Add(spectrum);
                }
            }
            for (int binCount = 8192; binCount > 8; binCount /= 2)
            {
                var binnedSpectra = spectra.Select(spectrum => BinnedSpectrum.BinSpectrum(binCount,
                    spectrum.Metadata.ScanWindowLowerLimit.Value, spectrum.Metadata.ScanWindowUpperLimit.Value,
                    spectrum.Mzs.Zip(spectrum.Intensities,
                        (mz, intensity) => new KeyValuePair<double, double>(mz, intensity)))).ToList();
                var digests = binnedSpectra.Select(spectrum => spectrum.Intensities).ToList();
                while (digests[0].Count > 4)
                {
                    var spectrumSummaryList = spectra.Zip(digests,
                        (spectrum, digest) => new SpectrumSummary(spectrum.Metadata, digest.Select(v => (float)v))).ToList();
                    int missCount = CountMisses(spectrumSummaryList);
                    Console.Out.WriteLine("BinCount: {0} DigestLength: {1} MissCount: {2}", binCount, digests[0].Count,
                        missCount);
                    digests = digests.Select(digest => ImmutableList.ValueOf(SpectrumSummary.HaarWaveletTransform(digest))).ToList();
                }
            }

        }

        /// <summary>
        /// Returns the number of cases where the spectrum 2 away has a better similarity score
        /// than the neighbor
        /// </summary>
        private int CountMisses(IList<SpectrumSummary> summaries)
        {
            int missCount = 0;
            for (int i = 2; i < summaries.Count; i++)
            {
                if (summaries[i - 1].SimilarityScore(summaries[i]) < summaries[i - 2].SimilarityScore(summaries[i]))
                {
                    missCount++;
                }
            }

            return missCount;
        }

        void DrawGrid(string name, SimilarityGrid grid)
        {
            var path = grid.ToQuadrant().FindBestQuadrants().OrderByDescending(q => q.CalculateAverageScore()).ToList();
            var halfPath = path.Take(path.Count / 2).ToList();
            var shortPath = path.Take(Math.Max(grid.XEntries.Count, grid.YEntries.Count)).ToList();
            var otherShortPath = ShortenPath(path).ToList();
            var otherOtherShortPath = OtherShortenPath(path).ToList();
            DrawPath(grid, path).Save(TestFilesDir.GetTestPath(name + ".png"));
            DrawPath(grid, halfPath).Save(TestFilesDir.GetTestPath(name + "_halfPoints.png"));
            DrawPath(grid, shortPath).Save(TestFilesDir.GetTestPath(name + "_short.png"));
            DrawPath(grid, otherShortPath).Save(TestFilesDir.GetTestPath(name + "_othershort.png"));
            DrawPath(grid, otherOtherShortPath).Save(TestFilesDir.GetTestPath(name + "_otherothershort.png"));
            // DrawAligner(grid, GetKdeAligner(path)).Save(TestFilesDir.GetTestPath(name + "_kde.png"));
            // DrawAligner(grid, GetKdeAligner(halfPath)).Save(TestFilesDir.GetTestPath(name + "_halfPoints_kde.png"));
            DrawKdeAligner(grid, shortPath, name + "_short");
            DrawKdeAligner(grid, otherShortPath, name + "_othershort");
            DrawKdeAligner(grid, otherOtherShortPath, name +"_otherothershort");
        }

        private void DrawKdeAligner(SimilarityGrid grid, IEnumerable<SimilarityGrid.Quadrant> path, string name)
        {
            var kdeAligner = GetKdeAligner(path, out var histogram);
            DrawAligner(grid, kdeAligner).Save(TestFilesDir.GetTestPath(name + "_kde.png"));
            DrawHeatMap(histogram).Save(TestFilesDir.GetTestPath(name + "_histogram.png"));
        }

        private IEnumerable<SimilarityGrid.Quadrant> ShortenPath(IEnumerable<SimilarityGrid.Quadrant> quadrants)
        {
            var xValues = new HashSet<int>();
            var yValues = new HashSet<int>();
            foreach (var q in quadrants)
            {
                if (xValues.Contains(q.XStart) || yValues.Contains(q.YStart))
                {
                    continue;
                }

                xValues.Add(q.XStart);
                yValues.Add(q.YStart);
                yield return q;
            }
        }
        private IEnumerable<SimilarityGrid.Quadrant> OtherShortenPath(IEnumerable<SimilarityGrid.Quadrant> quadrants)
        {
            var xValues = new HashSet<int>();
            var yValues = new HashSet<int>();
            foreach (var q in quadrants)
            {
                if (!xValues.Contains(q.XStart) || !yValues.Contains(q.YStart))
                {
                    xValues.Add(q.XStart);
                    yValues.Add(q.YStart);
                    yield return q;
                }
            }
        }

        public static string ToTsv(IEnumerable<SpectrumSummary> spectrumSummaryList)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RetentionTime\tScanWindowLower\tScanWindowUpper\tDigestValue");
            foreach (var spectrum in spectrumSummaryList)
            {
                sb.AppendLine(string.Join("\t", spectrum.RetentionTime.ToString("R", CultureInfo.InvariantCulture),
                    FloatToString((float)spectrum.SpectrumMetadata.ScanWindowLowerLimit.Value),
                    FloatToString((float)spectrum.SpectrumMetadata.ScanWindowUpperLimit.Value),
                    new FormattableList<float>(spectrum.SummaryValueFloats.ToList()).ToString("R",
                        CultureInfo.InvariantCulture)));
            }

            return sb.ToString();
        }

        private static string FloatToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private SpectrumSummaryList GetSpectrumSummaryList(string dataFilePath, int summaryLength)
        {
            using var msDataFile = new MsDataFileImpl(TestFilesDir.GetTestPath(dataFilePath));
            var spectrumSummaries = new List<SpectrumSummary>();
            for (int spectrumIndex = 0; spectrumIndex < msDataFile.SpectrumCount; spectrumIndex++)
            {
                var spectrum = msDataFile.GetSpectrum(spectrumIndex);
                var spectrumSummary = SpectrumSummary.FromSpectrum(spectrum, summaryLength);
                if (spectrumSummary.SpectrumMetadata.ScanWindowLowerLimit.HasValue &&
                    spectrumSummary.SpectrumMetadata.ScanWindowUpperLimit.HasValue &&
                    spectrumSummary.SummaryValueLength > 0)
                {
                    spectrumSummaries.Add(spectrumSummary);
                }
            }

            return new SpectrumSummaryList(spectrumSummaries);
        }

        private SpectrumSummaryList ReadSpectrumSummaryList(string tsvPath)
        {
            var streamReader = new StreamReader(tsvPath);
            var tsvReader = new DsvFileReader(streamReader, '\t');
            var spectrumSummaries = new List<SpectrumSummary>();
            while (null != tsvReader.ReadLine())
            {
                var spectrumMetadata = new SpectrumMetadata(spectrumSummaries.Count.ToString(),
                        double.Parse(tsvReader.GetFieldByIndex(0), CultureInfo.InvariantCulture))
                    .ChangeScanWindow(double.Parse(tsvReader.GetFieldByIndex(1), CultureInfo.InvariantCulture),
                        double.Parse(tsvReader.GetFieldByIndex(2), CultureInfo.InvariantCulture));
                var summaryValue = tsvReader.GetFieldByIndex(3).Split(',')
                    .Select(v => float.Parse(v, CultureInfo.InvariantCulture));
                spectrumSummaries.Add(new SpectrumSummary(spectrumMetadata, summaryValue));
            }
            return new SpectrumSummaryList(spectrumSummaries);
        }

        private SpectrumSummary ChangeRetentionTime(SpectrumSummary spectrumSummary, double retentionTime)
        {
            var spectrumMetadata = new SpectrumMetadata(spectrumSummary.SpectrumMetadata.Id, retentionTime)
                .ChangeScanWindow(spectrumSummary.SpectrumMetadata.ScanWindowLowerLimit.Value,
                    spectrumSummary.SpectrumMetadata.ScanWindowUpperLimit.Value);
            return new SpectrumSummary(spectrumMetadata, spectrumSummary.SummaryValue);
        }

        private Bitmap DrawSimilarityGrid(SimilarityGrid similarityGrid)
        {
            return DrawPath(similarityGrid, similarityGrid.ToQuadrant().FindBestQuadrants().ToList());
        }

        private Bitmap DrawPath(SimilarityGrid grid, IEnumerable<SimilarityGrid.Quadrant> quadrants)
        {
            int width = Math.Min(4000, grid.XEntries.Count);
            int height = Math.Min(4000, grid.YEntries.Count);
            var bitmap = new Bitmap(width, height);
            var orderedQuadrants = quadrants.OrderBy(q => q.MaxScore).ToList();
            for (int iQuadrant = 0; iQuadrant < orderedQuadrants.Count; iQuadrant++)
            {
                var q = orderedQuadrants[iQuadrant];
                var value = 255 * iQuadrant / orderedQuadrants.Count;
                var color = Color.FromArgb(value, value, value);
                bitmap.SetPixel(q.XStart * width / grid.XEntries.Count,
                    height - 1 - q.YStart * height / grid.YEntries.Count, color);
            }

            return bitmap;
        }

        private Bitmap DrawAligner(SimilarityGrid grid, Aligner aligner)
        {
            var bitmap = new Bitmap(1000, 1000);
            for (int i = 0; i < 1000; i++)
            {
                bitmap.SetPixel(i, 0, Color.Blue);
                bitmap.SetPixel(0, i, Color.Blue);
                bitmap.SetPixel(i, 999, Color.Blue);
                bitmap.SetPixel(999, i, Color.Blue);
            }
            aligner.GetSmoothedValues(out var xArr, out var yArr);
            if (xArr.Length == 0)
            {
                return bitmap;
            }
            var xMin = grid.XEntries.First().RetentionTime;
            var dx = grid.XEntries.Last().RetentionTime - xMin;
            var yMax = grid.YEntries.Last().RetentionTime;
            var dy = yMax - grid.YEntries.First().RetentionTime;
            for (int i = 0; i < xArr.Length; i++)
            {
                bitmap.SetPixel((int)((bitmap.Width - 1) * (xArr[i] - xMin) / dx),
                    (int)((bitmap.Height - 1) * (yMax - yArr[i]) / dy), Color.White);
            }

            return bitmap;
        }

        private IEnumerable<Tuple<SpectrumSummary, SpectrumSummary>> ToSummaryPairs(IEnumerable<SimilarityGrid.Quadrant> quadrants)
        {
            return quadrants.Select(q =>
                Tuple.Create(q.Grid.XEntries[q.XStart], q.Grid.YEntries[q.YStart]));
        }

        private KdeAligner GetKdeAligner(IEnumerable<SimilarityGrid.Quadrant> quadrants, out float[,] histogram)
        {
            var path = ToSummaryPairs(quadrants).ToList();
            var kdeAligner = new KdeAligner();
            histogram = kdeAligner.TrainWithWeights(path.Select(s => s.Item1.RetentionTime).ToArray(),
                path.Select(s => s.Item2.RetentionTime).ToArray(), Enumerable.Repeat(1.0, path.Count).ToArray(),
                CancellationToken.None);
            return kdeAligner;
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
                    double value = histogram[x, y];
                    int colorValue = (int)((value - min) / (max - min) * 255);
                    bitmap.SetPixel(x, histogram.GetLength(1) - y - 1, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return bitmap;
        }

        private LoessAligner GetLoessAligner(IEnumerable<SimilarityGrid.Quadrant> quadrants)
        {
            var path = ToSummaryPairs(quadrants).ToList();
            var loessAligner = new LoessAligner();
            loessAligner.Train(path.Select(s => s.Item1.RetentionTime).ToArray(), path.Select(s => s.Item2.RetentionTime).ToArray(), CancellationToken.None);
            return loessAligner;
        }

    }
}
