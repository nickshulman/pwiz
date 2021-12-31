using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbStorage.DataAccess.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ChromatogramGroupImpl : IChromatogramGroup
    {
        public ChromatogramGroupImpl(ExtractedDataFileImpl dataFile, ChromatogramGroup chromatogramGroup, IEnumerable<Chromatogram> chromatograms)
        {
            DataFile = dataFile;
            ChromatogramGroup = chromatogramGroup;
            Chromatograms = chromatograms.Select(chrom => new ChromatogramImpl(this, chrom)).ToList();
        }

        public SkylineDocumentImpl Document
        {
            get { return DataFile.Document; }
        }

        public ExtractedDataFileImpl DataFile { get; }
        public ChromatogramGroup ChromatogramGroup { get; }

        public double PrecursorMz => ChromatogramGroup.PrecursorMz;

        public string TextId => ChromatogramGroup.TextId;

        public double? StartTime => ChromatogramGroup.StartTime;

        public double? EndTime => ChromatogramGroup.EndTime;

        public double? CollisionalCrossSection => ChromatogramGroup.CollisionalCrossSection;

        public IEnumerable<IChromatogram> Chromatograms
        {
            get;
        }

        public InterpolationParameters InterpolationParameters => null;

        public IEnumerable<ICandidatePeakGroup> CandidatePeakGroups => new List<ICandidatePeakGroup>();
    }
}
