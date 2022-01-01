using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess
{
    public class SelectStatement<T> : PreparedStatement where T:Entity, new()
    {
        protected IList<Tuple<ColumnInfo, Func<IDataRecord, object>>> _columns;

        public SelectStatement(SkydbConnection connection) : base(connection.Connection)
        {
            _columns = new List<Tuple<ColumnInfo, Func<IDataRecord, object>>>();
            StringBuilder commandTest = new StringBuilder("SELECT Id");
            foreach (var property in connection.SkydbSchema.GetColumns(typeof(T)))
            {
                int columnIndex = _columns.Count + 1;
                var propertyType = property.ValueType;
                Func<IDataRecord, object> getter;
                if (propertyType == typeof(long) || propertyType == typeof(long?))
                {
                    getter = record=>record.GetInt64(columnIndex);
                }
                else if (propertyType == typeof(int) || propertyType == typeof(int?))
                {
                    getter = record => record.GetInt32(columnIndex);
                }
                else if (propertyType == typeof(double) || propertyType == typeof(double?))
                {
                    getter = record => record.GetDouble(columnIndex);
                }
                else 
                {
                    getter = record => record.GetValue(columnIndex);
                }
                commandTest.Append(", " + SqliteOps.QuoteIdentifier(property.Name));
                _columns.Add(Tuple.Create(property, getter));
            }

            commandTest.Append(" FROM " + SqliteOps.QuoteIdentifier(typeof(T).Name));
            Command.CommandText = commandTest.ToString();
        }

        protected IEnumerable<T> ReturnAll(IDbCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var entity = new T()
                    {
                        Id = reader.GetInt64(0)
                    };
                    for (int i = 0; i < _columns.Count; i++)
                    {
                        if (reader.IsDBNull(i + 1))
                        {
                            continue;
                        }

                        var tuple = _columns[i];
                        var property = tuple.Item1;
                        object value = tuple.Item2(reader);
                        property.SetValue(entity, value);
                        property.SetValue(entity, value);
                    }

                    yield return entity;
                }
            }
        }

        public IEnumerable<T> SelectAll()
        {
            return ReturnAll(Command);
        }

        public IEnumerable<T> SelectWhere(string columnName, object value)
        {
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = Command.CommandText + " WHERE " + SqliteOps.QuoteIdentifier(columnName) + " = ?";
                command.Parameters.Add(new SQLiteParameter {Value = value});
                foreach (var item in ReturnAll(command))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<T> SelectWhereIn(string columnName, IEnumerable values)
        {
            var parameters = new List<SQLiteParameter>();
            StringBuilder commandText = new StringBuilder(Command.CommandText);
            commandText.Append(" WHERE " + SqliteOps.QuoteIdentifier(columnName) + " IN (");
            string comma = string.Empty;
            foreach (var value in values)
            {
                commandText.Append(comma);
                comma = ", ";
                commandText.Append("?");
                parameters.Add(new SQLiteParameter() {Value = value});
            }

            commandText.Append(")");
            if (parameters.Count == 0)
            {
                yield break;
            }
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = commandText.ToString();
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
                foreach (var item in ReturnAll(command))
                {
                    yield return item;
                }
            }

        }
    }
}
