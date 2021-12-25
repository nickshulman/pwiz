using System;
using System.Runtime.CompilerServices;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    public abstract class Entity
    {
        [Id(Name = "Id", Generator = "native")]
        public long? Id { get; set; }

        public abstract Type EntityType { get; }

        protected bool Equals(Entity other)
        {
            if (other == null)
            {
                return false;
            }
            if (Id.HasValue)
            {
                return Id == other.Id && EntityType == other.EntityType;
            }

            return ReferenceEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as Entity);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? RuntimeHelpers.GetHashCode(this);
        }

    }

    public class Entity<TEntity> : Entity where TEntity : Entity
    {
        public override Type EntityType => typeof(TEntity);
    }
}
