using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class SkylineDocumentImpl : ISkylineDocument
    {
        public SkylineDocumentImpl(string path)
        {
            Path = path;
            using (var connection = SqliteOps.OpenDatabaseFile(path))
            {
                SkydbSchema = SkydbSchema.FromConnection(connection);
            }
        }

        public SkydbSchema SkydbSchema { get; private set; }

        public string Path { get; set; }

        public IEnumerable<IExtractedDataFile> ExtractedDataFiles
        {
            get
            {
                return CallWithConnection(connection =>
                {
                    var extractedDataFiles =
                        connection.SelectAllExtractedFiles().Select(file=>new ExtractedDataFileImpl(this, file)).ToList();
                    SelectPrecursors(connection, extractedDataFiles);
                    return extractedDataFiles;
                });
            }
        }

        public T CallWithConnection<T>(Func<SkydbConnection, T> func)
        {
            using (var connection = SkydbConnection.OpenFile(Path))
            {
                return func(connection);
            }
        }

        public IList<T> SelectAll<T>() where T : Entity, new()
        {
            return CallWithConnection(connection =>
            {
                using (var selectStatement = new SelectStatement<T>(connection))
                {
                    return selectStatement.SelectAll().ToList();
                }
            });
        }

        public IList<T> SelectWhere<T>(string column, object value) where T : Entity, new()
        {
            return CallWithConnection(connection =>
            {
                using (var selectStatement = new SelectStatement<T>(connection))
                {
                    return selectStatement.SelectWhere(column, value).ToList();
                }
            });
        }

        public IList<T> SelectWhereIn<T>(string column, IEnumerable values) where T : Entity, new()
        {
            return CallWithConnection(connection =>
            {
                using (var selectStatement = new SelectStatement<T>(connection))
                {
                    return selectStatement.SelectWhereIn(column, values.Cast<object>().ToList()).ToList();
                }
            });
        }

        private void SelectPrecursors(SkydbConnection connection,
            IList<ExtractedDataFileImpl> dataFiles)
        {
            var filesByUd = dataFiles.ToDictionary(file => file.Entity.Id.Value);
            foreach (var grouping in SelectPrecursors(connection)
                .GroupBy(group => Tuple.Create(group.TextId, group.PrecursorMz)))
            {
                var groupIds = grouping.Select(group => Tuple.Create(filesByUd[group.File], group.Id.Value)).ToList();
                Precursor.CreatePrecursor(this, grouping.Key.Item1, grouping.Key.Item2, groupIds);
            }
        }

        private IEnumerable<ChromatogramGroup> SelectPrecursors(SkydbConnection connection)
        {
            using (var cmd = connection.Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT " + string.Join(", ", nameof(ChromatogramGroup.Id),
                                                nameof(ChromatogramGroup.TextId), nameof(ChromatogramGroup.PrecursorMz),
                                                nameof(ChromatogramGroup.File))
                                            + " FROM ChromatogramGroup";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new ChromatogramGroup()
                        {
                            Id = reader.GetInt64(0),
                            TextId = reader.IsDBNull(1) ? null : reader.GetString(1),
                            PrecursorMz = reader.GetDouble(2),
                            File = reader.GetInt64(3)
                        };
                    }
                }
            }
        }
    }
}
