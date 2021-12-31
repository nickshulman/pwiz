using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using SkylineApi;

namespace pwiz.Skyline.Model.Skydb
{
    public class LegacyChromatogramCache : IDisposable
    {
        private ChromatogramCache _chromatogramCache;
        private Stream _stream;
        public LegacyChromatogramCache(ChromatogramCache chromatogramCache)
        {
            _chromatogramCache = chromatogramCache;
            _stream = _chromatogramCache.OpenReadStream();
            var scoreNameToIndex = new Dictionary<string, int>();
            foreach (var scoreType in _chromatogramCache.ScoreTypes)
            {
                scoreNameToIndex.Add(scoreType.FullName, scoreNameToIndex.Count);
            }

            ScoreNameToIndex = scoreNameToIndex;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public IDictionary<string, int> ScoreNameToIndex
        {
            get;
        }

        public IEnumerable<IExtractedDataFile> ExtractedDataFiles
        {
            get
            {
                return Enumerable.Range(0, _chromatogramCache.CachedFiles.Count)
                    .Select(i => new MsDataSourceFile(this, i));
            }
        }

        class MsDataSourceFile : IExtractedDataFile
        {
            public MsDataSourceFile(LegacyChromatogramCache cache, int fileIndex)
            {
                Cache = cache;
                FileIndex = fileIndex;
                MsDataFileScanIds = Cache._chromatogramCache.LoadMSDataFileScanIds(fileIndex);
                ChromCachedFile = Cache._chromatogramCache.CachedFiles[fileIndex];
            }

            public LegacyChromatogramCache Cache { get; }
            public ChromCachedFile ChromCachedFile { get; }
            public int FileIndex { get; }
            public MsDataFileScanIds MsDataFileScanIds { get; }

            public string SourceFilePath
            {
                get { return ChromCachedFile.FilePath.ToString(); }
            }

            public IEnumerable<IChromatogramGroup> ChromatogramGroups
            {
                get
                {
                    return Cache._chromatogramCache.ChromGroupHeaderInfos.Where(group => group.FileIndex == FileIndex)
                        .Select(group => new ExtractedChromatogramGroup(this, group));
                }
            }

            public IEnumerable<string> ScoreNames => Cache._chromatogramCache.ScoreTypes.Select(type=>type.FullName);

            public DateTime? LastWriteTime => ChromCachedFile.FileWriteTime;

            public bool HasCombinedIonMobility => ChromCachedFile.HasCombinedIonMobility;

            public bool Ms1Centroid => ChromCachedFile.UsedMs1Centroids;

            public bool Ms2Centroid => ChromCachedFile.UsedMs2Centroids;

            public DateTime? RunStartTime => ChromCachedFile.RunStartTime;

            public double? MaxRetentionTime => ChromCachedFile.MaxRetentionTime;

            public double? MaxIntensity => ChromCachedFile.MaxIntensity;

            public double? TotalIonCurrentArea => ChromCachedFile.TicArea;

            public string SampleId => ChromCachedFile.SampleId;

            public string InstrumentSerialNumber => ChromCachedFile.InstrumentSerialNumber;

            public IEnumerable<InstrumentInfo> InstrumentInfos
            {
                get
                {
                    return ChromCachedFile.InstrumentInfoList.Select(i =>
                        new InstrumentInfo(i.Model, i.Ionization, i.Analyzer, i.Detector));
                }
            }
        }

        class ExtractedChromatogramGroup : IChromatogramGroup
        {
            private TimeIntensitiesGroup _timeIntensitiesGroup;
            private IList<ChromPeak> _chromPeaks;
            public ExtractedChromatogramGroup(MsDataSourceFile sourceFile, ChromGroupHeaderInfo chromGroupHeaderInfo)
            {
                MsDataSourceFile = sourceFile;
                ChromGroupHeaderInfo = chromGroupHeaderInfo;
                Cache = MsDataSourceFile.Cache;
            }

            public MsDataSourceFile MsDataSourceFile { get; }
            public ChromGroupHeaderInfo ChromGroupHeaderInfo { get; }
            public LegacyChromatogramCache Cache { get; }

            public double PrecursorMz => ChromGroupHeaderInfo.Precursor;

            public string TextId => Cache._chromatogramCache.GetTextId(ChromGroupHeaderInfo);

            public double? StartTime => ChromGroupHeaderInfo.StartTime;

            public double? EndTime => ChromGroupHeaderInfo.EndTime;

            public double? CollisionalCrossSection => ChromGroupHeaderInfo.CollisionalCrossSection;

            public IEnumerable<IChromatogram> Chromatograms => 
                Enumerable.Range(0, ChromGroupHeaderInfo.NumTransitions).Select(i => new ExtractedChromatogram(this, i));

            public TimeIntensitiesGroup GetTimeIntensitiesGroup()
            {
                if (_timeIntensitiesGroup == null)
                {
                    _timeIntensitiesGroup = Cache._chromatogramCache.ReadTimeIntensities(Cache._stream, ChromGroupHeaderInfo);
                }
                return _timeIntensitiesGroup;
            }

            public IList<ChromPeak> GetChromPeaks(int transitionIndex)
            {
                if (_chromPeaks == null)
                {
                    _chromPeaks = Cache._chromatogramCache.ReadPeaks(Cache._stream, ChromGroupHeaderInfo);
                }

                if (_chromPeaks == null)
                {
                    return Array.Empty<ChromPeak>();
                }

                return Enumerable.Range(0, ChromGroupHeaderInfo.NumPeaks).Select(p =>
                    _chromPeaks[p + transitionIndex * ChromGroupHeaderInfo.NumPeaks]).ToList();
            }


            public InterpolationParameters InterpolationParameters {
                get
                {
                    var interpolationParams = (GetTimeIntensitiesGroup() as RawTimeIntensities)?.InterpolationParams;
                    if (interpolationParams == null)
                    {
                        return null;
                    }

                    return new InterpolationParameters(interpolationParams.StartTime, interpolationParams.EndTime,
                        interpolationParams.NumPoints, interpolationParams.IntervalDelta,
                        interpolationParams.InferZeroes);
                }
            }

            public IEnumerable<ICandidatePeakGroup> CandidatePeakGroups
            {
                get
                {
                    var peaks = Cache._chromatogramCache.ReadPeaks(Cache._stream, ChromGroupHeaderInfo);
                    if (peaks == null)
                    {
                        yield break;
                    }

                    var scores = Cache._chromatogramCache.ReadScores(Cache._stream, ChromGroupHeaderInfo);
                    int scoreCount = Cache._chromatogramCache.ScoreTypesCount;
                    for (int iPeakGroup = 0; iPeakGroup < ChromGroupHeaderInfo.NumPeaks; iPeakGroup++)
                    {
                        var peakGroupPeaks = Enumerable
                            .Range(0, ChromGroupHeaderInfo.NumTransitions)
                            .Select(i => peaks[iPeakGroup + i * ChromGroupHeaderInfo.NumPeaks]).ToList();
                        var peakGroupScores = Enumerable.Range(0, scoreCount)
                            .Select(i => scores[iPeakGroup * scoreCount + i]).ToList();

                        yield return new CandidatePeakGroup(peakGroupPeaks,
                            iPeakGroup == ChromGroupHeaderInfo.MaxPeakIndex, peakGroupScores, Cache.ScoreNameToIndex);

                    }
                }
            }
        }

        class ExtractedChromatogram : IChromatogram
        {
            public ExtractedChromatogram(ExtractedChromatogramGroup group, int transitionIndex)
            {
                Group = group;
                Cache = Group.Cache;
                TransitionIndex = transitionIndex;
                ChromTransition = Cache._chromatogramCache
                    .GetTransition(group.ChromGroupHeaderInfo.StartTransitionIndex + transitionIndex);
            }

            public ExtractedChromatogramGroup Group { get; }
            public int TransitionIndex { get; }
            public ChromTransition ChromTransition { get; }
            public LegacyChromatogramCache Cache { get; }

            public TimeIntensities TimeIntensities
            {
                get
                {
                    return Group.GetTimeIntensitiesGroup().TransitionTimeIntensities[TransitionIndex];
                }
            }

            public double ProductMz => ChromTransition.Product;

            public double ExtractionWidth => ChromTransition.ExtractionWidth;

            public double? IonMobilityValue => ZeroToNull(ChromTransition.IonMobilityValue);

            public double? IonMobilityExtractionWidth => ZeroToNull(ChromTransition.IonMobilityExtractionWidth);

            public int NumPoints => TimeIntensities.NumPoints;

            public IList<float> RetentionTimes => TimeIntensities.Times;

            public IList<float> Intensities => TimeIntensities.Intensities;

            public IList<float> MassErrors => TimeIntensities.MassErrors;

            public IList<string> SpectrumIdentifiers
            {
                get
                {
                    var msDataFileScanIds = Group.MsDataSourceFile.MsDataFileScanIds;
                    if (TimeIntensities.ScanIds == null || msDataFileScanIds == null)
                    {
                        return null;
                    }

                    return TimeIntensities.ScanIds.Select(id => msDataFileScanIds.GetMsDataFileSpectrumId(id)).ToList();
                }
            }

        }

        class CandidatePeakGroup : ICandidatePeakGroup
        {
            private IList<ChromPeak> _chromPeaks;
            private IDictionary<string, int> _scoreNameToIndex;
            private IList<float> _scores;
            private IList<ICandidatePeak> _candidatePeaks;

            public CandidatePeakGroup(IList<ChromPeak> chromPeaks, bool isBestPeak, IList<float> scores, IDictionary<string, int> scoreNameToIndex)
            {
                IsBestPeak = isBestPeak;
                _chromPeaks = chromPeaks;
                _scores = scores;
                _scoreNameToIndex = scoreNameToIndex;
                _candidatePeaks = new List<ICandidatePeak>();
                PeakIdentified bestIdentified = PeakIdentified.False;
                foreach (var chromPeak in chromPeaks)
                {
                    if (chromPeak.IsEmpty)
                    {
                        _candidatePeaks.Add(null);
                        continue;
                    }
                    _candidatePeaks.Add(new CandidatePeak(chromPeak));
                    switch (chromPeak.Identified)
                    {
                        case PeakIdentification.TRUE:
                            bestIdentified = PeakIdentified.True;
                            break;
                        case PeakIdentification.ALIGNED:
                            bestIdentified = (PeakIdentified) Math.Max((int) bestIdentified, (int) PeakIdentified.Aligned);
                            break;
                    }
                }

                Identified = bestIdentified;
            }
            public double? GetScore(string name)
            {
                if (!_scoreNameToIndex.TryGetValue(name, out int index))
                {
                    return null;
                }

                if (_scores == null || index >= _scores.Count)
                {
                    return null;
                }

                return _scores[index];
            }

            public bool IsBestPeak
            {
                get;
            }

            public IList<ICandidatePeak> CandidatePeaks
            {
                get
                {

                    return _candidatePeaks;
                }
            }

            public PeakIdentified Identified {get; }
        }

        private class CandidatePeak : ICandidatePeak
        {
            private ChromPeak _chromPeak;
            public CandidatePeak(ChromPeak chromPeak)
            {
                _chromPeak = chromPeak;
            }

            public double StartTime => _chromPeak.StartTime;

            public double EndTime => _chromPeak.EndTime;

            public double Area => _chromPeak.Area;

            public double BackgroundArea => _chromPeak.BackgroundArea;

            public double Height => _chromPeak.Height;

            public double FullWidthAtHalfMax => _chromPeak.Fwhm;

            public int? PointsAcross => _chromPeak.PointsAcross;

            public bool DegenerateFwhm => _chromPeak.IsFwhmDegenerate;

            public bool ForcedIntegration => _chromPeak.IsForcedIntegration;

            public bool? Truncated => _chromPeak.IsTruncated;

            public double? MassError => _chromPeak.MassError;
        }

        static double? ZeroToNull(double value)
        {
            if (value == 0)
            {
                return null;
            }

            return value;
        }

        public IEnumerable<string> ScoreNames
        {
            get
            {
                return _chromatogramCache.ScoreTypes.Select(type => type.FullName);
            }
        }
    }
}
