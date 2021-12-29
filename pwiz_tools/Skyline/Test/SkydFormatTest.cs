using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Chemistry;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Skydb;
using pwiz.SkylineTestUtil;
using SkydbStorage.Api;
using SkydbStorage.DataApi;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;

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
                var inputFilePath = testFilesDir.GetTestPath("Human_plasma.skyd");
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\delay.skyd";
                //var inputFilePath = @"D:\skydata\20140318_Hasmik_QE_DIA\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014.skyd";
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\BruderDIA2015_withdecoys.skyd";
                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                {
                    var skydbFile = new SkydbConnection(outputFile)
                    {
                        UseUnsafeJournalMode = true
                    };
                    skydbFile.CreateDatabase();
                    using (var source = new LegacyChromatogramCache(chromatogramCache))
                    {
                        foreach (var file in source.MsDataFileChromatogramDatas)
                        {
                            skydbFile.AddChromatogramData(file);
                        }
                    }
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
        public void TestMemoryJoining()
        {
            using (var testFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip"))
            {
                DateTime start = DateTime.UtcNow;
                var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\BruderDIA2015_withdecoys.skyd";
                //var inputFilePath = testFilesDir.GetTestPath("Bruder3Proteins.skyd");
                //var inputFilePath = testFilesDir.GetTestPath("Human_plasma.skyd");
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\delay.skyd";
                //var inputFilePath = @"D:\skydata\20140318_Hasmik_QE_DIA\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014.skyd";
                // var inputFilePath = @"D:\skydata\20150501_Bruderer\3Proteins_delayloadingpeaks\Bruder3Proteins.skyd";
                var outputFile = testFilesDir.GetTestPath("joined.skydb");
                var schema = new SkydbSchema();


                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                {
                    using (var target = schema.CreateDatabase(outputFile))
                    {
                        using (var writer = new SkydbWriter(target.Connection))
                        {
                            writer.EnsureScores(chromatogramCache.ScoreTypes.Select(type=>type.Name));
                        }
                    }
                    var lockObject = new object();
                    ParallelEx.For(0, chromatogramCache.CachedFiles.Count(), iPart =>
                    {
                        using (var legacyChromatogramCache = new LegacyChromatogramCache(chromatogramCache))
                        {
                            using (var skydbConnection = schema.CreateDatabase(SkydbSchema.MEMORY_DATABASE_PATH))
                            {
                                var msDataFiles = legacyChromatogramCache.MsDataFileChromatogramDatas.ToList();
                                skydbConnection.AddChromatogramData(msDataFiles[iPart]);
                                lock (lockObject)
                                {
                                    skydbConnection.CopyDataToPath(outputFile);
                                }
                            }
                        }
                    });
                }

                Console.Out.WriteLine("Elapsed time {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
                Console.Out.WriteLine("Input File Size: {0:N0}", new FileInfo(inputFilePath).Length);
                Console.Out.WriteLine("Output File Size: {0:N0}", new FileInfo(outputFile).Length);
                Console.Out.WriteLine("File size difference: {0:N0}", new FileInfo(outputFile).Length - new FileInfo(inputFilePath).Length);
                var targetFile =
                    Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(outputFile))),
                        Path.GetFileName(outputFile));
                File.Copy(outputFile, targetFile, true);
            }
        }


        [TestMethod]
        public void TestSkydbJoiner()
        {
            using (var testFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip"))
            {
                DateTime start = DateTime.UtcNow;
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\BruderDIA2015_withdecoys.skyd";
                var inputFilePath = testFilesDir.GetTestPath("Bruder3Proteins.skyd");
                //var inputFilePath = testFilesDir.GetTestPath("Human_plasma.skyd");
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\delay.skyd";
                //var inputFilePath = @"D:\skydata\20140318_Hasmik_QE_DIA\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014.skyd";
                // var inputFilePath = @"D:\skydata\20150501_Bruderer\3Proteins_delayloadingpeaks\Bruder3Proteins.skyd";
                List<string> partFiles = new List<string>();
                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                {
                    partFiles.AddRange(Enumerable.Range(0, chromatogramCache.CachedFiles.Count)
                        .Select(iPart => testFilesDir.GetTestPath("part" + iPart + ".skydb")));
                    ParallelEx.For(0, chromatogramCache.CachedFiles.Count(), iPart =>
                    {
                        using (var legacyChromatogramCache = new LegacyChromatogramCache(chromatogramCache))
                        {
                            var msDataFiles = legacyChromatogramCache.MsDataFileChromatogramDatas.ToList();
                            var skydbFile = new SkydbConnection(partFiles[iPart])
                            {
                                UseUnsafeJournalMode = true
                            };
                            skydbFile.CreateDatabase();
                            skydbFile.AddChromatogramData(msDataFiles[iPart]);
                        }
                    });
                }

                var outputFile = testFilesDir.GetTestPath("joined.skydb");
                var outputSkydbFile = new SkydbConnection(outputFile)
                {
                    UseUnsafeJournalMode = true
                };
                outputSkydbFile.CreateDatabase();
                outputSkydbFile.AddSkydbFiles(partFiles.Select(file=>new SkydbConnection(file)));
                Console.Out.WriteLine("Elapsed time {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
                Console.Out.WriteLine("Input File Size: {0:N0}", new FileInfo(inputFilePath).Length);
                Console.Out.WriteLine("Output File Size: {0:N0}", new FileInfo(outputFile).Length);
                Console.Out.WriteLine("File size difference: {0:N0}", new FileInfo(outputFile).Length - new FileInfo(inputFilePath).Length);
                var targetFile =
                    Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(outputFile))),
                        Path.GetFileName(outputFile));
                File.Copy(outputFile, targetFile, true);
            }
        }

        [TestMethod]
        public void TestJoinUsingAttachDatabase()
        {
            using (var testFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip"))
            {
                DateTime start = DateTime.UtcNow;
               // var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\BruderDIA2015_withdecoys.skyd";
                //var inputFilePath = testFilesDir.GetTestPath("Bruder3Proteins.skyd");
                var inputFilePath = testFilesDir.GetTestPath("Human_plasma.skyd");
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\delay.skyd";
                //var inputFilePath = @"D:\skydata\20140318_Hasmik_QE_DIA\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014.skyd";
                // var inputFilePath = @"D:\skydata\20150501_Bruderer\3Proteins_delayloadingpeaks\Bruder3Proteins.skyd";
                List<string> partFiles = new List<string>();
                List<string> scoreNames;
                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                {
                    scoreNames = chromatogramCache.ScoreTypes.Select(type => type.Name).ToList();
                    partFiles.AddRange(Enumerable.Range(0, chromatogramCache.CachedFiles.Count)
                        .Select(iPart => testFilesDir.GetTestPath("part" + iPart + ".skydb")));
                    ParallelEx.For(0, chromatogramCache.CachedFiles.Count(), iPart =>
                    {
                        using (var legacyChromatogramCache = new LegacyChromatogramCache(chromatogramCache))
                        {
                            var msDataFiles = legacyChromatogramCache.MsDataFileChromatogramDatas.ToList();
                            var skydbFile = new SkydbConnection(partFiles[iPart])
                            {
                                UseUnsafeJournalMode = true
                            };
                            skydbFile.CreateDatabase();
                            //skydbFile.SetStartingSequenceNumber((long)iPart * 2 << 31);
                            skydbFile.AddChromatogramData(msDataFiles[iPart]);
                        }
                    });
                }
                Console.Out.WriteLine("Time to generate parts {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
                var startJoin = DateTime.UtcNow;
                var outputFile = testFilesDir.GetTestPath("joined.skydb");
                var outputSkydbFile = new SkydbConnection(outputFile)
                {
                    UseUnsafeJournalMode = true
                };
                outputSkydbFile.CreateDatabase();
                using (var writer = outputSkydbFile.OpenWriter())
                {
                    writer.EnsureScores(scoreNames);
                }
                outputSkydbFile.JoinUsingAttachDatabase(partFiles.Select(file => new SkydbConnection(file)));
                Console.Out.WriteLine("Time to join parts {0}", DateTime.UtcNow.Subtract(startJoin).TotalMilliseconds);
                Console.Out.WriteLine("Elapsed time {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
                Console.Out.WriteLine("Input File Size: {0:N0}", new FileInfo(inputFilePath).Length);
                Console.Out.WriteLine("Output File Size: {0:N0}", new FileInfo(outputFile).Length);
                Console.Out.WriteLine("File size difference: {0:N0}", new FileInfo(outputFile).Length - new FileInfo(inputFilePath).Length);
                var targetFile =
                    Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(outputFile))),
                        Path.GetFileName(outputFile));
                File.Copy(outputFile, targetFile, true);
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

        [TestMethod]
        public void TestReadChromatogramGroupTable()
        {
            var originalFile = @"c:\skydb\joinedbyattachdb.skydb";
            var skydbPath = Path.Combine(TestContext.TestDir, "speedtest.skydb");
            File.Copy(originalFile, skydbPath, true);
            // var skydbPath = @"c:\skydb\joinedbyattachdb.skydb";
            var skydbFile = new SkydbConnection(skydbPath);
            var chromGroupHeaderInfos = new List<ChromGroupHeaderInfo>();
            var chromTransitions = new List<ChromTransition>();
            var spectrumInfos = new List<SpectrumInfo>();
            var startTime = DateTime.UtcNow;
            using (var connection = skydbFile.OpenConnection())
            {
                TimeAction(()=>
                {
                    using (var selectStatement = new SelectStatement<ChromatogramGroup>(connection))
                    {
                        foreach (var chromatogramGroup in selectStatement.SelectAll())
                        {
                            chromGroupHeaderInfos.Add(new ChromGroupHeaderInfo(new SignedMz(chromatogramGroup.PrecursorMz),
                                0, chromatogramGroup.TextId?.Length ?? 0, (int)chromatogramGroup.File, 0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0, 0, (float?)chromatogramGroup.StartTime, (float?)chromatogramGroup.EndTime, 0,
                                eIonMobilityUnits.none));
                        }
                    }

                    return string.Format("Read {0} Chromatogram Groups", chromGroupHeaderInfos.Count);
                });

                TimeAction(() =>
                {
                    using (var selectStatement = new SelectChromatogramStatement(connection))
                    {
                        foreach (var chromatogram in selectStatement.SelectAll())
                        {
                            chromTransitions.Add(new ChromTransition(chromatogram.ProductMz, (float?)chromatogram.ExtractionWidth ?? 0, (float?)chromatogram.IonMobilityValue ?? 0, (float?)chromatogram.IonMobilityExtractionWidth ?? 0, (ChromSource)chromatogram.Source));
                        }
                    }

                    return string.Format("Read {0} Chromatogram Transitions", chromTransitions.Count);
                });

                TimeAction(() =>
                {
                    using (var selectStatement = new SelectStatement<SpectrumInfo>(connection))
                    {
                        spectrumInfos.AddRange(selectStatement.SelectAll());
                    }

                    return string.Format("Read {0} Spectrum Infos", spectrumInfos.Count);
                });
            }

            Console.Out.WriteLine("Read {0} chromGroupHeaderInfos {1} chromTransitions, {2} spectrumInfos in {3} milliseconds", 
                chromGroupHeaderInfos.Count,
                chromTransitions.Count, spectrumInfos.Count,
                DateTime.UtcNow.Subtract(startTime).TotalMilliseconds);
        }

        private void TimeAction(Func<string> action)
        {
            DateTime start = DateTime.UtcNow;
            string description = action();
            var duration = DateTime.UtcNow.Subtract(start);
            Console.Out.WriteLine("Time to {0}:{1}", description, duration.TotalMilliseconds);
        }

        [TestMethod]
        public void TestReadOldSkydFile()
        {
            var start = DateTime.UtcNow;
            var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\BruderDIA2015_withdecoys.skyd";
            using (var chromatogramCache = ChromatogramCache.Load(
                inputFilePath,
                new ProgressStatus(),
                new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
            {
                Console.Out.WriteLine("Time to open {0}:{1} milliseconds", inputFilePath, DateTime.UtcNow.Subtract(start).TotalMilliseconds);
            }

        }
    }
}
