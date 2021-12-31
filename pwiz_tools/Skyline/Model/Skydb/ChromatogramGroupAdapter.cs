using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.Results;
using SkylineApi;
#if false
namespace pwiz.Skyline.Model.Skydb
{
    public class ChromatogramGroupAdapter
    {
        public ChromatogramGroupAdapter(ChromatogramCacheAdapter chromatogramCache, int fileIndex, int chromatogramGroupIndex)
        {
            ChromatogramCache = chromatogramCache;
            FileIndex = fileIndex;
            GroupIndex = GroupIndex;
            Chromatograms = ImmutableList.ValueOf(chromatogramGroup.Chromatograms);
            CandidatePeakGroups = ImmutableList.ValueOf(ChromatogramGroup.CandidatePeakGroups);
        }

        public ChromatogramCacheAdapter ChromatogramCache { get; }
        public int FileIndex { get; }
        public int GroupIndex { get; }

        public IExtractedDataFile ExtractedDataFile { get; }
        public IChromatogramGroup ChromatogramGroup { get; }
        public ImmutableList<IChromatogram> Chromatograms { get; }
        public ImmutableList<ICandidatePeakGroup> CandidatePeakGroups { get; }

        public ChromGroupHeaderInfo ToChromGroupHeaderInfo()
        {
            ChromGroupHeaderInfo.FlagValues flagValues = 0;
            if (ChromatogramGroup.InterpolationParameters != null)
            {
                flagValues |= ChromGroupHeaderInfo.FlagValues.raw_chromatograms;
            }
            // TODO: IonMobilityUnits
            return new ChromGroupHeaderInfo(new SignedMz(ChromatogramGroup.PrecursorMz), 0, Chromatograms.Count, 0,
                CandidatePeakGroups.Count, 0, 0, GetMaxPeakIndex(), 0, 0, 0, 0, flagValues, 0, 0,
                (float?) ChromatogramGroup.StartTime, (float?) ChromatogramGroup.EndTime,
                ChromatogramGroup.CollisionalCrossSection, eIonMobilityUnits.unknown);
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

        private class ChromatogramCacheImpl : IChromatogramCache
        {
            private IDictionary<string, int> _spectrumIdToScanId = new Dictionary<string, int>();
            private IList<string> _scanIdToSpectrumId = new List<string>();
            private TimeIntensitiesGroup _timeIntensitiesGroup;
            public ChromatogramCacheImpl(ChromatogramGroupAdapter adapter)
            {
                Adapter = adapter;
            }

            public ChromatogramGroupAdapter Adapter { get; }

            public TimeIntensitiesGroup ReadTimeIntensities(ChromGroupHeaderInfo header)
            {
                return GetTimeIntensitiesGroup();
            }

            public IList<float> ReadScores(ChromGroupHeaderInfo header)
            {
                // TODO
                return null;
            }

            public IList<ChromPeak> ReadPeaks(ChromGroupHeaderInfo header)
            {
                var peakGroups = Adapter.ChromatogramGroup.CandidatePeakGroups.ToList();
                int peakCount = peakGroups.Count;
                int transitionCount = Adapter.Chromatograms.Count;
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

            private TimeIntensities GetTimeIntensities(IChromatogram chromatogram)
            {
                var spectrumIdentifiers = chromatogram.SpectrumIdentifiers;
                List<int> scanIds = null;
                if (spectrumIdentifiers != null)
                {
                    scanIds = new List<int>(spectrumIdentifiers.Count);
                    foreach (var spectrumIdentifier in spectrumIdentifiers)
                    {
                        int scanId;
                        if (!_spectrumIdToScanId.TryGetValue(spectrumIdentifier, out scanId))
                        {
                            scanId = _scanIdToSpectrumId.Count;
                            _spectrumIdToScanId.Add(spectrumIdentifier, scanId);
                            _scanIdToSpectrumId.Add(spectrumIdentifier);
                        }
                        scanIds.Add(scanId);
                    }
                }

                return new TimeIntensities(chromatogram.RetentionTimes, chromatogram.Intensities,
                    chromatogram.MassErrors, scanIds);
            }

            private TimeIntensitiesGroup GetTimeIntensitiesGroup()
            {
                if (_timeIntensitiesGroup != null)
                {
                    return _timeIntensitiesGroup;
                }

                var transitionTimeIntensitiesList = Adapter.Chromatograms.Select(chrom => GetTimeIntensities(chrom)).ToList();
                var interpolationParams = Adapter.ChromatogramGroup.InterpolationParameters;
                if (interpolationParams == null)
                {
                    // TODO: chromSources
                    var chromSources = Adapter.Chromatograms.Select(chrom => ChromSource.unknown);
                    return new InterpolatedTimeIntensities(transitionTimeIntensitiesList, chromSources);
                }

                return _timeIntensitiesGroup = new RawTimeIntensities(transitionTimeIntensitiesList,
                    new InterpolationParams(interpolationParams.StartTime, interpolationParams.EndTime,
                        interpolationParams.NumberOfPoints, interpolationParams.IntervalDelta));
            }
        }
    }
}
#endif