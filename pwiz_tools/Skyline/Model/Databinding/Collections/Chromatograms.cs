using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class Chromatograms : SkylineObjectList<Tuple<string, ChromGroupHeaderInfo>, ChromatogramGroup>
    {
        public Chromatograms(SkylineDataSchema dataSchema) : base(dataSchema)
        {
        }

        protected override ChromatogramGroup ConstructItem(Tuple<string, ChromGroupHeaderInfo> key)
        {
            var measuredResults = SrmDocument.Settings.MeasuredResults;
            if (measuredResults == null)
            {
                return null;
            }
            var cache = measuredResults.GetChromatogramCache(key.Item1);
            if (cache == null)
            {
                return null;
            }
            return new ChromatogramGroup(DataSchema, cache, cache.LoadChromatogramInfo(key.Item2));
        }

        protected override IEnumerable<Tuple<string, ChromGroupHeaderInfo>> ListKeys()
        {
            var measuredResults = SrmDocument.Settings.MeasuredResults;
            if (measuredResults == null)
            {
                return new Tuple<string, ChromGroupHeaderInfo>[0];
            }
            return measuredResults.CachePaths.Select(path =>
                measuredResults.GetChromatogramCache(path)).SelectMany(cache =>
                cache.ChromGroupHeaderInfos.Select(header => Tuple.Create(cache.CachePath, header)));
        }
    }
}
