using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ChromatogramImpl : IChromatogram
    {
        public ChromatogramImpl(ChromatogramGroupImpl group, Chromatogram chromatogram)
        {
            Group = group;
            Chromatogram = chromatogram;
        }

        public SkylineDocumentImpl Document
        {
            get { return Group.Document; }
        }

        public ChromatogramGroupImpl Group { get; }
        public Chromatogram Chromatogram { get; }
        public double ProductMz => Chromatogram.ProductMz;

        public double ExtractionWidth => Chromatogram.ExtractionWidth;

        public double? IonMobilityValue => Chromatogram.IonMobilityValue;

        public double? IonMobilityExtractionWidth => Chromatogram.IonMobilityExtractionWidth;

    }
}
