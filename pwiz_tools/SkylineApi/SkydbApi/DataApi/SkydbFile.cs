using System.Data;
using Microsoft.Data.Sqlite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SkydbFile
    {
        public SkydbFile(string path)
        {
            FilePath = path;
        }
        public static SkydbFile CreateNewSkydbFile(string path)
        {
            var sessionFactory = SessionFactoryFactory.CreateSessionFactory(path, typeof(Entity), true);
            sessionFactory.Dispose();
            return new SkydbFile(path);
        }

        public string FilePath { get; }

        public IDbConnection OpenConnection(SqliteOpenMode mode)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder() {DataSource = FilePath};
            connectionStringBuilder.Mode = mode;
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            connection.Open();
            return connection;
        }

        public SkydbWriter OpenWriter()
        {
            return new SkydbWriter(OpenConnection(SqliteOpenMode.ReadWrite));
        }

        public SkydbReader OpenReader()
        {
            return new SkydbReader(OpenConnection(SqliteOpenMode.ReadOnly));
        }
    }
}
