using System.Data;
using System.Data.SQLite;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
using SkydbApi;
using SkydbApi.ChromatogramData;
using SkydbStorage.DataApi;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.Api
{
    public class SkydbFile
    {
        public SkydbFile(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }

        public bool UseUnsafeJournalMode { get; set; }

        public void CreateDatabase()
        {
            var configuration = CreateConfiguration(true);
            configuration.SetProperty(@"hbm2ddl.auto", @"create");
            using (configuration.BuildSessionFactory())
            {
            }
        }

        public void AddChromatogramData(IExtractedChromatograms data)
        {
            using (var writer = new SkydbWriter(OpenConnection()))
            {
                writer.SetUnsafeJournalMode();
                writer.BeginTransaction();
                using (var adder = new MsDataSourceFileWriter(writer, data))
                {
                    adder.Write();
                }
                writer.CommitTransaction();
            }
        }

        public void AddSkydbFile(SkydbFile fileToAdd)
        {
            using (var writer = OpenWriter())
            {
                using (var reader = fileToAdd.OpenConnection())
                {
                    var joiner = new SkydbJoiner(writer, reader);
                    joiner.JoinFiles();
                }
            }
        }

        internal IDbConnection OpenConnection()
        {
            var connectionStringBuilder = SqliteOperations.MakeConnectionStringBuilder(FilePath);
            return new SQLiteConnection(connectionStringBuilder.ToString()).OpenAndReturn();
        }

        public SkydbWriter OpenWriter()
        {
            var writer = new SkydbWriter(OpenConnection());
            if (UseUnsafeJournalMode)
            {
                writer.SetUnsafeJournalMode();
            }

            return writer;
        }

        internal ISessionFactory CreateSessionFactory()
        {
            var configuration = CreateConfiguration(false);
            return configuration.BuildSessionFactory();
        }

        private Configuration CreateConfiguration(bool enforceReferentialIntegrity)
        {
            Configuration configuration = new Configuration()
                //.SetProperty("show_sql", "true")
                //.SetProperty("generate_statistics", "true")
                .SetProperty(@"dialect", typeof(NHibernate.Dialect.SQLiteDialect).AssemblyQualifiedName)
                .SetProperty(@"connection.connection_string", SqliteOperations.MakeConnectionStringBuilder(FilePath).ToString())
                .SetProperty(@"connection.driver_class",
                    typeof(NHibernate.Driver.SQLite20Driver).AssemblyQualifiedName);
            configuration.SetProperty(@"connection.provider",
                typeof(NHibernate.Connection.DriverConnectionProvider).AssemblyQualifiedName);
            var assembly = typeof(Entity).Assembly;
            configuration.SetDefaultAssembly(assembly.FullName);
            configuration.SetDefaultNamespace(typeof(Entity).Namespace);
            var hbmWriter = new CustomHbmWriter
            {
                EnforceReferentialIntegrity = enforceReferentialIntegrity
            };
            var serializer = new HbmSerializer
            {
                Validate = true, HbmNamespace = typeof(Entity).Namespace, HbmAssembly = assembly.FullName,
                HbmWriter = hbmWriter
            };
            using (var stream = serializer.Serialize(assembly))
            {
                configuration.AddInputStream(stream);
            }
            return configuration;
        }
    }
}
