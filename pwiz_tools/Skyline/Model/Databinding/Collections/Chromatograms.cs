using System.Collections;
using pwiz.Skyline.Model.Databinding.Entities;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class Chromatograms : SkylineObjectList<ChromatogramGroup>
    {
        public Chromatograms(SkylineDataSchema dataSchema) : base(dataSchema)
        {
        }

        public override IEnumerable GetItems()
        {
            var measuredResults = SrmDocument.Settings.MeasuredResults;
            if (measuredResults == null)
            {
                yield break;
            }

            foreach (var cachePath in measuredResults.CachePaths)
            {
                var cache = measuredResults.GetChromatogramCache(cachePath);
                if (cache == null)
                {
                    continue;
                }

                foreach (var headerInfo in cache.ChromGroupHeaderInfos)
                {
                    yield return new ChromatogramGroup(DataSchema, cache, cache.LoadChromatogramInfo(headerInfo));
                }
            }
        }
    }
}
