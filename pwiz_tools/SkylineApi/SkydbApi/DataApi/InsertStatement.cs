using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm;
using SkydbApi.Orm.Attributes;

namespace SkydbApi.DataApi
{
    public class InsertStatement<T> : PreparedStatement where T:Entity
    {
        protected IDbCommand _command;
        protected List<Tuple<PropertyInfo, SQLiteParameter>> _parameters;

        public InsertStatement(IDbConnection connection) : base(connection)
        {
            _command = CreateCommand();
            var properties = ListColumnProperties().ToList();
            StringBuilder commandText = new StringBuilder("INSERT INTO " + typeof(T).Name + " (");
            commandText.Append(string.Join(",", properties.Select(prop => prop.Name)));
            commandText.Append(") VALUES (");
            commandText.Append(string.Join(",", properties.Select(prop => "?")));
            commandText.Append("); select last_insert_rowid();");
            _command.CommandText = commandText.ToString();
            _parameters = new List<Tuple<PropertyInfo, SQLiteParameter>>();
            foreach (var property in properties)
            {
                var sqliteParameter = new SQLiteParameter();
                _parameters.Add(Tuple.Create(property, sqliteParameter));
                _command.Parameters.Add(sqliteParameter);
            }
        }

        public void Insert(T entity)
        {
            foreach (var tuple in _parameters)
            {
                object value = tuple.Item1.GetValue(entity);
                if (value is Entity foreignKey)
                {
                    value = foreignKey.Id;
                }
                tuple.Item2.Value = value;
            }

            entity.Id = Convert.ToInt64(_command.ExecuteScalar());
        }

        public static IEnumerable<PropertyInfo> ListColumnProperties()
        {
            return typeof(T).GetProperties().Where(prop => null != prop.GetCustomAttribute<ColumnAttribute>());
        }
    }
}
