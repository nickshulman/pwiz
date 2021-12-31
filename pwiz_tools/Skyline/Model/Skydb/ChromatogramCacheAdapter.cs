using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;
using SkylineApi;

namespace pwiz.Skyline.Model.Skydb
{
    public class ChromatogramCacheAdapter : AbstractChromatogramCache
    {
        private ImmutableList<ChromGroupHeaderInfo> _chromGroupHeaderInfos;
        private ImmutableList<ChromCachedFile> _chromCachedFiles;
        public ChromatogramCacheAdapter(string cachePath, IEnumerable<IExtractedDataFile> extractedDataFiles)
        {
            CachePath = cachePath;
            ExtractedDataFiles = ImmutableList.ValueOf(extractedDataFiles);
            ChromatogramGroups =
                ImmutableList.ValueOf(ExtractedDataFiles.Select(file =>
                    ImmutableList.ValueOf(file.ChromatogramGroups)));
            SpectrumIdMaps = ImmutableList.ValueOf(ExtractedDataFiles.Select(file => new SpectrumIdMap()));
            _chromGroupHeaderInfos = ImmutableList.ValueOf(GetChromGroupHeaderInfos()
                .OrderBy(header => Tuple.Create(header.Precursor, header.FileIndex)));
            _chromCachedFiles = ImmutableList.ValueOf(ExtractedDataFiles.Select(ToChromCachedFile));
            var scoreTypes = new List<Type>();
            foreach (var scoreTypeName in ExtractedDataFiles.SelectMany(file => file.ScoreNames).Distinct())
            {
                var type = Type.GetType(scoreTypeName);
                Assume.IsNotNull(type, scoreTypeName);
                scoreTypes.Add(type);
            }
            Init(scoreTypes);
        }

        public ImmutableList<IExtractedDataFile> ExtractedDataFiles { get; }
        public ImmutableList<ImmutableList<IChromatogramGroup>> ChromatogramGroups { get; }
        public ImmutableList<SpectrumIdMap> SpectrumIdMaps { get; }

        public ChromGroupHeaderInfo MakeChromGroupHeaderInfo(int fileIndex, int groupIndex)
        {
            var chromatogramGroup = ChromatogramGroups[fileIndex][groupIndex];
            ChromGroupHeaderInfo.FlagValues flagValues = 0;
            var chromatograms = chromatogramGroup.Chromatograms.ToList();
            // var candidatePeakGroups = chromatogramGroup.CandidatePeakGroups.ToList();
            // int maxPeakIndex = -1;
            //     // Enumerable.Range(0, candidatePeakGroups.Count).Cast<int?>()
            //     // .FirstOrDefault(i => candidatePeakGroups[i.Value].IsBestPeak) ?? -1;

            // TODO: IonMobilityUnits
            return new ChromGroupHeaderInfo(new SignedMz(chromatogramGroup.PrecursorMz), fileIndex, chromatograms.Count, 
                groupIndex,
                0, 0, 0, -1, 0, 0, 0, 0, flagValues, 0, 0,
                (float?)chromatogramGroup.StartTime, (float?)chromatogramGroup.EndTime,
                chromatogramGroup.CollisionalCrossSection, eIonMobilityUnits.unknown);
        }

        private IChromatogramGroup GetChromatogramGroup(ChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            return ChromatogramGroups[chromGroupHeaderInfo.FileIndex][chromGroupHeaderInfo.StartTransitionIndex];
        }

