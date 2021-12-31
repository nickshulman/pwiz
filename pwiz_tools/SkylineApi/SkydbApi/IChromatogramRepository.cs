using System.Collections.Generic;

namespace SkylineApi
{
    public interface IChromatogramRepository
    {
        IEnumerable<IExtractedDataFile> ExtractedDataFiles { get; }
    }
}
