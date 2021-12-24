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

        public SkydbConnection OpenConnection()
        {
            return new SkydbConnection(new SQLiteConnection(
                SessionFactoryFactory.SQLiteConnectionStringBuilderFromFilePath(FilePath).ToString()).OpenAndReturn());
        }
    }
}
