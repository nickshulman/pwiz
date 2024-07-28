using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Metadata;

namespace pwiz.Common.Database.NHibernate
{
    public class DatabaseMetadata
    {
        private Configuration _configuration;
        public DatabaseMetadata(Configuration configuration, ISessionFactory sessionFactory)
        {
            SessionFactory = sessionFactory;
            _configuration = configuration;
        }

        public ISessionFactory SessionFactory { get; }

        public IClassMetadata GetClassMetadata(string entityName)
        {
            return SessionFactory.GetClassMetadata(entityName);
        }

        public IClassMetadata GetClassMetadata(Type persistentClass)
        {
            return SessionFactory.GetClassMetadata(persistentClass);
        }

        public PersistentClass GetPersistentClass(Type persistentClass)
        {
            return _configuration.GetClassMapping(persistentClass);
        }

        public IEnumerable<string> GetColumnNames(Type persistentClass)
        {
            return GetPersistentClass(persistentClass).Table.ColumnIterator.Select(column => column.Text);
        }

        public Dictionary<string, object> GetColumnValues(Type entityType, object entity)
        {
            var classMetadata = GetClassMetadata(entityType);
            var persistentClass = GetPersistentClass(entityType);
            var columnValues = new Dictionary<string, object>();

            foreach (var propertyName in classMetadata.PropertyNames)
            {
                var propertyType = classMetadata.GetPropertyType(propertyName);
                var property = persistentClass.GetProperty(propertyName);
                var column = property.ColumnIterator.SingleOrDefault();
                if (column != null)
                {
                    var value = classMetadata.GetPropertyValue(entity, propertyName);
                    if (propertyType.IsEntityType && value != null)
                    {
                        var associatedClassMetadata = GetClassMetadata(propertyType.ReturnedClass);
                        value = associatedClassMetadata.GetIdentifier(value);
                    }

                    columnValues[column.Text] = value;
                }
            }
            return columnValues;
        }

    }
}
