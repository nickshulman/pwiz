using System.Data;
using System.Data.SQLite;
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

        public IDbConnection OpenConnection()
        {
            var connectionStringBuilder = SqliteOperations.MakeConnectionStringBuilder(FilePath);
            return new SQLiteConnection(connectionStringBuilder.ToString()).OpenAndReturn();
}

        public SkydbWriter OpenWriter()
        {
            return new SkydbWriter(OpenConnection());
        }

        public SkydbReader OpenReader()
        {
            return new SkydbReader(OpenConnection());
        }
    }
}
