using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    public class Entity
    {
        [Id(TypeType = typeof(long), Column = "Id", Name = "Id")]
        [Generator(Class = "identity")]
        public virtual long? Id { get; set; }
    }
}
