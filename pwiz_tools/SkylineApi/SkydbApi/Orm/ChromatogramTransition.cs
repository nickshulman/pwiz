using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false)]
    public class TransitionChromatogram : Entity<TransitionChromatogram>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public ChromatogramGroup ChromatogramGroup { get; set; }
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
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
