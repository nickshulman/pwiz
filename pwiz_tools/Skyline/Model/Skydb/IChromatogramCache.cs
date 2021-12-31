using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Skydb
{
    public interface IChromatogramCache : IDisposable
    {
        TimeIntensitiesGroup ReadTimeIntensities(ChromGroupHeaderInfo header);
        IList<float> ReadScores(ChromGroupHeaderInfo header);
        IList<ChromPeak> ReadPeaks(ChromGroupHeaderInfo header);
        IReadOnlyList<ChromGroupHeaderInfo> ChromGroupHeaderInfos { get; }
        IList<ChromCachedFile> CachedFiles { get; }
        IEnumerable<MsDataFileUri> CachedFilePaths { get; }

        void ReadDataForAll(IList<ChromGroupHeaderInfo> chromGroupHeaderInfos, IList<ChromPeak>[] peaks,
            IList<float>[] scores);

        string GetTextId(ChromGroupHeaderInfo chromGroupHeaderInfo);
        IEnumerable<Type> ScoreTypes { get; }
        CacheFormatVersion Version { get; }
        bool IsReadStreamModified { get; }
        string ReadStreamModifiedExplanation { get; }
        string CachePath { get; }
        MsDataFileScanIds LoadMSDataFileScanIds(int fileIndex);
        IEnumerable<ChromTransition> GetTransitions(ChromGroupHeaderInfo chromGroupHeaderInfo);

    }
}
