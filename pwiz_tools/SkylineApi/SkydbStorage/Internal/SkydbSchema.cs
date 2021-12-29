using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal
{
    public class SkydbSchema
    {
        public IEnumerable<Tuple<PropertyInfo, Type>> GetColumns(Type entityType)
        {
            foreach (var property in entityType.GetProperties())
            {
                Type foreignKeyType;
                if (property.GetCustomAttribute<PropertyAttribute>() != null)
                {
                    foreignKeyType = null;
                }
                else
                {
                    var manyToOneAttribute = property.GetCustomAttribute<ManyToOneAttribute>();
                    if (manyToOneAttribute != null)
                    {
                        foreignKeyType = manyToOneAttribute.ClassType;
                    }
                    else
                    {
                        continue;
                    }
                }

                yield return Tuple.Create(property, foreignKeyType);
            }
        }
    }
}
