using System.Runtime.CompilerServices;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    public class Entity
    {
        [Id(0, Name = "Id")]
        [Generator(1, Class = "native")]
        public virtual long? Id { get; set; }
    }

    public class Entity<TEntity> : Entity where TEntity : Entity
    {
        protected bool Equals(Entity<TEntity> other)
        {
            if (Id.HasValue)
            {
                return Id == other.Id;
            }

            return ReferenceEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var entity = obj as Entity<TEntity>;
            if (entity == null)
            {
                return false;
            }
            return Equals(entity);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? RuntimeHelpers.GetHashCode(this);
        }
    }
}
