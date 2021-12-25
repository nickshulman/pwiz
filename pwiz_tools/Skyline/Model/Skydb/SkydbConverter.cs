using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NHibernate.Criterion;
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
            var result = new Dictionary<Tuple<float, string>, int>();
            var msDataFileScanIds = Source.LoadMSDataFileScanIds(fileIndex);
            if (msDataFileScanIds == null)
            {
                return result;
            }
            var scanTimeIndexes = new HashSet<KeyValuePair<float, int>>();
            foreach (var chromGroupHeaderInfo in chromGroupHeaderInfos)
            {
                var timeIntensitiesGroup = Source.ReadTimeIntensities(chromGroupHeaderInfo);
                if (timeIntensitiesGroup == null)
                {
                    continue;
                }

                var processed = new List<Tuple<ImmutableList<float>, ImmutableList<int>>>();
                foreach (var timeIntensities in timeIntensitiesGroup.TransitionTimeIntensities)
                {
                    if (timeIntensities.ScanIds == null)
                    {
                        continue;
                    }
                    var newTuple = Tuple.Create(timeIntensities.Times, timeIntensities.ScanIds);
                    if (processed.Any(tuple =>
                        ReferenceEquals(tuple.Item1, newTuple.Item1) && ReferenceEquals(tuple.Item2, newTuple.Item2)))
                    {
                        continue;
                    }
                    processed.Add(newTuple);
                    for (int i = 0; i < timeIntensities.NumPoints; i++)
                    {
                        scanTimeIndexes.Add(new KeyValuePair<float, int>(timeIntensities.Times[i],
                            timeIntensities.ScanIds?[i] ?? -1));
                    }
                }
            }
            var scans = new HashSet<Tuple<float, string>>();
            foreach (var scanTimeIndex in scanTimeIndexes)
            {
                string spectrumIdentifier = null;
                if (scanTimeIndex.Value != -1)
                {
                    spectrumIdentifier = msDataFileScanIds?.GetMsDataFileSpectrumId(scanTimeIndex.Value);
                }
                scans.Add(Tuple.Create(scanTimeIndex.Key, spectrumIdentifier));
            }
            int scanNumber = 0;
            var orderedScans = scans.OrderBy(scan => scan).ToList();
            foreach (var scan in orderedScans)
            {
                var scanInfo = new SpectrumInfo
                {
                    MsDataFile = msDataFile,
                    RetentionTime = scan.Item1,
                    SpectrumIndex = scanNumber,
                };
                scanInfo.SetSpectrumIdentifier(scan.Item2);
                scanNumber++;
                _skydbWriter.Insert(scanInfo);
                if (result.ContainsKey(scan))
                {
                    Assume.Fail(string.Format("{0} is already present", scan));
                }
                result.Add(scan, scanInfo.SpectrumIndex);
            }

            return result;
        }

        public void WriteChromatogramGroup(MsDataFileScanIds scanIds, ChromGroupHeaderInfo chromGroupHeaderInfo, Dictionary<Tuple<float, string>, int> scanInfos, IDictionary<Hash, long> retentionTimeHashes, IDictionary<Tuple<int, int>, long> scoreDictionary)
        {
            var chromatogramGroup = new ChromatogramGroup()
            {
                StartTime = chromGroupHeaderInfo.StartTime,
                EndTime = chromGroupHeaderInfo.EndTime,
                PrecursorMz = chromGroupHeaderInfo.Precursor,
                TextId = Source.GetTextId(chromGroupHeaderInfo),
            };
            _skydbWriter.Insert(chromatogramGroup);
            var transitionChromatograms = new List<TransitionChromatogram>(chromGroupHeaderInfo.NumTransitions);
            for (int iTransition = 0; iTransition < chromGroupHeaderInfo.NumTransitions; iTransition++)
            {
                var chromTransition = Source.GetTransition(chromGroupHeaderInfo.StartTransitionIndex + iTransition);
                var transitionChromatogram = new TransitionChromatogram
                {
                    ChromatogramGroup = chromatogramGroup,
                    ExtractionWidth = chromTransition.ExtractionWidth,
                    IonMobilityExtractionWidth = chromTransition.IonMobilityExtractionWidth == 0
                        ? (double?) null
                        : chromTransition.IonMobilityExtractionWidth,
                    IonMobilityValue = chromTransition.IonMobilityValue == 0
                        ? (double?) null
                        : chromTransition.IonMobilityValue,
                    Source = (int) chromTransition.Source,
                    ProductMz = chromTransition.Product
                };
                _skydbWriter.Insert(transitionChromatogram);
                transitionChromatograms.Add(transitionChromatogram);
            }

            WritePeaks(chromGroupHeaderInfo, chromatogramGroup, transitionChromatograms, scoreDictionary);
            WriteChromatogramData(scanIds, chromGroupHeaderInfo, chromatogramGroup, transitionChromatograms, scanInfos, retentionTimeHashes);
        }

        private void WriteChromatogramData(MsDataFileScanIds scanIds, ChromGroupHeaderInfo chromGroupHeaderInfo,
            ChromatogramGroup chromatogramGroup,
            IList<TransitionChromatogram> transitionChromatograms, Dictionary<Tuple<float, string>, int> scanInfos, IDictionary<Hash, long> retentionTimeHashes)
        {
            var timeIntensitiesGroup = Source.ReadTimeIntensities(chromGroupHeaderInfo);
            if (timeIntensitiesGroup != null)
            {
                for (int iTransition = 0; iTransition < chromGroupHeaderInfo.NumTransitions; iTransition++)
                {
                    var timeIntensities = timeIntensitiesGroup.TransitionTimeIntensities[iTransition];
                    var chromatogramData = new ChromatogramData
                    {
                        PointCount = timeIntensities.NumPoints,
                        IntensitiesData = Compress(PrimitiveArrays.ToBytes(timeIntensities.Intensities.ToArray())),
                    };
                    SpectrumList spectrumList;
                    if (scanIds != null && timeIntensities.ScanIds != null)
                    {
                        var spectrumIndexList = GetSpectrumIndexes(scanIds, scanInfos, timeIntensities).ToArray();
                        var spectrumIndexBytes = PrimitiveArrays.ToBytes(spectrumIndexList);
                        var retentionTimeHash = GetHashCode(spectrumIndexBytes);
                        if (retentionTimeHashes.TryGetValue(retentionTimeHash, out long spectrumListId))
                        {
                            spectrumList = new SpectrumList {Id = spectrumListId};
                        }
                        else
                        {
                            spectrumList = new SpectrumList
                            {
                                SpectrumCount = timeIntensities.NumPoints,
                                SpectrumIndexData = Compress(spectrumIndexBytes)
                            };
                            _skydbWriter.Insert(spectrumList);
                            retentionTimeHashes.Add(retentionTimeHash, spectrumList.Id.Value);
                        }
                    }
                    else
                    {
                        var retentionTimeBytes = PrimitiveArrays.ToBytes(timeIntensities.Times.ToArray());
                        var retentionTimeHash = GetHashCode(retentionTimeBytes);
                        if (retentionTimeHashes.TryGetValue(retentionTimeHash, out long spectrumListId))
                        {
                            spectrumList = new SpectrumList {Id = spectrumListId};
                        }
                        else
                        {
                            spectrumList = new SpectrumList
                            {
                                SpectrumCount = timeIntensities.NumPoints,
                                RetentionTimeData = Compress(retentionTimeBytes)
                            };
                            _skydbWriter.Insert(spectrumList);
                            retentionTimeHashes.Add(retentionTimeHash, spectrumList.Id.Value);

                        }
                    }
                    chromatogramData.SpectrumList = spectrumList;
                    if (timeIntensities.MassErrors != null)
                    {
                        chromatogramData.MassErrorsData =
                            Compress(PrimitiveArrays.ToBytes(timeIntensities.MassErrors.ToArray()));
                    }
                    _skydbWriter.Insert(chromatogramData);
                }
            }

        }

        private void WritePeaks(ChromGroupHeaderInfo chromGroupHeaderInfo,
            ChromatogramGroup chromatogramGroup,
            IList<TransitionChromatogram> transitionChromatograms,
            IDictionary<Tuple<int, int>, long> scoreDictionary)
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
                        scoresEntity = new Scores { Id = scoresId };
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
                    .Select(iTransition => peaks[iTransition * chromGroupHeaderInfo.NumPeaks + iPeakGroup]).ToList();
                var peakGroup = new CandidatePeakGroup
                {
                    ChromatogramGroup = chromatogramGroup,
                    Scores = scoresEntity,
                    IsBestPeak = iPeakGroup == chromGroupHeaderInfo.MaxPeakIndex
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
                for (int iTransition = 0; iTransition < chromGroupHeaderInfo.NumTransitions; iTransition++)
                {
                    var peak = peakGroupPeaks[iTransition];
                    var candidatePeak = new CandidatePeak
                    {
                        TransitionChromatogram = transitionChromatograms[iTransition],
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
                if (!scanInfos.TryGetValue(key, out int spectrumIndex))
                {
                    Assume.Fail(string.Format(@"Could not find spectrum {0}", key));
                }
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
