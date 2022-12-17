using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Util;
using SkylineApi;

namespace pwiz.Skyline.Model.Skydb
{
    public class ChromatogramCacheAdapter : AbstractChromatogramCache
    {
        private List<Tuple<IChromatogramGroup, int>> _allChromatogramGroups;
        private ImmutableList<ChromCachedFile> _chromCachedFiles;
        public ChromatogramCacheAdapter(string cachePath, IEnumerable<IExtractedDataFile> extractedDataFiles)
        {
            CachePath = cachePath;
            ExtractedDataFiles = ImmutableList.ValueOf(extractedDataFiles);
            ChromatogramGroups =
                ImmutableList.ValueOf(ExtractedDataFiles.Select(file =>
                    ImmutableList.ValueOf(file.ChromatogramGroups)));
            SpectrumIdMaps = ImmutableList.ValueOf(ExtractedDataFiles.Select(file => new SpectrumIdMap()));
            _allChromatogramGroups = new List<Tuple<IChromatogramGroup, int>>();
            for (int iFile = 0; iFile < ExtractedDataFiles.Count; iFile++)
            {
                _allChromatogramGroups.AddRange(ExtractedDataFiles[iFile].ChromatogramGroups
                    .Select(group => Tuple.Create(group, iFile)));
            }
            _allChromatogramGroups.Sort((a,b)=>a.Item1.PrecursorMz.CompareTo(b.Item1.PrecursorMz));
            _chromCachedFiles = ImmutableList.ValueOf(ExtractedDataFiles.Select(ToChromCachedFile));
            var scoreTypes = new List<Type>();
            foreach (var scoreTypeName in ExtractedDataFiles.SelectMany(file => file.ScoreNames).Distinct())
            {
                var type = Type.GetType(scoreTypeName);
                Assume.IsNotNull(type, scoreTypeName);
                scoreTypes.Add(type);
            }
            Init(FeatureNames.FromScoreTypes(scoreTypes));
        }

        public ImmutableList<IExtractedDataFile> ExtractedDataFiles { get; }
        public ImmutableList<ImmutableList<IChromatogramGroup>> ChromatogramGroups { get; }
        public ImmutableList<SpectrumIdMap> SpectrumIdMaps { get; }

        public override TimeIntensitiesGroup ReadTimeIntensities(IChromGroupHeaderInfo header)
        {
            return ((ChromatogramGroupAdapter) header).ReadTimeIntensities();
        }

        public override IList<float> ReadScores(IChromGroupHeaderInfo header)
        {
            return ((ChromatogramGroupAdapter) header).ReadScores();
        }

        public override IList<ChromPeak> ReadPeaks(IChromGroupHeaderInfo header)
        {
            return ((ChromatogramGroupAdapter) header).ReadPeaks();
        }


        public override IList<IChromGroupHeaderInfo> ChromGroupHeaderInfos
        {
            get
            {
                return ReadOnlyList.Create<IChromGroupHeaderInfo>(_allChromatogramGroups.Count,
                    index => new ChromatogramGroupAdapter(this, _allChromatogramGroups[index].Item2, _allChromatogramGroups[index].Item1));
            }
        }

        public override IList<ChromCachedFile> CachedFiles
        {
            get { return _chromCachedFiles; }
        }

        public override string GetTextId(IChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            return ((ChromatogramGroupAdapter) chromGroupHeaderInfo).TextId;
        }

        public override CacheFormatVersion Version {
            get { return CacheFormatVersion.CURRENT; }
        }

        public override IMsDataFileScanIds LoadMSDataFileScanIds(int fileIndex)
        {
            return SpectrumIdMaps[fileIndex];
        }

        public override IEnumerable<ChromTransition> GetTransitions(IChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            return ((ChromatogramGroupAdapter) chromGroupHeaderInfo).GetTransitions();
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
                null,
                (float) extractedDataFile.MaxRetentionTime.GetValueOrDefault(),
                (float) extractedDataFile.MaxIntensity.GetValueOrDefault(),
                0, 0, null,
                eIonMobilityUnits.unknown,
                extractedDataFile.SampleId,
                extractedDataFile.InstrumentSerialNumber,
                Array.Empty<MsInstrumentConfigInfo>());
        }
    }
}
