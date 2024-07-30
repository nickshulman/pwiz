using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NHibernate.Mapping.Attributes;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public abstract class EntityInsertSql<TEntity> where TEntity : Entity
    {
        private ImmutableList<string> _columnNames;

        public virtual string TableName
        {
            get { return typeof(TEntity).Name; }
        }
        public abstract IEnumerable<object> GetColumnValues(TEntity entity);

        public abstract ImmutableList<string> GetColumnNames();

        public string GetInsertSql(int batchSize, bool includeId)
        {
            IList<string> columnNames = GetColumnNames();
            if (includeId)
            {
                columnNames = columnNames.Prepend(nameof(Entity.Id)).ToList();
            }

            var columnNamesString = string.Join(", ", columnNames);
            var paramsString = string.Join(", ", Enumerable.Repeat("?", columnNames.Count));
            var lines = new List<string>
            {
                "INSERT INTO " + TableName + "(" + columnNamesString + ")"
            };
            if (batchSize == 1)
            {
                lines.Add("VALUES (" + paramsString + ")");
            }
            else
            {
                lines.Add("SELECT " + string.Join(", ", columnNames.Select(name=>"? AS " + name)));
                lines.AddRange(Enumerable.Repeat("UNION ALL SELECT " + paramsString, batchSize - 1));
            }

            return CommonTextUtil.LineSeparate(lines);
        }
    }

    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public class ReflectedEntityInsertSql<TEntity> : EntityInsertSql<TEntity> where TEntity : Entity
    {
        private static readonly ImmutableList<PropertyInfo> _columnProperties =
            ImmutableList.ValueOf(ListColumnProperties());

        private static readonly ImmutableList<string> _columnNames =
            ImmutableList.ValueOf(_columnProperties.Select(GetColumnName));

        public override IEnumerable<object> GetColumnValues(TEntity entity)
        {
            return _columnProperties.Select(prop => prop.GetValue(entity));
        }

        public override ImmutableList<string> GetColumnNames()
        {
            return _columnNames;
        }

        private static IEnumerable<PropertyInfo> ListColumnProperties()
        {
            return typeof(TEntity).GetProperties().Where(p => null != GetPropertyAttribute(p));
        }

        private static PropertyAttribute GetPropertyAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<PropertyAttribute>();
        }

        private static string GetColumnName(PropertyInfo propertyInfo)
        {
            var propertyAttribute = GetPropertyAttribute(propertyInfo);
            if (propertyAttribute == null)
            {
                return null;
            }

            return propertyAttribute.Name ?? propertyInfo.Name;
        }
    }
}