        public override TimeIntensitiesGroup ReadTimeIntensities(ChromGroupHeaderInfo header)
        {
            var chromatogramGroup = GetChromatogramGroup(header);
            var spectrumIdMap = SpectrumIdMaps[header.FileIndex];
            var chromatograms = chromatogramGroup.Chromatograms.ToList();
            var chromatogramGroupData = chromatogramGroup.Data;
            var transitionTimeIntensitiesList = chromatogramGroupData.ChromatogramDatas.Select(chrom => GetTimeIntensities(chrom, spectrumIdMap)).ToList();
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

        public override IList<float> ReadScores(ChromGroupHeaderInfo header)
        {
            //TODO
            return null;
        }

        private TimeIntensities GetTimeIntensities(IChromatogramData chromatogram, SpectrumIdMap spectrumIdMap)
        {
            ImmutableList<int> scanIds =
                ImmutableList.ValueOf(chromatogram.SpectrumIdentifiers?.Select(spectrumIdMap.GetScanId));
            return new TimeIntensities(chromatogram.RetentionTimes, chromatogram.Intensities,
                chromatogram.MassErrors, scanIds);
        }

        public override IList<ChromPeak> ReadPeaks(ChromGroupHeaderInfo header)
        {
            var chromatogramGroup = GetChromatogramGroup(header);
            var peakGroups = chromatogramGroup.Data.CandidatePeakGroups.ToList();
            int peakCount = peakGroups.Count;
            int transitionCount = header.NumTransitions;
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


        public override IReadOnlyList<ChromGroupHeaderInfo> ChromGroupHeaderInfos
        {
            get
            {
                return _chromGroupHeaderInfos;
            }
        }

        public override IList<ChromCachedFile> CachedFiles
        {
            get { return _chromCachedFiles; }
        }

        public override string GetTextId(ChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            return GetChromatogramGroup(chromGroupHeaderInfo).TextId;
        }

        public override CacheFormatVersion Version {
            get { return CacheFormatVersion.CURRENT; }
        }

        public override IMsDataFileScanIds LoadMSDataFileScanIds(int fileIndex)
        {
            return SpectrumIdMaps[fileIndex];
        }

        public override IEnumerable<ChromTransition> GetTransitions(ChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            var chromatogramGroup = GetChromatogramGroup(chromGroupHeaderInfo);
            // TODO: ChromSource
            return chromatogramGroup.Chromatograms.Select(chrom => new ChromTransition(chrom.ProductMz,
                (float) chrom.ExtractionWidth, (float) chrom.IonMobilityValue.GetValueOrDefault(), (float)chrom.IonMobilityExtractionWidth.GetValueOrDefault(), ChromSource.unknown));
        }

        private IEnumerable<ChromGroupHeaderInfo> GetChromGroupHeaderInfos()
        {
            for (int fileIndex = 0; fileIndex < ExtractedDataFiles.Count; fileIndex++)
            {
                for (int groupIndex = 0; groupIndex < ChromatogramGroups[fileIndex].Count; groupIndex++)
                {
                    yield return MakeChromGroupHeaderInfo(fileIndex, groupIndex);
                }
            }
        }

        public static ChromCachedFile ToChromCachedFile(IExtractedDataFile extractedDataFile)
        {
            ChromCachedFile.FlagValues flagValues = 0;
            if (extractedDataFile.HasCombinedIonMobility)
            {
                flagValues |= ChromCachedFile.FlagValues.has_combined_ion_mobility;
            }

            if (extractedDataFile.Ms1Centroid)
            {
                flagValues |= ChromCachedFile.FlagValues.used_ms1_centroids;
            }

            if (extractedDataFile.Ms2Centroid)
            {
                flagValues |= ChromCachedFile.FlagValues.used_ms2_centroids;
            }

            // TODO: ion mobility units, msInstrumentConfigInfo
            return new ChromCachedFile(MsDataFileUri.Parse(extractedDataFile.SourceFilePath), flagValues,
                extractedDataFile.LastWriteTime.GetValueOrDefault(), extractedDataFile.RunStartTime,
                (float) extractedDataFile.MaxRetentionTime.GetValueOrDefault(),
                (float) extractedDataFile.MaxIntensity.GetValueOrDefault(),
                eIonMobilityUnits.unknown,
                extractedDataFile.SampleId,
                extractedDataFile.InstrumentSerialNumber,
                Array.Empty<MsInstrumentConfigInfo>());
        }
    }
}
