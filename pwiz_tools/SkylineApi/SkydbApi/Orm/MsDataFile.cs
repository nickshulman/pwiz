using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class MsDataFile : Entity<MsDataFile>
    {
        [Property]
        public virtual string FilePath { get; set; }
    }
}
