using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SkydbApi.ChromatogramData;
using SkydbStorage.DataApi;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.Internal
{
    public class MsDataSourceFileWriter : IDisposable
    {
        private IList<string> _scoreNames;
        private InsertScoresStatement _insertScoresStatement;
        private ExtractedFile _msDataFile;
        private Dictionary<Tuple<float, string>, int> _spectrumIds = new Dictionary<Tuple<float, string>, int>();
        private Dictionary<HashValue, long> _spectrumIndexLists = new Dictionary<HashValue, long>();
        private Dictionary<HashValue, long> _retentionTimeLists = new Dictionary<HashValue, long>();
        private Dictionary<HashValue, long> _scoreDictionary = new Dictionary<HashValue, long>();

        public MsDataSourceFileWriter(SkydbWriter writer, IExtractedDataFile msDataSourceFile)
        {
            Writer = writer;
            MsDataSourceFile = msDataSourceFile;
            _scoreNames = InsertScoresStatement.GetScoreNames(writer.Connection).ToList();
            _insertScoresStatement = new InsertScoresStatement(writer.Connection, _scoreNames);
        }

        public void Dispose()
        {
            _insertScoresStatement.Dispose();
        }

        public IExtractedDataFile MsDataSourceFile { get; }

        public SkydbWriter Writer { get; }

        public void Write()
        {
            _msDataFile = new ExtractedFile()
            {
                FilePath = MsDataSourceFile.SourceFilePath
            };
            Writer.Insert(_msDataFile);
            WriteSpectrumInfos();
            foreach (var group in MsDataSourceFile.ChromatogramGroups)
            {
                WriteGroup(group);
            }
        }

        private void WriteGroup(IChromatogramGroup group)
        {
            var chromGroup = new ChromatogramGroup()
            {
                File = _msDataFile,
                StartTime = group.StartTime,
                EndTime = group.EndTime,
                PrecursorMz = group.PrecursorMz,
                TextId = group.TextId
            };
            var interpolationParams = group.InterpolationParameters;
            if (interpolationParams != null)
            {
                chromGroup.InterpolationStartTime = interpolationParams.StartTime;
                chromGroup.InterpolationEndTime = interpolationParams.EndTime;
                chromGroup.InterpolationNumberOfPoints = interpolationParams.NumberOfPoints;
                chromGroup.InterpolationIntervalDelta = interpolationParams.IntervalDelta;
                chromGroup.InterpolationInferZeroes = interpolationParams.InferZeroes;
            }
            Writer.Insert(chromGroup);
            var transitionChromatograms = new List<Chromatogram>();
            foreach (var chromatogram in group.Chromatograms)
            {
                transitionChromatograms.Add(WriteChromatogram(chromGroup, chromatogram));
            }
            WritePeaks(chromGroup, transitionChromatograms, group);
        }

        private Chromatogram WriteChromatogram(ChromatogramGroup group, IChromatogram chromatogram)
        {
            var chromatogamData = WriteChromatogramData(chromatogram);
            var chromTransition = new Chromatogram()
            {
                ChromatogramGroup = group,
                ChromatogramData = chromatogamData,
                ExtractionWidth = chromatogram.ExtractionWidth,
                IonMobilityExtractionWidth = chromatogram.IonMobilityExtractionWidth,
                IonMobilityValue = chromatogram.IonMobilityValue,
                ProductMz = chromatogram.ProductMz,
                
            };
            Writer.Insert(chromTransition);
            return chromTransition;
        }

        private ChromatogramData WriteChromatogramData(IChromatogram data)
        {
            if (data == null)
            {
                return null;
            }

            var chromatogramData = new ChromatogramData()
            {
                IntensitiesBlob = DataUtil.Compress(DataUtil.PrimitivesToByteArray(data.Intensities.ToArray())),
            };
            var massErrors = data.MassErrors;
            if (massErrors != null)
            {
                chromatogramData.MassErrorsBlob =
                    DataUtil.Compress(DataUtil.PrimitivesToByteArray(massErrors.ToArray()));
            }
            var spectrumIndexes = GetSpectrumIndexes(data.RetentionTimes, data.SpectrumIdentifiers);
            if (spectrumIndexes != null)
            {
                var spectrumIndexBytes = DataUtil.PrimitivesToByteArray(spectrumIndexes);
                var hashCode = HashValue.HashBytes(spectrumIndexBytes);
                if (_spectrumIndexLists.TryGetValue(hashCode, out long id))
                {
                    chromatogramData.SpectrumList = new SpectrumList {Id = id};
                }
                else
                {
                    var spectrumList = new SpectrumList
                    {
                        File = _msDataFile,
                        SpectrumCount = spectrumIndexes.Length,
                        SpectrumIndexBlob = DataUtil.Compress(spectrumIndexBytes)
                    };
                    Writer.Insert(spectrumList);
                    _spectrumIndexLists.Add(hashCode, spectrumList.Id.Value);
                    chromatogramData.SpectrumList = spectrumList;
                }
            }

            if (chromatogramData.SpectrumList == null)
            {
                var retentionTimeBytes = DataUtil.PrimitivesToByteArray(data.RetentionTimes.ToArray());
                var hashCode = HashValue.HashBytes(retentionTimeBytes);
                if (_retentionTimeLists.TryGetValue(hashCode, out long id))
                {
                    chromatogramData.SpectrumList = new SpectrumList {Id = id};
                }
                else
                {
                    var spectrumList = new SpectrumList()
                    {
                        File = _msDataFile,
                        SpectrumCount = data.NumPoints,
                        RetentionTimeBlob = DataUtil.Compress(retentionTimeBytes)
                    };
                    Writer.Insert(spectrumList);
                    _retentionTimeLists.Add(hashCode, spectrumList.Id.Value);
                    chromatogramData.SpectrumList = spectrumList;
                }
            }
            Writer.Insert(chromatogramData);
            return chromatogramData;
        }

        private int[] GetSpectrumIndexes(IList<float> retentionTimes, IList<string> spectrumIdentifiers)
        {
            if (spectrumIdentifiers == null)
            {
                return null;
            }
            var spectrumIndexes = new int[retentionTimes.Count];
            for (int i = 0; i < spectrumIndexes.Length; i++)
            {
                var key = Tuple.Create(retentionTimes[i], spectrumIdentifiers[i]);
                if (!_spectrumIds.TryGetValue(key, out int spectrumIndex))
                {
                    Trace.TraceWarning("Unable to find spectrum index {0}", key);
                    return null;
                }

                spectrumIndexes[i] = spectrumIndex;
            }

            return spectrumIndexes;
        }

        public void WriteSpectrumInfos()
        {
            var scanTimes = new HashSet<Tuple<float, string>>();
            foreach (var group in MsDataSourceFile.ChromatogramGroups)
            {
                foreach (var transition in group.Chromatograms)
                {
                    var spectrumIdentifiers = transition.SpectrumIdentifiers;
                    if (spectrumIdentifiers == null)
                    {
                        continue;
                    }

                    var times = transition.RetentionTimes;
                    scanTimes.UnionWith(Enumerable.Range(0, times.Count).Select(i=>Tuple.Create(times[i], spectrumIdentifiers[i])));
                }
            }

            var orderedScans = scanTimes.OrderBy(t => t).ToList();
            for (int scanNumber = 0; scanNumber < orderedScans.Count; scanNumber++)
            {
                var scan = orderedScans[scanNumber];
                var scanInfo = new SpectrumInfo
                {
                    File = _msDataFile,
                    RetentionTime = scan.Item1,
                    SpectrumIndex = scanNumber,
                };
                scanInfo.SetSpectrumIdentifier(scan.Item2);
                Writer.Insert(scanInfo);
                _spectrumIds.Add(scan, scanInfo.SpectrumIndex);
            }
        }

        private void WritePeaks(ChromatogramGroup chromatogramGroup, IList<Chromatogram> transitionChromatograms, IChromatogramGroup group)
        {
            foreach (var candidatePeakGroup in group.CandidatePeakGroups)
            {
                double[] scores = _scoreNames.Select(score => candidatePeakGroup.GetScore(score) ?? double.NaN).ToArray();
                Scores scoresEntity = null;
                if (scores.Any(s=>!double.IsNaN(s)))
                {
                    var scoreKey = HashValue.HashBytes(DataUtil.PrimitivesToByteArray(scores));
                    if (_scoreDictionary.TryGetValue(scoreKey, out long scoresId))
                    {
                        scoresEntity = new Scores { Id = scoresId };
                    }
                    else
                    {
                        scoresEntity = new Scores();
                        for (int iScore = 0; iScore < _scoreNames.Count; iScore++)
                        {
                            if (!double.IsNaN(scores[iScore]))
                            {
                                scoresEntity.SetScore(_scoreNames[iScore], scores[iScore]);
                            }
                        }

                        _insertScoresStatement.Insert(scoresEntity);
                        _scoreDictionary.Add(scoreKey, scoresEntity.Id.Value);
                    }

                }

                var peakGroupPeaks = candidatePeakGroup.CandidatePeaks.ToList();
                var peakGroup = new CandidatePeakGroup
                {
                    ChromatogramGroup = chromatogramGroup,
                    Scores = scoresEntity,
                    IsBestPeak = candidatePeakGroup.IsBestPeak
                };
                foreach (var peak in peakGroupPeaks)
                {
                    if (peak == null)
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

                peakGroup.Identified = (int) candidatePeakGroup.Identified;
                Writer.Insert(peakGroup);
                var transitions = group.Chromatograms.ToList();
                for (int iTransition = 0; iTransition < transitions.Count; iTransition++)
                {
                    var peak = peakGroupPeaks[iTransition];
                    var candidatePeak = new CandidatePeak
                    {
                        Chromatogram = transitionChromatograms[iTransition],
                        CandidatePeakGroup = peakGroup,
                        Area = peak.Area,
                        BackgroundArea = peak.BackgroundArea,
                        DegenerateFwhm = peak.DegenerateFwhm,
                        ForcedIntegration = peak.ForcedIntegration,
                        FullWidthAtHalfMax = peak.FullWidthAtHalfMax,
                        Height = peak.Height,
                        MassError = peak.MassError,
                        PointsAcross = peak.PointsAcross,
                        Truncated = peak.Truncated
                    };
                    if (peak.StartTime != peakGroup.StartTime)
                    {
                        candidatePeak.StartTime = peak.StartTime;
                    }

                    if (peak.EndTime != peakGroup.EndTime)
                    {
                        candidatePeak.EndTime = peak.EndTime;
                    }
                    Writer.Insert(candidatePeak);
                }
            }
        }
    }
}
