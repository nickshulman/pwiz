using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.Results;
using SkylineApi;
namespace pwiz.Skyline.Model.Skydb
{
    public class ChromatogramGroupAdapter : IChromGroupHeaderInfo
    {
        private IChromatogramGroupData _groupData;
        private ImmutableList<ICandidatePeakGroup> _peakGroups;
        private ImmutableList<IChromatogram> _chromatograms;
        public ChromatogramGroupAdapter(ChromatogramCacheAdapter chromatogramCache, int fileIndex, IChromatogramGroup group)
        {
            ChromatogramCache = chromatogramCache;
            FileIndex = fileIndex;
            ExtractedDataFile = chromatogramCache.ExtractedDataFiles[fileIndex];
            ChromatogramGroup = group;
        }

        public ChromatogramCacheAdapter ChromatogramCache { get; }
        public int FileIndex { get; }
        public string TextId
        {
            get { return ChromatogramGroup.TextId; }
        }

        [Browsable(false)]
        public IChromatogramGroupData ChromatogramGroupData
        {
            get
            {
                if (_groupData == null)
                {
                    _groupData = ChromatogramGroup.Data;
                }

                return _groupData;
            }
        }

        public IExtractedDataFile ExtractedDataFile { get; }
        public IChromatogramGroup ChromatogramGroup { get; }

        public ImmutableList<IChromatogram> Chromatograms
        {
            get
            {
                lock (this)
                {
                    if (_chromatograms == null)
                    {
                        _chromatograms = ImmutableList.ValueOf(ChromatogramGroup.Chromatograms);
                    }

                    return _chromatograms;
                }
            }
        }

        [Browsable(false)]
        public ImmutableList<ICandidatePeakGroup> CandidatePeakGroups
        {
            get
            {
                if (_peakGroups == null)
                {
                    _peakGroups = ImmutableList.ValueOf(ChromatogramGroupData.CandidatePeakGroups);
                }

                return _peakGroups;
            }
        }

        public int GetMaxPeakIndex()
        {
            for (int i = 0; i < CandidatePeakGroups.Count; i++)
            {
                if (CandidatePeakGroups[i].IsBestPeak)
                {
                    return i;
                }
            }

            return -1;
        }

        public int NumTransitions => Chromatograms.Count;

        public int NumPeaks => CandidatePeakGroups.Count;

        public int MaxPeakIndex => GetMaxPeakIndex();

        public float? StartTime => (float?)ChromatogramGroup.StartTime;

        public float? EndTime => (float?) ChromatogramGroup.EndTime;

        public SignedMz Precursor => new SignedMz(ChromatogramGroup.PrecursorMz);

        public float? CollisionalCrossSection => (float?) ChromatogramGroup.CollisionalCrossSection;

        public ChromExtractor Extractor
        {
            get
            {
                // TODO
                return ChromExtractor.summed;
            }
        }

        public bool NegativeCharge
        {
            get
            {
                return ChromatogramGroup.PrecursorMz < 0;
            }
        }

        public bool IsNotIncludedTime(double retentionTime)
        {
            return StartTime.HasValue && EndTime.HasValue &&
                   (retentionTime < StartTime.Value || EndTime.Value < retentionTime);
        }

        public TimeIntensitiesGroup ReadTimeIntensities()
        {
            var chromatogramGroup = ChromatogramGroup;
            var spectrumIdMap = ChromatogramCache.SpectrumIdMaps[FileIndex];
            var chromatograms = chromatogramGroup.Chromatograms.ToList();
            var chromatogramGroupData = chromatogramGroup.Data;
            var transitionTimeIntensitiesList = chromatogramGroupData.ChromatogramDatas
                .Select(chrom => GetTimeIntensities(chrom, spectrumIdMap)).ToList();
            var interpolationParams = chromatogramGroupData.InterpolationParameters;
            if (interpolationParams == null)
            {
                // TODO: chromSources
                var chromSources = chromatograms.Select(chrom => ChromSource.unknown);
                return new InterpolatedTimeIntensities(transitionTimeIntensitiesList, chromSources);
            }

            return new RawTimeIntensities(transitionTimeIntensitiesList,
                new InterpolationParams(interpolationParams.StartTime, interpolationParams.EndTime,
                    interpolationParams.NumberOfPoints, interpolationParams.IntervalDelta));
        }

        private TimeIntensities GetTimeIntensities(IChromatogramData chromatogram, SpectrumIdMap spectrumIdMap)
        {
            ImmutableList<int> scanIds =
                ImmutableList.ValueOf(chromatogram.SpectrumIdentifiers?.Select(spectrumIdMap.GetScanId));
            return new TimeIntensities(chromatogram.RetentionTimes, chromatogram.Intensities,
                chromatogram.MassErrors, scanIds);
        }

        public IList<ChromPeak> ReadPeaks()
        {
            var peakGroups = ChromatogramGroupData.CandidatePeakGroups.ToList();
            int peakCount = peakGroups.Count;
            int transitionCount = NumTransitions;
            var chromPeaks = new ChromPeak[peakCount * transitionCount];
            for (int iPeak = 0; iPeak < peakCount; iPeak++)
            {
                var peakGroupPeaks = peakGroups[iPeak].CandidatePeaks;
                for (int iTransition = 0; iTransition < transitionCount; iTransition++)
                {
                    chromPeaks[iTransition * peakCount + iPeak] = new ChromPeak(peakGroupPeaks[iTransition]);
                }
            }

            return chromPeaks;
        }

        public IList<float> ReadScores()
        {
            var result = new List<float>();
            foreach (var peakGroup in ChromatogramGroupData.CandidatePeakGroups)
            {
                foreach (var scoreType in ChromatogramCache.ScoreTypes.AsCalculatorTypes())
                {
                    result.Add((float?) peakGroup.GetScore(scoreType.FullName) ?? float.NaN);
                }
            }

            return result;
        }

        public IEnumerable<ChromTransition> GetTransitions()
        {
            // TODO: ChromSource
            return ChromatogramGroup.Chromatograms.Select(chrom => new ChromTransition(chrom.ProductMz,
                (float)chrom.ExtractionWidth, (float)chrom.IonMobilityValue.GetValueOrDefault(), (float)chrom.IonMobilityExtractionWidth.GetValueOrDefault(), ChromSource.unknown));
        }

        public ChromGroupHeaderInfo.FlagValues Flags {
            get
            {
                // TODO
                return 0;
            }
        }

        public bool HasRawTimes()
        {
            return 0 != (Flags & ChromGroupHeaderInfo.FlagValues.raw_chromatograms);
        }

        public eIonMobilityUnits IonMobilityUnits
        {
            get
            {
                // TODO
                return eIonMobilityUnits.unknown;
            }
        }
    }
}
