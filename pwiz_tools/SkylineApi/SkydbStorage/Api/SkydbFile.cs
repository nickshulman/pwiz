using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
using SkydbStorage.DataApi;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;
using SkylineApi;
using InstrumentInfo = SkydbStorage.Internal.Orm.InstrumentInfo;

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

        public void AddChromatogramData(IExtractedDataFile data)
        {
            using (var writer = new SkydbWriter(OpenConnection()))
            {
                writer.SetUnsafeJournalMode();
                writer.BeginTransaction();
                var adder = new ExtractedDataFileWriter(writer, data);
                adder.Write();
                writer.CommitTransaction();
            }
        }

        public void AddSkydbFiles(IEnumerable<SkydbFile> filesToAdd)
        {
            using (var writer = OpenWriter())
            {
                writer.BeginTransaction();
                foreach (var fileToAdd in filesToAdd)
                {
                    using (var reader = fileToAdd.OpenConnection())
                    {
                        var joiner = new SkydbJoiner(writer, reader);
                        joiner.JoinFiles();
                    }
                }
                writer.CommitTransaction();
            }
        }

        public void JoinUsingAttachDatabase(IEnumerable<SkydbFile> filesToAdd)
        {
            using (var writer = OpenWriter())
            {
                foreach (var fileToAdd in filesToAdd)
                {
                    using (var cmd = writer.Connection.CreateCommand())
                    {
                        cmd.CommandText = "ATTACH ? AS toMerge";
                        cmd.Parameters.Add(new SQLiteParameter() {Value = fileToAdd.FilePath});
                        cmd.ExecuteNonQuery();
                    }
                    writer.BeginTransaction();
                    foreach (var tableClass in GetTableClasses())
                    {
                        using (var cmd = writer.Connection.CreateCommand())
                        {
                            cmd.CommandText = "INSERT INTO " + SqliteOperations.QuoteIdentifier(tableClass.Name) +
                                              " SELECT * FROM toMerge." +
                                              SqliteOperations.QuoteIdentifier(tableClass.Name);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    writer.CommitTransaction();
                    using (var cmd = writer.Connection.CreateCommand())
                    {
                        cmd.CommandText = "DETACH toMerge";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public IDbConnection OpenConnection()
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

        public ISessionFactory CreateSessionFactory()
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
                typeof(CandidatePeak),
                typeof(CandidatePeakGroup),
                typeof(InstrumentInfo),
            };
        }

        public void SetStartingSequenceNumber(long sequenceNumber)
        {
            using (var connection = OpenConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"DELETE FROM SQLITE_SEQUENCE";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO SQLITE_SEQUENCE (NAME, SEQ) VALUES (?, ?)";
                    cmd.Parameters.Add(new SQLiteParameter());
                    cmd.Parameters.Add(new SQLiteParameter());
                    foreach (var tableClass in GetTableClasses())
                    {
                        ((SQLiteParameter) cmd.Parameters[0]).Value = tableClass.Name;
                        ((SQLiteParameter) cmd.Parameters[1]).Value = sequenceNumber;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

    }
}
