using System;
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
                    var chromatogramGroups = connection.SelectAll<ChromatogramGroup>().ToLookup(group => group.File);
                    var chromatograms = connection.SelectAll<Chromatogram>().ToLookup(chrom => chrom.ChromatogramGroup);
                    var spectrumInfos = connection.SelectAll<SpectrumInfo>().ToLookup(spectrum => spectrum.File);
                    return connection.SelectAll<ExtractedFile>().Select(file => new ExtractedDataFileImpl(this, file,
                        chromatogramGroups[file.Id.Value], spectrumInfos[file.Id.Value], chromatograms)).ToList();
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
    }
}
