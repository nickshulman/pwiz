using System.Reflection;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false)]
    public class SpectrumList : Entity<SpectrumList>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public MsDataFile MsDataFile { get; set; }
        [Property]
        public int SpectrumCount { get; set; }
        [Property]
        public byte[] SpectrumIndexBlob { get; set; }
        [Property]
        public byte[] RetentionTimeBlob { get; set; }
    }
}
