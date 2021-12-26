using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class ChromatogramData : Entity<ChromatogramData>
    {
        [ManyToOne]
        public virtual SpectrumList SpectrumList { get; set; }
        [Property]
        public virtual int PointCount { get; set; }
        [Property]
        public virtual byte[] IntensitiesBlob { get; set; }
        [Property]
        public virtual byte[] MassErrorsBlob { get; set; }
    }
}
