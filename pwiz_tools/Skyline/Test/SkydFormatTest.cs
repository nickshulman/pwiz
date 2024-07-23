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
using pwiz.Skyline.Properties;
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
            TestFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip");
            DateTime start = DateTime.UtcNow;
            var path = TestFilesDir.GetTestPath("test.skydb");
            var outputFile = SkydbFile.CreateNewSkydbFile(path);
            var doc = new SrmDocument(SrmSettingsList.GetDefault());
            using var chromatogramCache = ChromatogramCache.Load(
                TestFilesDir.GetTestPath("Human_plasma.skyd"),
                new ProgressStatus(),
                new DefaultFileLoadMonitor(new SilentProgressMonitor()), doc);
            using var skydbConnection = outputFile.OpenConnection();
            skydbConnection.SetUnsafeJournalMode();
            skydbConnection.BeginTransaction();
            skydbConnection.EnsureScores(chromatogramCache.ScoreTypes);
            var fileGroups = chromatogramCache.ChromGroupHeaderInfos.GroupBy(header => header.FileIndex).ToList();
            ParallelEx.ForEach(fileGroups, grouping=>
            {
                WriteFileData(skydbConnection, chromatogramCache, grouping.Key,
                    grouping.ToList());
            });
            skydbConnection.CommitTransaction();
            PreparedStatement.DumpStatements();
            Console.Out.WriteLine("Elapsed time {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
            Console.Out.WriteLine("File Size: {0}", new FileInfo(outputFile.FilePath).Length);
        }

        [TestMethod]
        public void TestMerge()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"Test\SkydFormatTest.zip");
            DateTime start = DateTime.UtcNow;
            var doc = new SrmDocument(SrmSettingsList.GetDefault());
            using var chromatogramCache = ChromatogramCache.Load(
                TestFilesDir.GetTestPath("Human_plasma.skyd"),
                new ProgressStatus(),
                new DefaultFileLoadMonitor(new SilentProgressMonitor()), doc);
            var fileGroups = chromatogramCache.ChromGroupHeaderInfos.GroupBy(header => header.FileIndex).Select(grouping=>Tuple.Create(grouping, TestFilesDir.GetTestPath("test" + grouping.Key + ".skydb"))).ToList();

            ParallelEx.ForEach(fileGroups, tuple =>
            {
                var grouping = tuple.Item1;
                var outputFile = SkydbFile.CreateNewSkydbFile(tuple.Item2);
                using var skydbConnection = outputFile.OpenConnection();
                skydbConnection.SetUnsafeJournalMode();
                skydbConnection.BeginTransaction();
                skydbConnection.EnsureScores(chromatogramCache.ScoreTypes);
                WriteFileData(skydbConnection, chromatogramCache, grouping.Key,
                    grouping.ToList());
                skydbConnection.CommitTransaction();
            });
            var firstSkydbFile = new SkydbFile(fileGroups[0].Item2);
            using var connection = firstSkydbFile.OpenConnection();
            var merger = new SkydbMerger(connection);
            foreach (var tuple in fileGroups.Skip(1))
            {
                merger.Merge(tuple.Item2);
            }
            PreparedStatement.DumpStatements();
            Console.Out.WriteLine("Elapsed time {0}", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
            //Console.Out.WriteLine("File Size: {0}", new FileInfo(outputFile.FilePath).Length);
        }

        public void WriteFileData(SkydbConnection connection, ChromatogramCache cache, int fileIndex,
            IList<ChromGroupHeaderInfo> chromGroupHeaderInfos)
        {
            var chromCachedFile = cache.CachedFiles[fileIndex];
            var msDataFile = new MsDataFile
            {
                FilePath = chromCachedFile.FilePath.ToString()
            };
            connection.Insert(msDataFile);
            var msDataFileScanIds = cache.LoadMSDataFileScanIds(fileIndex);
            var scanInfos = WriteScanInfos(connection, cache, fileIndex, chromGroupHeaderInfos, msDataFile);
            var retentionTimeHashes = new Dictionary<Hash, long>();
            foreach (var groupHeader in chromGroupHeaderInfos)
            {
                WriteChromatogramGroup(connection, cache, msDataFileScanIds, groupHeader, scanInfos, retentionTimeHashes);
            }
        }

        public Dictionary<Tuple<float, string>, int> WriteScanInfos(SkydbConnection connection, ChromatogramCache cache, int fileIndex, IEnumerable<ChromGroupHeaderInfo> chromGroupHeaderInfos, MsDataFile msDataFile)
        {
            var msDataFileScanIds = cache.LoadMSDataFileScanIds(fileIndex);
            var scans = new HashSet<KeyValuePair<float, string>>();
            foreach (var chromGroupHeaderInfo in chromGroupHeaderInfos)
            {
                var timeIntensitiesGroup = cache.ReadTimeIntensities(chromGroupHeaderInfo);
                if (timeIntensitiesGroup == null)
                {
                    continue;
                }

                foreach (var timeIntensities in timeIntensitiesGroup.TransitionTimeIntensities)
                {
                    for (int i = 0; i < timeIntensities.NumPoints; i++)
                    {
                        string spectrumIdentifier = null;
                        if (timeIntensities.ScanIds != null)
                        {
                            spectrumIdentifier = msDataFileScanIds?.GetMsDataFileSpectrumId(timeIntensities.ScanIds[i]);
                        }
                        scans.Add(new KeyValuePair<float, string>(timeIntensities.Times[i], spectrumIdentifier));
                    }
                }
            }

            var result = new Dictionary<Tuple<float, string>, int>();
            int scanNumber = 0;
            var orderedScans = scans.OrderBy(scan => Tuple.Create(scan.Key, scan.Value)).ToList();
            foreach (var scan in orderedScans)
            {
                var scanInfo = new SpectrumInfo
                {
                    MsDataFileId = msDataFile.Id.Value,
                    RetentionTime = scan.Key,
                    SpectrumIdentifier = scan.Value,
                    SpectrumIndex = scanNumber,
                };
                scanNumber++;
                connection.Insert(scanInfo);
                var key = Tuple.Create(scan.Key, scan.Value);
                if (result.ContainsKey(key))
                {
                    Assert.Fail("{0} is already present", key);
                }
                result.Add(key, scanInfo.SpectrumIndex);
            }

            return result;
        }

        public IEnumerable<string> GetScanIdentifiers(MsDataFileScanIds scanIds)
        {
            for (int i = 0;; i++)
            {
                string str;
                try
                {
                    str = scanIds.GetMsDataFileSpectrumId(i);
                }
                catch (IndexOutOfRangeException)
                {
                    yield break;
                }

                yield return str;
            }
        }

        public bool HasDuplicates(MsDataFileScanIds scanIds)
        {
            var identifiers = GetScanIdentifiers(scanIds).ToList();
            var duplicateCounts = identifiers.GroupBy(str => str).Where(grouping => grouping.Count() > 1)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.Count());
            var indexes = duplicateCounts.ToDictionary(kvp => kvp.Key,
                kvp => Enumerable.Range(0, identifiers.Count).Where(i => kvp.Key == identifiers[i]).ToList());
            return indexes.Any();
        }

        public void WriteChromatogramGroup(SkydbConnection connection,
            ChromatogramCache cache, MsDataFileScanIds scanIds, ChromGroupHeaderInfo chromGroupHeaderInfo, Dictionary<Tuple<float, string>, int> scanInfos, IDictionary<Hash, long> retentionTimeHashes)
        {
            var peaks = cache.ReadPeaks(chromGroupHeaderInfo);
            for (int iPeakGroup = 0; iPeakGroup < chromGroupHeaderInfo.NumPeaks; iPeakGroup++)
            {
                var peakGroupPeaks = Enumerable.Range(0, chromGroupHeaderInfo.NumTransitions)
                    .Select(iTransition => peaks[iTransition & chromGroupHeaderInfo.NumPeaks + iPeakGroup]).ToList();
                var peakGroup = new CandidatePeakGroup();
                foreach (var peak in peakGroupPeaks)
                {
                    if (peak.IsEmpty)
                    {
                        continue;
                    }

                    if (peakGroup.StartTime == null || peakGroup.StartTime > peak.StartTime)
                    {
                        peakGroup.StartTime = peak.StartTime;
                    }

                    if (peakGroup.EndTime == null || peakGroup.EndTime < peak.EndTime)
                    {
                        peakGroup.EndTime = peak.EndTime;
                    }
                }

                if (peakGroupPeaks.Any(peak => peak.Identified == PeakIdentification.TRUE))
                {
                    peakGroup.Identified = (int) PeakIdentification.TRUE;
                }
                else if (peakGroupPeaks.Any(peak => peak.Identified == PeakIdentification.ALIGNED))
                {
                    peakGroup.Identified = (int) PeakIdentification.ALIGNED;
                }
                connection.Insert(peakGroup);
                foreach (var peak in peakGroupPeaks)
                {
                    var candidatePeak = new CandidatePeak
                    {
                        CandidatePeakGroupId = peakGroup.Id.Value,
                        Area = peak.Area,
                        BackgroundArea = peak.BackgroundArea,
                        DegenerateFwhm = peak.IsFwhmDegenerate,
                        ForcedIntegration = peak.IsForcedIntegration,
                        FullWidthAtHalfMax = peak.Fwhm,
                        Height = peak.Height,
                        MassError = peak.MassError,
                        PointsAcross = peak.PointsAcross,
                        TimeNormalized = 0 != (peak.Flags & ChromPeak.FlagValues.time_normalized),
                        Truncated = peak.IsTruncated
                    };
                    if (peak.StartTime != peakGroup.StartTime)
                    {
                        candidatePeak.StartTime = peak.StartTime;
                    }

                    if (peak.EndTime != peakGroup.EndTime)
                    {
                        candidatePeak.EndTime = peak.EndTime;
                    }
                    connection.Insert(candidatePeak);
                }
            }
            var timeIntensitiesGroup = cache.ReadTimeIntensities(chromGroupHeaderInfo);
            if (timeIntensitiesGroup != null)
            {
                for (int iTransition = 0; iTransition < chromGroupHeaderInfo.NumTransitions; iTransition++)
                {
                    var timeIntensities = timeIntensitiesGroup.TransitionTimeIntensities[iTransition];
                    var spectrumIndexList = GetSpectrumIndexes(scanIds, scanInfos, timeIntensities).ToArray();
                    var spectrumIndexBytes = PrimitiveArrays.ToBytes(spectrumIndexList);
                    var retentionTimeHash = GetHashCode(spectrumIndexBytes);
                    var chromatogramData = new ChromatogramData
                    {
                        PointCount = timeIntensities.NumPoints,
                        IntensitiesData = Compress(PrimitiveArrays.ToBytes(timeIntensities.Intensities.ToArray())),
                    };
                    if (retentionTimeHashes.TryGetValue(retentionTimeHash, out long spectrumListId))
                    {
                        chromatogramData.SpectrumListId = spectrumListId;
                    }
                    else
                    {
                        var spectrumList = new SpectrumList
                        {
                            SpectrumCount = timeIntensities.NumPoints,
                            SpectrumIndexData = Compress(spectrumIndexBytes)
                        };
                        connection.Insert(spectrumList);
                    }
                    if (timeIntensities.MassErrors != null)
                    {
                        chromatogramData.MassErrorsData =
                            Compress(PrimitiveArrays.ToBytes(timeIntensities.MassErrors.ToArray()));
                    }
                    connection.Insert(chromatogramData);
                    if (chromatogramData.RetentionTimesData != null)
                    {
                        retentionTimeHashes.Add(retentionTimeHash, chromatogramData.Id.Value);
                    }
                }
            }
        }

        private IEnumerable<int> GetSpectrumIndexes(MsDataFileScanIds msDataFileScanIds,
            Dictionary<Tuple<float, string>, int> scanInfos, TimeIntensities timeIntensities)
        {
            for (int i = 0; i < timeIntensities.NumPoints; i++)
            {
                string scanIdentifier = null;
                if (timeIntensities.ScanIds != null)
                {
                    scanIdentifier = msDataFileScanIds?.GetMsDataFileSpectrumId(timeIntensities.ScanIds[i]);
                }

                var key = Tuple.Create(timeIntensities.Times[i], scanIdentifier);
                Assert.IsTrue(scanInfos.TryGetValue(key, out int spectrumIndex), "Could not find spectrum {0}", key);
                yield return spectrumIndex;
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

        private byte[] GetHashCode(byte[] bytes)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return sha1.ComputeHash(bytes);
            }
        }

        private byte[] Compress(byte[] bytes)
        {
            //return bytes;
            return UtilDB.Compress(bytes);
        }
    }
}
