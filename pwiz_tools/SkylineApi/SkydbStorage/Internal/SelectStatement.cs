using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using SkydbStorage.DataApi;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.Internal
{
    public class SelectStatement<T> : IDisposable where T : Entity
    {
        protected IDbCommand _command;
        protected IList<Tuple<PropertyInfo, Func<IDataRecord, object>>> _columns;

        public SelectStatement(IDbConnection connection)
        {
            _columns = new List<Tuple<PropertyInfo, Func<IDataRecord, object>>>();
            StringBuilder commandTest = new StringBuilder("SELECT Id");
            foreach (var property in InsertStatement<T>.ListColumnProperties())
            {
                int columnIndex = _columns.Count + 1;
                var propertyType = property.PropertyType;
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
                else if (typeof(Entity).IsAssignableFrom(propertyType))
                {
                    continue;
                    getter = record =>
                    {
                        var entity = (Entity) Activator.CreateInstance(propertyType);
                        entity.Id = record.GetInt64(columnIndex);
                        return entity;
                    };
                }
                else
                {
                    getter = record => record.GetValue(columnIndex);
                }
                commandTest.Append(", " + SqliteOperations.QuoteIdentifier(property.Name));
                _columns.Add(Tuple.Create(property, getter));
            }

            commandTest.Append(" FROM " + SqliteOperations.QuoteIdentifier(typeof(T).Name));
            _command = connection.CreateCommand();
            _command.CommandText = commandTest.ToString();
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public IEnumerable<T> SelectAll()
        {
            using (var reader = _command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var entity = Activator.CreateInstance<T>();
                    entity.Id = reader.GetInt64(0);
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
    }
}
