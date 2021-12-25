using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class SpectrumList : Entity<SpectrumList>
    {
        [Property]
        public virtual int SpectrumCount { get; set; }
        [Property]
        public virtual byte[] SpectrumIndexData { get; set; }
        [Property]
        public virtual byte[] RetentionTimeData { get; set; }
    }
}
