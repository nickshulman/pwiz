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

namespace SkydbStorage.Api
{
    public class SkydbConnection : IDisposable
    {
        private IDbTransaction _transaction;
        public SkydbConnection(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; }

        public void Dispose()
        {
            Connection?.Dispose();
        }

        public void AttachDatabase(string filePath, string schemaName)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "ATTACH DATABASE ? AS " + QuoteIdentifier(schemaName);
                cmd.Parameters.Add(new SQLiteParameter() {Value = filePath});
                cmd.ExecuteNonQuery();
            }
        }

        public void DetachDatabase(string schemaName)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "DETACH DATABASE " + QuoteIdentifier(schemaName);
                cmd.ExecuteNonQuery();
            }
        }

        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException(@"Transaction already begun");
            }

            _transaction = Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException(@"No transaction");
            }
            _transaction.Commit();
            _transaction = null;
        }


        public string QuoteIdentifier(string identifier)
        {
            return SqliteOperations.QuoteIdentifier(identifier);
        }

        public string QuoteIdentifier(string schemaName, string objectName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                return QuoteIdentifier(objectName);
            }

            return QuoteIdentifier(schemaName) + "." + QuoteIdentifier(objectName);
        }

        public SkydbConnection(string filePath)
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
                BeginTransaction();
                var adder = new ExtractedDataFileWriter(writer, data);
                adder.Write();
                CommitTransaction();
            }
        }

        public void AddSkydbFiles(IEnumerable<SkydbConnection> filesToAdd)
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

        public void JoinUsingAttachDatabase(IEnumerable<SkydbConnection> filesToAdd)
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

                    var idOffsets = GetIdOffsets(writer.Connection, "toMerge", null);
                    writer.BeginTransaction();
                    foreach (var tableClass in SkydbSchema.GetTableClasses())
                    {
                        using (var insertSelect =
                            new InsertSelectStatement(writer.Connection, "toMerge", null, tableClass))
                        {
                            insertSelect.CopyData(idOffsets);
                        }
                        // using (var cmd = writer.Connection.CreateCommand())
                        // {
                        //     cmd.CommandText = "INSERT INTO " + SqliteOperations.QuoteIdentifier(tableClass.Name) +
                        //                       " SELECT * FROM toMerge." +
                        //                       SqliteOperations.QuoteIdentifier(tableClass.Name);
                        //     cmd.ExecuteNonQuery();
                        // }
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

        public void CopyDataToPath(string path)
        {
            const string targetSchema = "targetSchema";
            AttachDatabase(path, targetSchema);
            BeginTransaction();
            var idOffsets = GetIdOffsets(Connection, null, targetSchema);
            foreach (var tableClass in SkydbSchema.GetTableClasses())
            {
                using (var insertSelect =
                    new InsertSelectStatement(Connection, null, targetSchema, tableClass))
                {
                    insertSelect.CopyData(idOffsets);
                }
            }

            CommitTransaction();
            DetachDatabase(targetSchema);
        }

        public IDbConnection OpenConnection()
        {
            return Connection;
        }

        public SkydbWriter OpenWriter()
        {
            return new SkydbWriter(Connection);
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
                .SetProperty(@"connection.connection_string",
                    SqliteOperations.MakeConnectionStringBuilder(FilePath).ToString())
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
                    foreach (var tableClass in SkydbSchema.GetTableClasses())
                    {
                        ((SQLiteParameter) cmd.Parameters[0]).Value = tableClass.Name;
                        ((SQLiteParameter) cmd.Parameters[1]).Value = sequenceNumber;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static IDictionary<Type, long> GetIdOffsets(IDbConnection connection, string sourceSchema,
            string targetSchema)
        {
            var result = new Dictionary<Type, long>();
            foreach (var tableType in SkydbSchema.GetTableClasses())
            {
                long minSourceId;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Min(Id) FROM " +
                                      SqliteOperations.QuoteIdentifier(sourceSchema, tableType.Name);
                    var value = cmd.ExecuteScalar();
                    if (value == null || value is DBNull)
                    {
                        minSourceId = 1;
                    }
                    else
                    {
                        minSourceId = Convert.ToInt64(value);
                    }
                }

                long maxTargetId;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Max(Id) FROM " +
                                      SqliteOperations.QuoteIdentifier(targetSchema, tableType.Name);
                    var value = cmd.ExecuteScalar();
                    if (value == null || value is DBNull)
                    {
                        maxTargetId = 0;
                    }
                    else
                    {
                        maxTargetId = Convert.ToInt64(value);
                    }
                }
                result.Add(tableType, 1 + maxTargetId - minSourceId);
            }

            return result;
        }
    }
}
