using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class Chromatogram : Entity<Chromatogram>
    {
        [ManyToOne]
        public ChromatogramGroup ChromatogramGroup { get; set; }
        [ManyToOne]
        public ChromatogramData ChromatogramData { get; set; }
        [Property]
        public double ProductMz { get; set; }
        [Property]
        public double ExtractionWidth { get; set; }
        [Property]
        public double? IonMobilityValue { get; set; }
        [Property]
        public double? IonMobilityExtractionWidth { get; set; }
        [Property]
        public int Source { get; set; }
    }
}
