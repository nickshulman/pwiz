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
using SkydbStorage.DataAccess;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class SkydFormatTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestMemoryJoining()
        {
            using (var testFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip"))
            {
                DateTime start = DateTime.UtcNow;
                var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\BruderDIA2015_withdecoys.skyd";
                //var inputFilePath = testFilesDir.GetTestPath("Bruder3Proteins.skyd");
                //var inputFilePath = @"D:\skydata\20110215_MikeB\Bereman_5proteins_spikein.skyd";
                var outputFile = Path.ChangeExtension(inputFilePath, ".skydb");
                //var inputFilePath = testFilesDir.GetTestPath("Human_plasma.skyd");
                //var inputFilePath = @"D:\skydata\20150501_Bruderer\delayloadingpeaks\delay.skyd";
                //var inputFilePath = @"D:\skydata\20140318_Hasmik_QE_DIA\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014\Study9_2_Curve_DIA_QE_5trans_withSpLib_Jan2014.skyd";
                // var inputFilePath = @"D:\skydata\20150501_Bruderer\3Proteins_delayloadingpeaks\Bruder3Proteins.skyd";
                //var outputFile = testFilesDir.GetTestPath("joined.skydb");


                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                {
                    var schema = new SkydbSchema(chromatogramCache.ScoreTypes);
                    using (schema.CreateDatabase(outputFile))
                    {
                    }
                    var lockObject = new object();
                    ParallelEx.For(0, chromatogramCache.CachedFiles.Count(), iPart =>
                    {
                        using (var legacyChromatogramCache = new LegacyChromatogramCache(chromatogramCache))
                        {
                            using (var skydbConnection = schema.CreateDatabase(SkydbSchema.MEMORY_DATABASE_PATH))
                            {
                                var msDataFiles = legacyChromatogramCache.ExtractedDataFiles.ToList();
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
        public void TestSkydHeaderSizes()
        {
            foreach (var version in new[]
                {CacheFormatVersion.Two, CacheFormatVersion.Five, CacheFormatVersion.Nine, CacheFormatVersion.Fifteen})
            {
                Console.Out.WriteLine("{0}:{1}", version, CacheHeaderStruct.GetStructSize(version));
            }
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
            using (skydbFile)
            {
                TimeAction(()=>
                {
                    using (var selectStatement = new SelectStatement<ChromatogramGroup>(skydbFile))
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
                    using (var selectStatement = new SelectChromatogramStatement(skydbFile.Connection))
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
                    using (var selectStatement = new SelectStatement<SpectrumInfo>(skydbFile))
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
