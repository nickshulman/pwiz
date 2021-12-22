using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.ProtoBuf;
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
                var chromatogramCache = ChromatogramCache.Load(
                    testFilesDir.GetTestPath("Bereman_5proteins_spikein_test_rescore.skyd"),
                    new ProgressStatus(),
                    new DefaultFileLoadMonitor(new SilentProgressMonitor()), false);
                using (var outStream = File.OpenWrite(TestFilesDir.GetTestPath("output.skyd")))
                {
                    var dataLocations = new List<DataLocation>();
                    foreach (var grouping in chromatogramCache.ChromGroupHeaderInfos.GroupBy(header => header.FileIndex))
                    {
                        dataLocations.Add(WriteFileData(outStream, chromatogramCache, chromatogramCache.CachedFiles[grouping.Key], grouping));
                    }
                }
            }
        }

        public DataLocation WriteFileData(Stream stream, ChromatogramCache cache, ChromCachedFile chromCachedFile,
            IEnumerable<ChromGroupHeaderInfo> chromGroupHeaderInfos)
        {
            var dataLocation = new DataLocation()
            {
                Offset = stream.Position
            };
            var fileData = new ChromatogramFileData()
            {
                FileModifiedTime = chromCachedFile.FileWriteTime.ToBinary(),
                FilePath = chromCachedFile.FilePath.ToString(),
                ImportTime = chromCachedFile.ImportTime?.ToBinary() ?? 0,
                MaxIntensity = chromCachedFile.MaxIntensity,
                MaxRetentionTime = chromCachedFile.MaxRetentionTime,
                RunStart = chromCachedFile.RunStartTime?.ToBinary() ?? 0,
                SampleId = chromCachedFile.SampleId,
                SerialNumber = chromCachedFile.InstrumentSerialNumber,
                TicArea = chromCachedFile.TicArea ?? 0
            };
            var textIds = new Dictionary<string, int>();
            foreach (var groupHeader in chromGroupHeaderInfos)
            {
                fileData.ChromatogramGroups.Add(WriteChromatogramGroup(stream, cache, groupHeader, dataLocation.Offset, textIds));
            }
            fileData.FileModifiedTime = chromCachedFile.FileWriteTime.ToBinary();
            fileData.TextIds.AddRange(textIds.OrderBy(kvp=>kvp.Value).Select(kvp=>kvp.Key));
            return dataLocation;
        }

        public ChromatogramFileData.Types.ChromatogramGroup WriteChromatogramGroup(Stream stream,
            ChromatogramCache cache, ChromGroupHeaderInfo chromGroupHeaderInfo, long fileDataStart, Dictionary<string, int> textIds)
        {
            var chromatogramGroup = new ChromatogramFileData.Types.ChromatogramGroup()
            {
                CollisionalCrossSection = chromGroupHeaderInfo.CollisionalCrossSection??0,
                EndTime = chromGroupHeaderInfo.EndTime??0,
                StartTime = chromGroupHeaderInfo.StartTime??0,
            };
            string textId = cache.GetTextIdString(chromGroupHeaderInfo.TextIdIndex, chromGroupHeaderInfo.TextIdLen);
            if (textId != null)
            {
                if (textIds.TryGetValue(textId, out int textIdIndex))
                {
                    chromatogramGroup.TextIdIndex = textIdIndex;
                }
                else
                {
                    chromatogramGroup.TextIdIndex = textIds.Count + 1;
                    textIds.Add(textId, chromatogramGroup.TextIdIndex);
                }
            }

            return chromatogramGroup;
        }
    }
}
