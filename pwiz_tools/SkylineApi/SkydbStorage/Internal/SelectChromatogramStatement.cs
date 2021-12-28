using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.Internal
{
    public class SelectChromatogramStatement : IDisposable
    {
        protected IDbCommand _command;
        public SelectChromatogramStatement(IDbConnection connection)
        {
            _command = connection.CreateCommand();
            _command.CommandText =
                @"SELECT Id, ChromatogramGroup, ChromatogramData, ProductMz, ExtractionWidth, IonMobilityValue, IonMobilityExtractionWidth, Source FROM Chromatogram";
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public IEnumerable<Chromatogram> SelectAll()
        {
            using (var reader = (SQLiteDataReader) _command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var chromatogram = new Chromatogram()
                    {
                        Id = reader.GetInt64(0),
                        ChromatogramGroup = MakeEntity(GetOptionalLong(reader, 1), () => new ChromatogramGroup()),
                        ChromatogramData = MakeEntity(GetOptionalLong(reader, 2), () => new ChromatogramData()),
                        ProductMz = reader.GetDouble(3),
                        ExtractionWidth = reader.GetDouble(4),
                        IonMobilityValue = GetOptionalDouble(reader,5),
                        IonMobilityExtractionWidth = GetOptionalDouble(reader, 6),
                        Source = reader.GetInt32(7)
                    };
                    yield return chromatogram;
                }
            }
        }

        private T MakeEntity<T>(long? id, Func<T> constructor) where T : Entity
        {
            if (!id.HasValue)
            {
                return null;
            }

            T entity = constructor();
            entity.Id = id.Value;
            return entity;
        }

        private double? ToOptionalDouble(object value)
        {
            if (value == null || value is DBNull)
            {
                return null;
            }

            return Convert.ToDouble(value);
        }

        private double? GetOptionalDouble(SQLiteDataReader reader, int column)
        {
            return reader.IsDBNull(column) ? (double?) null : reader.GetDouble(column);
        }
        private long? GetOptionalLong(SQLiteDataReader reader, int column)
        {
            return reader.IsDBNull(column) ? (long?)null : reader.GetInt64(column);
        }
    }
}
