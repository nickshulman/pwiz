using System;
using System.Collections.Generic;
using System.Linq;
using CommonDatabase.SQLite;
using NHibernate.Cfg;
using NHibernate.Mapping;

namespace CommonDatabase.NHibernate
{
    public class NHibernateConfiguration
    {
        public NHibernateConfiguration() : this(NewDefaultConfiguration())
        {
        }

        public NHibernateConfiguration(Configuration configuration)
        {
            Configuration = configuration;
        }

        public Configuration Configuration { get; }

        public NHibernateConfiguration SetCreateSchema()
        {
            Configuration.SetProperty(@"hbm2ddl.auto", @"create");
            return this;
        }

        public NHibernateConfiguration SetShowSql()
        {
            Configuration.SetProperty("show_sql", "true")
                .SetProperty("generate_statistics", "true");
            return this;
        }

        public NHibernateConfiguration SetSQLiteFilePath(string path)
        {
            Configuration.SetProperty(@"dialect",
                    typeof(global::NHibernate.Dialect.SQLiteDialect).AssemblyQualifiedName)
                .SetProperty(@"connection.connection_string",
                    SQLiteDbConnection.NewSQLiteConnectionStringBuilder(path).ToString())
                .SetProperty(@"connection.driver_class",
                    typeof(global::NHibernate.Driver.SQLite20Driver).AssemblyQualifiedName)
                .SetProperty(@"connection.provider",
                    typeof(global::NHibernate.Connection.DriverConnectionProvider).AssemblyQualifiedName);
            return this;
        }

        public static Configuration NewDefaultConfiguration()
        {
            return new Configuration()
                .SetProperty(@"connection.provider", typeof(global::NHibernate.Connection.DriverConnectionProvider).AssemblyQualifiedName);
        }

        public PersistentClass GetPersistentClass(Type persistentClass)
        {
            return Configuration.GetClassMapping(persistentClass);
        }

        public IEnumerable<string> GetColumnNames(Type persistentClass)
        {
            return GetPersistentClass(persistentClass).Table.ColumnIterator.Select(column => column.Text);
        }

    }
}
