using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Metadata;

namespace CommonDatabase.NHibernate
{
    public class NHibernateSessionFactory : IDisposable
    {
        public NHibernateSessionFactory(NHibernateConfiguration configuration, ISessionFactory sessionFactory)
        {
            Configuration = configuration;
            SessionFactory = sessionFactory;
        }

        public NHibernateSessionFactory(Configuration configuration, ISessionFactory sessionFactory)
        {
            Configuration = new NHibernateConfiguration(configuration);
            SessionFactory = sessionFactory;
            LeaveOpen = true;
        }

        public NHibernateConfiguration Configuration { get; }
        public ISessionFactory SessionFactory { get; }

        public bool LeaveOpen { get; }

        public void Dispose()
        {
            if (!LeaveOpen)
            {
                SessionFactory.Dispose();
            }
        }

        public IClassMetadata GetClassMetadata(string entityName)
        {
            return SessionFactory.GetClassMetadata(entityName);
        }

        public IClassMetadata GetClassMetadata(Type persistentClass)
        {
            return SessionFactory.GetClassMetadata(persistentClass);
        }

        public Dictionary<string, object> GetColumnValues(Type entityType, object entity)
        {
            var classMetadata = GetClassMetadata(entityType);
            var persistentClass = Configuration.GetPersistentClass(entityType);
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
