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
            var bestPath = SimilarityGrid.FilterBestPoints(similarityGrid.GetBestPointCandidates(null, null)).Select(pt=>Tuple.Create(pt.XRetentionTime, pt.YRetentionTime)).ToList();
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
            var similarityGrid = spectrumSummaryList1.GetSimilarityGrid(spectrumSummaryList2);
            DrawGrid("BigAlignment", similarityGrid);
            // var bestQuadrants = similarityGrid.ToQuadrant().FindBestQuadrants().ToList();
            // DrawPath(similarityGrid, bestQuadrants).Save(TestFilesDir.GetTestPath("coral.png"));
            // var kdeAligner = new KdeAligner();
            // kdeAligner.Train(bestQuadrants.Select(q => q.Grid.XEntries[q.XStart].RetentionTime).ToArray(),
            //     bestQuadrants.Select(q => q.Grid.YEntries[q.YStart].RetentionTime).ToArray(), CancellationToken.None);
            // DrawAligner(similarityGrid, kdeAligner).Save(TestFilesDir.GetTestPath("coral.kde.png"));
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
            var sublist1 = ScaleSpectrumList(list1, x =>
            {
                if (x < .25)
                {
                    return 2.0;
                }

                if (x < .75)
                {
                    return .5;
                }

                return 2;
            });
            var list2 = ReadSpectrumSummaryList(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.spectrumsummaries.tsv"));
            var sublist2 = ScaleSpectrumList(list2, x =>
            {
                if (x < .25)
                {
                    return .5;
                }

                if (x < .75)
                {
                    return 2;
                }

                return .5;
            });
            var grid = new SpectrumSummaryList(sublist1).GetSimilarityGrid(new SpectrumSummaryList(sublist2));
            DrawGrid("streched", grid);
        }

        private SpectrumSummaryList ScaleSpectrumList(SpectrumSummaryList list, Func<double, double> scaleFactorFunc)
        {
            double sourceIndex = 0;
            double targetIndex = 0;
            var result = new List<SpectrumSummary>();
            while (sourceIndex < list.Count)
            {
                var scaleFactor = scaleFactorFunc(sourceIndex / list.Count);
                var oldTargetIndex = targetIndex;
                targetIndex += scaleFactor;
                if (Math.Floor(oldTargetIndex) < Math.Floor(targetIndex))
                {
                    var spectrum = list[(int)Math.Floor(sourceIndex)];
                    if (sourceIndex > 0 && targetIndex > 0)
                    {
                        var spectrumMetadata = spectrum.SpectrumMetadata;
                        spectrumMetadata = spectrumMetadata
                            .ChangeRetentionTime(spectrumMetadata.RetentionTime * targetIndex / sourceIndex);
                        spectrum = new SpectrumSummary(spectrumMetadata, spectrum.SummaryValue);
                    }
                    result.Add(spectrum);
                }

                sourceIndex++;
            }

            return new SpectrumSummaryList(result);
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

        [TestMethod]
        public void TestMikeBDataset()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var files = new[] { "S_1", "S_2", "S_12", "S_13" };
            var spectrumSummaries = files.Select(file =>
                GetSpectrumSummaryList(Path.Combine("F:\\skydata\\20110215_MikeB", file + ".raw"), 128)).ToList();
            for (int iFile1 = 0; iFile1 < files.Length - 1; iFile1++)
            {
                for (int iFile2 = iFile1 + 1; iFile2 < files.Length; iFile2++)
                {
                    var grid = spectrumSummaries[iFile1].GetSimilarityGrid(spectrumSummaries[iFile2]);
                    DrawGrid(files[iFile1] + "_vs_" + files[iFile2], grid);
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
            var path = grid.GetBestPointCandidates(null, null).OrderByDescending(q => q.Score).ToList();
            var shortPath = ShortenPath(path).ToList();
            DrawPath(grid, path).Save(TestFilesDir.GetTestPath(name + ".png"));
            DrawPath(grid, shortPath).Save(TestFilesDir.GetTestPath(name + "_short.png"));
            // DrawAligner(grid, GetKdeAligner(path)).Save(TestFilesDir.GetTestPath(name + "_kde.png"));
            // DrawAligner(grid, GetKdeAligner(halfPath)).Save(TestFilesDir.GetTestPath(name + "_halfPoints_kde.png"));
            DrawKdeAligner(grid, shortPath, name + "_short");
            DrawLoess(grid, shortPath, name);
        }

        private void DrawKdeAligner(SimilarityGrid grid, IEnumerable<SimilarityGrid.Point> path, string name)
        {
            var kdeAligner = GetKdeAligner(path, out var histogram);
            DrawAligner(grid, kdeAligner).Save(TestFilesDir.GetTestPath(name + "_kde.png"));
            DrawHeatMap(histogram).Save(TestFilesDir.GetTestPath(name + "_histogram.png"));
        }

        private void DrawLoess(SimilarityGrid grid, IEnumerable<SimilarityGrid.Point> path, string name)
        {
            var loess = GetLoessAligner(path);
            DrawAligner(grid, loess).Save(TestFilesDir.GetTestPath(name + "_loess.png"));
        }

        private IEnumerable<SimilarityGrid.Point> ShortenPath(IEnumerable<SimilarityGrid.Point> quadrants)
        {
            return SimilarityGrid.FilterBestPoints(quadrants);
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
            return DrawPath(similarityGrid, similarityGrid.GetBestPointCandidates(null, null).ToList());
        }

        private Bitmap DrawPath(SimilarityGrid grid, IEnumerable<SimilarityGrid.Point> points)
        {
            int width = Math.Min(8000, grid.XEntries.Count);
            int height = Math.Min(8000, grid.YEntries.Count);
            var bitmap = new Bitmap(width, height);
            var orderedQuadrants = points.OrderBy(q => q.Score).ToList();
            for (int iQuadrant = 0; iQuadrant < orderedQuadrants.Count; iQuadrant++)
            {
                var q = orderedQuadrants[iQuadrant];
                var value = (int) (255L * iQuadrant / orderedQuadrants.Count);
                var color = Color.FromArgb(value, value, value);
                bitmap.SetPixel(q.X * width / grid.XEntries.Count,
                    height - 1 - q.Y * height / grid.YEntries.Count, color);
            }

            return bitmap;
        }

        private Bitmap DrawAligner(SimilarityGrid grid, Aligner aligner)
        {
            return DrawAligner(GetRange(grid), aligner);
        }
        
        private Bitmap DrawAligner(Range range, Aligner aligner)
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

            xArr = xArr.ToArray();
            yArr = yArr.ToArray();
            Array.Sort(xArr, yArr);
            for (int i = 0; i < xArr.Length; i++)
            {
                var x = xArr[i];
                var y = aligner.GetValue(x);
                //Assert.AreEqual(y, aligner.GetValue(x), 1);
                var scaledX = (x - range.MinX) / range.DeltaX;
                var scaledY = (range.MaxY - y) / range.DeltaY;
                if (scaledX >= 0 && scaledX <= 1 && scaledY >= 0 && scaledY <= 1)
                {
                    bitmap.SetPixel((int)((bitmap.Width - 1) * scaledX),
                        (int) ((bitmap.Height - 1) * scaledY), Color.White);
                }

            }

            return bitmap;
        }

        private IEnumerable<Tuple<SpectrumSummary, SpectrumSummary>> ToSummaryPairs(IEnumerable<SimilarityGrid.Point> quadrants)
        {
            return quadrants.Select(q =>
                Tuple.Create(q.Grid.XEntries[q.X], q.Grid.YEntries[q.Y])).OrderBy(t=>t.Item1.RetentionTime);
        }

        private KdeAligner GetKdeAligner(IEnumerable<SimilarityGrid.Point> quadrants, out float[,] histogram)
        {
            var path = ToSummaryPairs(quadrants).OrderBy(p=>p.Item1.RetentionTime).ToList();
            var kdeAligner = new KdeAligner(2000){StartingWindowSizeProportion = .5};
            histogram = kdeAligner.GetHistogramAndTrain(path.Select(s => s.Item1.RetentionTime).ToArray(),
                path.Select(s => s.Item2.RetentionTime).ToArray(), CancellationToken.None);
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

        private LoessAligner GetLoessAligner(IEnumerable<SimilarityGrid.Point> quadrants)
        {
            var path = ToSummaryPairs(quadrants).ToList();
            var loessAligner = new LoessAligner();
            loessAligner.Train(path.Select(s => s.Item1.RetentionTime).ToArray(), path.Select(s => s.Item2.RetentionTime).ToArray(), CancellationToken.None);
            return loessAligner;
        }

        [TestMethod]
        public void TestKdeHistogram()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\SpectrumSummaryTest.zip");
            var xValues = Enumerable.Range(0, 100).Select(x => (double)x).ToArray();
            var kdeAligner = new KdeAligner();
            var yValues = xValues.Select(x =>
            {
                if (x < 25)
                {
                    return x / 2;
                }

                if (x < 75)
                {
                    return 12.5 + (x - 25);
                }

                return 62.5 + (x - 75) / 2;
            }).ToArray();
            var histogram = kdeAligner.GetHistogramAndTrain(xValues,yValues, CancellationToken.None);
            var range = new Range(0, 100, 0, 100);
            DrawAligner(range, kdeAligner).Save(TestFilesDir.GetTestPath("kdealigner.png"));
            DrawHeatMap(histogram).Save(TestFilesDir.GetTestPath("kdehistogram.png"));
            var reverseKde = new KdeAligner();
            var reverseHistogram = reverseKde.GetHistogramAndTrain(yValues, xValues, CancellationToken.None);
            DrawAligner(range, reverseKde).Save(TestFilesDir.GetTestPath("reversekdealigner.png"));
            DrawHeatMap(reverseHistogram).Save(TestFilesDir.GetTestPath("reversekdehistogram.png"));

        }

        private Range GetRange(SimilarityGrid grid)
        {
            return new Range(grid.XEntries.Min(s => s.RetentionTime),
                grid.XEntries.Max(s => s.RetentionTime),
                grid.YEntries.Min(s => s.RetentionTime),
                grid.YEntries.Max(s => s.RetentionTime));
        }

        class Range
        {
            public Range(double minX, double maxX, double minY, double maxY)
            {
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
            }
            public double MinX { get; }
            public double MaxX { get; }
            public double MinY { get; }
            public double MaxY { get; }

            public double DeltaX
            {
                get
                {
                    return MaxX - MinX;
                }
            }

            public double DeltaY
            {
                get { return MaxY - MinY; }
            }
        }
    }
}
