using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{

    [Class(Lazy = false, Table = nameof(MsDataFile))]
    public class MsDataFile : Entity
    {
        [Property]
        public string FilePath { get; set; }
    }
}
