using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
using NHibernate.Tool.hbm2ddl;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess
{
    public class SkydbSchema
    {
        private IList<string> _scoreNames;
        public const string MEMORY_DATABASE_PATH = ":memory:";

        public SkydbSchema(IEnumerable<string> scoreNames)
        {
            _scoreNames = scoreNames.ToList();
        }

        public SkydbSchema(IEnumerable<Type> scoreTypes) : this(scoreTypes.Select(type=>type.FullName))
        {
        }

        public static SkydbSchema FromConnection(IDbConnection connection)
        {
            var scoreNames = SqliteOps.ListColumnNames(connection, nameof(Scores))
                .Except(new[] {nameof(Entity.Id)});
            return new SkydbSchema(scoreNames);
        }

        public IEnumerable<string> ScoreNames
        {
            get { return _scoreNames.AsEnumerable(); }
        }
        public IEnumerable<ColumnInfo> GetColumns(Type entityType)
        {
            foreach (var property in entityType.GetProperties())
            {
                Type foreignKeyType;
                if (property.GetCustomAttribute<PropertyAttribute>() != null)
                {
                    foreignKeyType = null;
                }
                else
                {
                    var manyToOneAttribute = property.GetCustomAttribute<ManyToOneAttribute>();
                    if (manyToOneAttribute != null)
                    {
                        foreignKeyType = manyToOneAttribute.ClassType;
                    }
                    else
                    {
                        continue;
                    }
                }

                yield return new ColumnInfo.Property(property, foreignKeyType);
            }

            if (entityType == typeof(Scores))
            {
                foreach (var scoreName in ScoreNames)
                {
                    yield return new ColumnInfo.Score(scoreName);
                }
            }
        }

        public static IEnumerable<Type> GetTableClasses()
        {
            return new[]
            {
                typeof(ExtractedFile),
                typeof(Scores),
                typeof(SpectrumInfo),
                typeof(SpectrumList),
                typeof(ChromatogramData),
                typeof(ChromatogramGroup),
                typeof(Chromatogram),
                typeof(CandidatePeakGroup),
                typeof(CandidatePeak),
                typeof(InstrumentInfo),
            };
        }

        public Configuration CreateHibernateConfiguration(string filePath)
        {
            Configuration configuration = new Configuration()
                //.SetProperty("show_sql", "true")
                //.SetProperty("generate_statistics", "true")
                .SetProperty(@"dialect", typeof(NHibernate.Dialect.SQLiteDialect).AssemblyQualifiedName)
                .SetProperty(@"connection.connection_string",
                    SqliteOps.MakeConnectionStringBuilder(filePath).ToString())
                .SetProperty(@"connection.driver_class",
                    typeof(NHibernate.Driver.SQLite20Driver).AssemblyQualifiedName);
            configuration.SetProperty(@"connection.provider",
                typeof(NHibernate.Connection.DriverConnectionProvider).AssemblyQualifiedName);
            var assembly = typeof(Entity).Assembly;
            configuration.SetDefaultAssembly(assembly.FullName);
            configuration.SetDefaultNamespace(typeof(Entity).Namespace);
            var hbmWriter = new CustomHbmWriter
            {
                EnforceReferentialIntegrity = true,
            };
            var serializer = new HbmSerializer
            {
                Validate = true,
                HbmNamespace = typeof(Entity).Namespace,
                HbmAssembly = assembly.FullName,
                HbmWriter = hbmWriter
            };
            using (var stream = serializer.Serialize(assembly))
            {
                configuration.AddInputStream(stream);
            }

            return configuration;
        }

        public SkydbConnection CreateDatabase(string path)
        {
            var configuration = CreateHibernateConfiguration(path);
            var connection = new SQLiteConnection(SqliteOps.MakeConnectionStringBuilder(path).ToString());
            connection.OpenAndReturn();
            new SchemaExport(configuration).Execute(false, true, false, connection, null);
            foreach (var scoreName in ScoreNames)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "ALTER TABLE Scores ADD COLUMN " + SqliteOps.QuoteIdentifier(scoreName) +
                                      " DOUBLE";
                    cmd.ExecuteNonQuery();
                }
            }
            return new SkydbConnection(this, connection);
        }
    }
}
