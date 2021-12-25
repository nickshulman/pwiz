using System;
using System.Data;
using System.Data.SQLite;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
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
            var skydbFile = new SkydbFile(path);
            var sessionFactory = skydbFile.CreateSessionFactory(true);
            sessionFactory.Dispose();
            return skydbFile;
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

        public ISessionFactory CreateSessionFactory(bool createDatabase)
        {
            var configuration = CreateConfiguration();
            if (createDatabase)
            {
                configuration.SetProperty(@"hbm2ddl.auto", @"create");
            }
            return configuration.BuildSessionFactory();
        }

        private Configuration CreateConfiguration()
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
            var serializer = new HbmSerializer() {Validate = true, HbmNamespace = typeof(Entity).Namespace, HbmAssembly = assembly.FullName, };
            using (var stream = serializer.Serialize(assembly))
            {
                configuration.AddInputStream(stream);
            }
            
            return configuration;
        }
    }
}
