using System.Collections.Generic;
using System.Linq;
using System.Text;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class ChromatogramGroup : RootSkylineObject
    {
        private readonly ChromatogramCache _chromatogramCache;
        private readonly ChromatogramGroupInfo _chromatogramGroupInfo;
        private readonly CachedValue<IList<TransitionChromatogram>> _transitionChromatograms;
        public ChromatogramGroup(SkylineDataSchema dataSchema, ChromatogramCache chromatogramCache, ChromatogramGroupInfo chromatogramGroupInfo)
            : base(dataSchema)
        {
            _chromatogramCache = chromatogramCache;
            _chromatogramGroupInfo = chromatogramGroupInfo;
            _transitionChromatograms = CachedValue.Create(dataSchema, GetTransitionChromatograms);
        }

        public string CachePath { get { return _chromatogramCache.CachePath; } }
        public double PrecursorMz
        {
            get
            {
                return _chromatogramGroupInfo.PrecursorMz.RawValue;
            }
        }

        public string RawFileName
        {
            get { return _chromatogramGroupInfo.FilePath.ToString(); }
        }

        public string ChromatogramTextId
        {
            get
            {
                var header = _chromatogramGroupInfo.Header;
                if (header.TextIdLen == 0)
                {
                    return null;
                }

                var bytes = _chromatogramCache.GetTextIdBytes(header.TextIdIndex, header.TextIdLen);
                if (bytes == null)
                {
                    return null;
                }
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public TimeIntensitiesGroup ReadTimeIntensitiesGroup()
        {
            var chromatogramGroupInfo = DataSchema.ChromDataCache.LoadChromatogramPoints(_chromatogramCache, _chromatogramGroupInfo);
            if (chromatogramGroupInfo == null)
            {
                return null;
            }
            return chromatogramGroupInfo.TimeIntensitiesGroup;
        }

        private IList<TransitionChromatogram> GetTransitionChromatograms()
        {
            return Enumerable.Range(0, _chromatogramGroupInfo.NumTransitions)
                .Select(i => new TransitionChromatogram(this, new ChromatogramInfo(_chromatogramGroupInfo, i)))
                .ToArray();
        }

        [OneToMany(ForeignKey = "ChromatogramGroup")]
        public IList<TransitionChromatogram> TransitionChromatograms
        {
            get { return _transitionChromatograms.Value; }
        }

        public bool IsBasePeak { get { return _chromatogramGroupInfo.Header.Extractor == ChromExtractor.base_peak; } }
        public float? StartTime { get { return _chromatogramGroupInfo.Header.StartTime; } }
        public float? EndTime { get { return _chromatogramGroupInfo.Header.EndTime; } }

        public MsDataFileScanIds ReadMsDataFileScanIds()
        {
            return DataSchema.ChromDataCache.GetScanIds(DataSchema.Document,
                _chromatogramGroupInfo.FilePath);
        }
    }
}
