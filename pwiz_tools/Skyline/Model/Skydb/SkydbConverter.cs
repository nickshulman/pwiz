using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using SkydbApi.DataApi;
using SkydbApi.Orm;

namespace pwiz.Skyline.Model.Skydb
{
    public class SkydbConverter : IDisposable
    {
        private SkydbWriter _skydbWriter;
        private InsertScoresStatement _insertScoreStatement;
        private IList<string> _scoreNames;
        public SkydbConverter(ChromatogramCache source, string targetFile)
        {
            Source = source;
            TargetFile = targetFile;

        }

        public ChromatogramCache Source { get; }
        public string TargetFile { get; }

        public void Convert()
        {
            var skydbFile = SkydbFile.CreateNewSkydbFile(TargetFile);
            _skydbWriter = skydbFile.OpenWriter();
            _skydbWriter.SetUnsafeJournalMode();
            _skydbWriter.BeginTransaction();
            _scoreNames = Source.ScoreTypes.Select(type => type.FullName).ToList();
            _skydbWriter.EnsureScores(_scoreNames);
            _insertScoreStatement = new InsertScoresStatement(_skydbWriter.Connection, _scoreNames);
            foreach (var grouping in Source.ChromGroupHeaderInfos.GroupBy(header => header.FileIndex))
            {
                WriteFileData(grouping.Key, grouping.ToList());
            }
            _skydbWriter.CommitTransaction();
        }

        public void Dispose()
        {
            _skydbWriter?.Dispose();
            _insertScoreStatement?.Dispose();
        }

        public void WriteFileData(int fileIndex, IList<ChromGroupHeaderInfo> chromGroupHeaderInfos)
        {
            var chromCachedFile = Source.CachedFiles[fileIndex];
            var msDataFile = new MsDataFile
            {
                FilePath = chromCachedFile.FilePath.ToString()
            };
            _skydbWriter.Insert(msDataFile);
            var msDataFileScanIds = Source.LoadMSDataFileScanIds(fileIndex);
            var scanInfos = WriteScanInfos(fileIndex, chromGroupHeaderInfos, msDataFile);
            var retentionTimeHashes = new Dictionary<Hash, long>();
            var scores = new Dictionary<Tuple<int, int>, long>();
            foreach (var groupHeader in chromGroupHeaderInfos)
            {
                WriteChromatogramGroup(msDataFileScanIds, groupHeader, scanInfos, retentionTimeHashes, scores);
            }
        }

        public Dictionary<Tuple<float, string>, int> WriteScanInfos(int fileIndex, IEnumerable<ChromGroupHeaderInfo> chromGroupHeaderInfos, MsDataFile msDataFile)
        {
            var msDataFileScanIds = Source.LoadMSDataFileScanIds(fileIndex);
            var scans = new HashSet<KeyValuePair<float, string>>();
            foreach (var chromGroupHeaderInfo in chromGroupHeaderInfos)
            {
                var timeIntensitiesGroup = Source.ReadTimeIntensities(chromGroupHeaderInfo);
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
                    MsDataFile = msDataFile,
                    RetentionTime = scan.Key,
                    SpectrumIdentifier = scan.Value,
                    SpectrumIndex = scanNumber,
                };
                scanNumber++;
                _skydbWriter.Insert(scanInfo);
                var key = Tuple.Create(scan.Key, scan.Value);
                if (result.ContainsKey(key))
                {
                    Assume.Fail(string.Format("{0} is already present", key));
                }
                result.Add(key, scanInfo.SpectrumIndex);
            }

            return result;
        }

        public IEnumerable<string> GetScanIdentifiers(MsDataFileScanIds scanIds)
        {
            for (int i = 0; ; i++)
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

        public void WriteChromatogramGroup(MsDataFileScanIds scanIds, ChromGroupHeaderInfo chromGroupHeaderInfo, Dictionary<Tuple<float, string>, int> scanInfos, IDictionary<Hash, long> retentionTimeHashes, IDictionary<Tuple<int, int>, long> scoreDictionary)
        {
            IList<float> scores = null;
            var peaks = Source.ReadPeaks(chromGroupHeaderInfo);
            for (int iPeakGroup = 0; iPeakGroup < chromGroupHeaderInfo.NumPeaks; iPeakGroup++)
            {
                Scores scoresEntity = null;
                if (_scoreNames.Count > 0)
                {
                    var scoreKey = Tuple.Create(chromGroupHeaderInfo.StartScoreIndex, iPeakGroup);
                    if (scoreDictionary.TryGetValue(scoreKey, out long scoresId))
                    {
                        scoresEntity = new Scores {Id = scoresId};
                    }
                    else
                    {
                        scores = scores ?? Source.ReadScores(chromGroupHeaderInfo);
                        scoresEntity = new Scores();
                        for (int iScore = 0; iScore < _scoreNames.Count; iScore++)
                        {
                            float scoreValue = scores[iScore + iPeakGroup * _scoreNames.Count];
                            if (!float.IsNaN(scoreValue))
                            {
                                scoresEntity.SetScore(_scoreNames[iScore], scoreValue);
                            }
                        }
                        _insertScoreStatement.Insert(scoresEntity);
                        scoreDictionary.Add(scoreKey, scoresEntity.Id.Value);
                    }

                }
                var peakGroupPeaks = Enumerable.Range(0, chromGroupHeaderInfo.NumTransitions)
                    .Select(iTransition => peaks[iTransition & chromGroupHeaderInfo.NumPeaks + iPeakGroup]).ToList();
                var peakGroup = new CandidatePeakGroup
                {
                    Scores = scoresEntity
                };
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
                    peakGroup.Identified = (int)PeakIdentification.TRUE;
                }
                else if (peakGroupPeaks.Any(peak => peak.Identified == PeakIdentification.ALIGNED))
                {
                    peakGroup.Identified = (int)PeakIdentification.ALIGNED;
                }
                _skydbWriter.Insert(peakGroup);
                foreach (var peak in peakGroupPeaks)
                {
                    var candidatePeak = new CandidatePeak
                    {
                        CandidatePeakGroup = peakGroup,
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
                    _skydbWriter.Insert(candidatePeak);
                }
            }
            var timeIntensitiesGroup = Source.ReadTimeIntensities(chromGroupHeaderInfo);
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
                        chromatogramData.SpectrumList = new SpectrumList
                        {
                            Id = spectrumListId
                        };
                    }
                    else
                    {
                        var spectrumList = new SpectrumList
                        {
                            SpectrumCount = timeIntensities.NumPoints,
                            SpectrumIndexData = Compress(spectrumIndexBytes)
                        };
                        _skydbWriter.Insert(spectrumList);
                    }
                    if (timeIntensities.MassErrors != null)
                    {
                        chromatogramData.MassErrorsData =
                            Compress(PrimitiveArrays.ToBytes(timeIntensities.MassErrors.ToArray()));
                    }
                    _skydbWriter.Insert(chromatogramData);
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
                Assume.IsTrue(scanInfos.TryGetValue(key, out int spectrumIndex), string.Format(@"Could not find spectrum {0}", key));
                yield return spectrumIndex;
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
