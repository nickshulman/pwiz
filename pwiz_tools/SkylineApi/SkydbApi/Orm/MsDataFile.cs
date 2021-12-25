using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class MsDataFile : Entity
    {
        [Column]
        public virtual string FilePath { get; set; }
    }
}
