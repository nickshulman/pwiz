using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using NHibernate.Mapping.Attributes;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataApi
{
    public class InsertStatement<T> : IDisposable where T:Entity
    {
        protected IDbCommand _command;
        protected List<PropertyInfo> _parameters;

        public InsertStatement(IDbConnection connection)
        {
            _command = connection.CreateCommand();
            var properties = ListColumnProperties().ToList();
            StringBuilder commandText = new StringBuilder("INSERT INTO " + QuoteId(TableName) + " (");
            commandText.Append(string.Join(",", properties.Select(prop => QuoteId(prop.Name))));
            commandText.Append(") VALUES (");
            commandText.Append(string.Join(",", properties.Select(prop => "?")));
            commandText.Append("); select last_insert_rowid();");
            _command.CommandText = commandText.ToString();
            _parameters = new List<PropertyInfo>();
            foreach (var property in properties)
            {
                var sqliteParameter = new SQLiteParameter();
                _parameters.Add(property);
                _command.Parameters.Add(sqliteParameter);
            }
        }

        public string TableName
        {
            get { return typeof(T).Name; }
        }

        public virtual void Insert(T entity)
        {
            for (int iProperty = 0; iProperty < _parameters.Count; iProperty++)
            {
                var propertyInfo = _parameters[iProperty];
                object value = propertyInfo.GetValue(entity);
                if (value is Entity foreignKey)
                {
                    value = foreignKey.Id;
                }

                ((SQLiteParameter) _command.Parameters[iProperty]).Value = value;
            }

            entity.Id = Convert.ToInt64(_command.ExecuteScalar());
        }

        private void UpdateForeignKeys(T entity, EntityIdMap entityIdMap)
        {
            foreach (var property in _parameters)
            {
                var entityValue = property.GetValue(entity) as Entity;
                if (entityValue != null)
                {
                    var newEntity = (Entity) Activator.CreateInstance(entityValue.EntityType);
                    newEntity.Id = entityIdMap.GetNewId(entityValue.EntityType, entityValue.Id.Value);
                    property.SetValue(entity, newEntity);
                }
            }
        }

        public void CopyAll(IDbConnection connection, EntityIdMap entityIdMap)
        {
            var entityProperties = _parameters.Where(p => typeof(Entity).IsAssignableFrom(p.PropertyType)).ToList();
            foreach (var entity in SelectAll(connection))
            {
                foreach (var p in entityProperties)
                {
                    var entityValue = p.GetValue(entity) as Entity;
                    if (entityValue != null)
                    {
                        entityValue.Id = entityIdMap.GetNewId(entityValue.EntityType, entityValue.Id.Value);
                    }
                }

                var oldId = entity.Id.Value;
                entity.Id = null;
                Insert(entity);
                entityIdMap.SetNewId(typeof(T), oldId, entity.Id.Value);
            }
        }

        public IEnumerable<T> SelectAll(IDbConnection connection)
        {
            var commandText = new StringBuilder("SELECT ");
            commandText.Append(string.Join(",", _parameters.Select(p => p.Name).Prepend("Id").Select(QuoteId)));
            commandText.Append(" FROM " + QuoteId(TableName));
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = commandText.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = (T) Activator.CreateInstance(typeof(T));
                        entity.Id = reader.GetInt64(0);
                        for (int i = 0; i < _parameters.Count; i++)
                        {
                            object columnValue = reader.GetValue(i + 1);
                            if (columnValue == null || columnValue is DBNull)
                            {
                                continue;
                            }
                            var property = _parameters[i];
                            if (typeof(Entity).IsAssignableFrom(property.PropertyType))
                            {
                                long? foreignKey = Convert.ToInt64(columnValue);
                                var foreignEntity = (Entity) Activator.CreateInstance(property.PropertyType);
                                foreignEntity.Id = foreignKey;
                                property.SetValue(entity, foreignEntity);
                            }
                            else
                            {

                                property.SetValue(entity, columnValue);
                            }
                        }

                        yield return entity;
                    }
                }
            }
        }

        public static IEnumerable<PropertyInfo> ListColumnProperties()
        {
            return typeof(T).GetProperties().Where(prop =>
                null != prop.GetCustomAttribute<PropertyAttribute>() ||
                null != prop.GetCustomAttribute<ManyToOneAttribute>());
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        private static string QuoteId(string str)
        {
            return SqliteOperations.QuoteIdentifier(str);
        }
    }
}
