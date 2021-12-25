using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Model.Skydb;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;
using SkydbApi.DataApi;
using SkydbApi.Orm;

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
                using (var chromatogramCache = ChromatogramCache.Load(
                    inputFilePath,
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false))
                using (var converter = new SkydbConverter(chromatogramCache, outputFile))
                {
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
    }
}
