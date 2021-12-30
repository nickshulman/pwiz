using System;
using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess;
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
                return CallWithConnection(skydbConnection =>
                {
                    using (var selectStatement = new SelectStatement<ExtractedFile>(skydbConnection))
                    {
                        return selectStatement.SelectAll().Select(file => new ExtractedDataFileImpl(this, file));
                    }
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
    }
}
