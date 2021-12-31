using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.DataAccess
{
    public class SkydbConnection : IDisposable
    {
        private IDbTransaction _transaction;
        public SkydbConnection(SkydbSchema schema, IDbConnection connection)
        {
            SkydbSchema = schema;
            Connection = connection;
        }

        public static SkydbConnection OpenFile(string path)
        {
            var connection = SqliteOps.OpenDatabaseFile(path);
            return new SkydbConnection(SkydbSchema.FromConnection(connection), connection);
        }

        public SkydbSchema SkydbSchema { get; }

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
            return SqliteOps.QuoteIdentifier(identifier);
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

        public void AddChromatogramData(IExtractedDataFile data)
        {
            using (var writer = new SkydbWriter(this))
            {
                BeginTransaction();
                var adder = new ExtractedDataFileWriter(writer, data);
                adder.Write();
                CommitTransaction();
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
                    new SelectIntoStatement(SkydbSchema, Connection, null, targetSchema, tableClass))
                {
                    insertSelect.CopyData(idOffsets);
                }
            }

            CommitTransaction();
            DetachDatabase(targetSchema);
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
                    SqliteOps.MakeConnectionStringBuilder(FilePath).ToString())
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
                                      SqliteOps.QuoteIdentifier(sourceSchema, tableType.Name);
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
                                      SqliteOps.QuoteIdentifier(targetSchema, tableType.Name);
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

        public IEnumerable<T> SelectAll<T>() where T : Entity, new()
        {
            using (var statement = new SelectStatement<T>(this))
            {
                return statement.SelectAll().ToList();
            }
        }
    }
}
