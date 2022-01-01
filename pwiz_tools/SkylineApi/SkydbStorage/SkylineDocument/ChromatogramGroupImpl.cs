using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ChromatogramGroupImpl : IChromatogramGroup
    {
        private ChromatogramGroup _chromatogramGroup;
        private IList<ChromatogramImpl> _chromatograms;
        public ChromatogramGroupImpl(ExtractedDataFileImpl dataFile, Precursor precursor, long id)
        {
            DataFile = dataFile;
            Precursor = precursor;
            Id = id;
        }

        public long Id { get; }

        public SkylineDocumentImpl Document
        {
            get { return DataFile.Document; }
        }

        public Precursor Precursor { get; }

        public ExtractedDataFileImpl DataFile { get; }

        public ChromatogramGroup ChromatogramGroup
        {
            get
            {
                if (_chromatogramGroup == null)
                {
                    _chromatogramGroup = Precursor.GetChromatogramGroup(Id);
                }

                return _chromatogramGroup;
            }
        }

        public double PrecursorMz => Precursor.PrecursorMz;

        public string TextId => Precursor.TextId;

        public double? StartTime => ChromatogramGroup.StartTime;

        public double? EndTime => ChromatogramGroup.EndTime;

        public double? CollisionalCrossSection => ChromatogramGroup.CollisionalCrossSection;

        public IList<ChromatogramImpl> ChromatogramImpls
        {
            get
            {
                lock (this)
                {
                    if (_chromatograms == null)
                    {
                        _chromatograms = Precursor.GetChromatograms(Id)
                            .Select(chrom => new ChromatogramImpl(this, chrom)).ToList();
                    }

                    return _chromatograms;
                }
            }
        }

        public IEnumerable<IChromatogram> Chromatograms
        {
            get { return ChromatogramImpls; }
        }

        public IChromatogramGroupData Data => new ChromatogramGroupDataImpl(this);
    }
}
