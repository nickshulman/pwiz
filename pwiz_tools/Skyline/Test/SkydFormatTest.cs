using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Skydb;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class SkydFormatTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestConvertToNewFormat()
        {
            using (var testFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip"))
            {
                DateTime start = DateTime.UtcNow;
                var outputFile = testFilesDir.GetTestPath("test.skydb");
                //var inputFilePath = testFilesDir.GetTestPath("Human_plasma.skyd");
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\delay.skyd";
                //var inputFilePath = @"D:\skydata\20140318_Hasmik_QE_DIA\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014.skyd";
                var inputFilePath = @"D:\skydata\20150501_Bruderer\3Proteins_delayloadingpeaks\Bruder3Proteins.skyd";
                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                using (var converter = new SkydbConverter(chromatogramCache, outputFile))
                {
                    DumpStatistics(chromatogramCache);
                    converter.Convert();
                }

                // using (var reader = outputFile.OpenReader())
                // {
                //     foreach (var entry in reader.GetTableSizes().OrderBy(kvp => kvp.Key))
                //     {
                //         Console.Out.WriteLine("{0}:{1:N0}", entry.Key, entry.Value);
                //     }
                // }
                Console.Out.WriteLine("Elapsed time {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
                Console.Out.WriteLine("Input File Size: {0:N0}", new FileInfo(inputFilePath).Length);
                Console.Out.WriteLine("Output File Size: {0:N0}", new FileInfo(outputFile).Length);
                Console.Out.WriteLine("File size difference: {0:N0}", new FileInfo(outputFile).Length - new FileInfo(inputFilePath).Length);
            }
        }

        [TestMethod]
        public void TestSkydHeaderSizes()
        {
            foreach (var version in new[]
                {CacheFormatVersion.Two, CacheFormatVersion.Five, CacheFormatVersion.Nine, CacheFormatVersion.Fifteen})
            {
                Console.Out.WriteLine("{0}:{1}", version, CacheHeaderStruct.GetStructSize(version));
            }
        }

        private void DumpStatistics(ChromatogramCache chromatogramCache)
        {
            Console.Out.WriteLine("{0} statistics:", chromatogramCache.CachePath);
            Console.Out.WriteLine("Number of ChromGroups: {0:N0}", chromatogramCache.ChromGroupHeaderInfos.Count);
            Console.Out.WriteLine("Number of ChromTransitions: {0:N0}", chromatogramCache.ChromGroupHeaderInfos.Sum(group=>group.NumTransitions));
            Console.Out.WriteLine("Total chromatogram data: Compressed: {0:N0} Uncompressed: {1:N0}",
                chromatogramCache.ChromGroupHeaderInfos.Sum(group => group.CompressedSize),
                chromatogramCache.ChromGroupHeaderInfos.Sum(group => group.UncompressedSize));
            long totalPeakSize = (long) CacheHeaderStruct.GetStructSize(chromatogramCache.Version) *
                chromatogramCache.ChromGroupHeaderInfos.Sum(group => group.NumPeaks * group.NumTransitions);
            long totalScoreSize = (long)chromatogramCache.ScoreTypesCount * sizeof(float) * chromatogramCache.ChromGroupHeaderInfos.Select(group =>
                    Tuple.Create(group.StartScoreIndex, group.NumPeaks * group.NumTransitions)).Distinct()
                .Sum(tuple => tuple.Item2);
            Console.Out.WriteLine("Total peak data: {0:N0} Scores: {1:N0}",totalPeakSize, totalScoreSize);
        }
    }
}
