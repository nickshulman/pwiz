using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false)]
    public class MsDataFile : Entity<MsDataFile>
    {
        [Property]
        public virtual string FilePath { get; set; }
    }
}
