using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess
{
    public class InsertStatement<T> : PreparedStatement where T:Entity, new()
    {
        protected List<ColumnInfo> _parameters;

        public InsertStatement(SkydbSchema skydbSchema, IDbConnection connection) : base(connection)
        {
            var properties = skydbSchema.GetColumns(typeof(T)).ToList();
            StringBuilder commandText = new StringBuilder("INSERT INTO " + QuoteId(TableName) + " (");
            commandText.Append(string.Join(",", properties.Select(prop => QuoteId(prop.Name))));
            commandText.Append(") VALUES (");
            commandText.Append(string.Join(",", properties.Select(prop => "?")));
            commandText.Append("); select last_insert_rowid();");
            Command.CommandText = commandText.ToString();
            _parameters = new List<ColumnInfo>();
            foreach (var property in properties)
            {
                var sqliteParameter = new SQLiteParameter();
                _parameters.Add(property);
                Command.Parameters.Add(sqliteParameter);
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

                ((SQLiteParameter) Command.Parameters[iProperty]).Value = value;
            }

            entity.Id = Convert.ToInt64(Command.ExecuteScalar());
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
            var entityProperties = _parameters.Where(p => null != p.ForeignEntityType).ToList();
            foreach (var entity in SelectAll(connection))
            {
                foreach (var p in entityProperties)
                {
                    var foreignId = (long?) p.GetValue(entity);
                    if (foreignId.HasValue)
                    {
                        p.SetValue(entity, entityIdMap.GetNewId(p.ForeignEntityType, foreignId.Value));
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
                        var entity = new T
                        {
                            Id = reader.GetInt64(0)
                        };
                        for (int i = 0; i < _parameters.Count; i++)
                        {
                            object columnValue = reader.GetValue(i + 1);
                            if (columnValue == null || columnValue is DBNull)
                            {
                                continue;
                            }
                            var property = _parameters[i];
                            property.SetValue(entity, columnValue);
                        }

                        yield return entity;
                    }
                }
            }
        }

        private static string QuoteId(string str)
        {
            return SqliteOps.QuoteIdentifier(str);
        }
    }
}
