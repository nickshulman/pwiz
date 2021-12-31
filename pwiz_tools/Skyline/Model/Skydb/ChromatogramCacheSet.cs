using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Results;
using SkylineApi;

namespace pwiz.Skyline.Model.Skydb
{
    public class ChromatogramCacheSet : Immutable, IChromatogramRepository
    {
        public ChromatogramCacheSet(ChromatogramCache cacheFinal)
        {
            CacheFinal = cacheFinal;
        }

        public ChromatogramCacheSet(IEnumerable<ChromatogramCache> partialCaches)
        {
            ListPartialCaches = ImmutableList.ValueOf(partialCaches);
        }

        public ChromatogramCache CacheFinal { get; private set; }
        public ChromatogramCache CacheRecalc { get; private set; }
        public ImmutableList<ChromatogramCache> ListPartialCaches { get; private set; }
        public ImmutableList<string> ListSharedCachePaths { get; private set; }

        public IEnumerable<IExtractedDataFile> ExtractedDataFiles
        {
            get
            {
                return Caches.SelectMany(cache => new LegacyChromatogramCache(cache).ExtractedDataFiles);
            }
        }

        public IEnumerable<ChromatogramCache> Caches
        {
            get
            {
                if (CacheFinal != null)
                    yield return CacheFinal;
                if (ListPartialCaches != null)
                {
                    foreach (var cache in ListPartialCaches)
                        yield return cache;
                }
            }
        }
        /// <summary>
        /// List of caches with _cacheRecalc as backstop during reloading
        /// </summary>
        public IEnumerable<ChromatogramCache> CachesEx
        {
            get
            {
                foreach (var cache in Caches)
                    yield return cache;
                if (CacheRecalc != null)
                    yield return CacheRecalc;
            }
        }
    }
}
