using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class ChromatogramData : Entity<ChromatogramData>
    {
        [ManyToOne(ClassType = typeof(SpectrumList))]
        public long SpectrumList { get; set; }
        [Property]
        public int PointCount { get; set; }
        [Property]
        public byte[] IntensitiesBlob { get; set; }
        [Property]
        public byte[] MassErrorsBlob { get; set; }
    }
}
