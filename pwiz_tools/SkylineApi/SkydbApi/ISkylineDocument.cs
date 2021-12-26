using System.Collections.Generic;

namespace SkylineApi
{
    public interface ISkylineDocument
    {
        IEnumerable<IExtractedChromatograms> ExtractedChromatogramData { get; }
    }
}
