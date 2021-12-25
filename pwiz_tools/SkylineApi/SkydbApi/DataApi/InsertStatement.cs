using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using NHibernate.Mapping.Attributes;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertStatement<T> : IDisposable where T:Entity
    {
        protected IDbCommand _command;
        protected List<PropertyInfo> _parameters;

        public InsertStatement(IDbConnection connection)
        {
            _command = connection.CreateCommand();
            var properties = ListColumnProperties().ToList();
            StringBuilder commandText = new StringBuilder("INSERT INTO " + typeof(T).Name + " (");
            commandText.Append(string.Join(",", properties.Select(prop => SqliteOperations.QuoteIdentifier(prop.Name))));
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
    }
}
