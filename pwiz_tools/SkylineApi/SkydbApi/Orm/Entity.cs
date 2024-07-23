using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    public class Entity
    {
        [Id(TypeType = typeof(int), Column = "Id", Name = "Id")]
        [Generator(Class = "identity")]
        public virtual long? Id { get; set; }
    }
}
