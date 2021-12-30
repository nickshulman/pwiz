using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class Chromatogram : Entity<Chromatogram>
    {
        [ManyToOne(ClassType = typeof(ChromatogramGroup))]
        public long ChromatogramGroup { get; set; }
        [ManyToOne(ClassType = typeof(ChromatogramData))]
        public long ChromatogramData { get; set; }
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
