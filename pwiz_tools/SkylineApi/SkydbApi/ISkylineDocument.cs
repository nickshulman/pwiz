using System.Collections.Generic;

namespace SkylineApi
{
    public interface ISkylineDocument
    {
        IEnumerable<IExtractedDataFile> ExtractedDataFiles { get; }
    }
}
