using System;
using System.Collections.Generic;

namespace pwiz.Common.Database.NHibernate
{
    public class InsertSession<TEntity> : IDisposable
    {
        private IDictionary<Type, EntityHandler> _entityHandlers;
        public InsertSession(SessionQueue sessionQueue, IDictionary<Type, EntityHandler> entityHandlers)
        {
            SessionQueue = sessionQueue;
            _entityHandlers = entityHandlers;
        }

        public SessionQueue SessionQueue { get; private set; }

        public void Flush()
        {
            foreach (var entityHandler in _entityHandlers.Values)
            {
                entityHandler.Flush();
            }
            SessionQueue.Flush();
        }

        public void Insert<T>(T entity) where T : TEntity
        {
            var handler = GetEntityHandler(typeof(T));
            if (handler == null)
            {
                throw new ArgumentException(string.Format("Unsupported entity type {0}", typeof(T)));
            }
            handler.Insert(entity);
        }

        public void SetBatchSize<T>(int batchSize) where T : TEntity
        {
            foreach (var handler in _entityHandlers.Values)
            {
                if (typeof(T).IsAssignableFrom(handler.EntityType))
                {
                    handler.SetBatchSize(batchSize);
                }
            }
        }

        private EntityHandler GetEntityHandler(Type entityType)
        {
            _entityHandlers.TryGetValue(entityType, out var handler);
            return handler;
        }

        public static InsertSession<TEntity> Create(SessionQueue sessionQueue,
            DatabaseMetadata databaseMetadata)
        {
            var entityHandlers = new Dictionary<Type, EntityHandler>();
            foreach (var classMetadata in databaseMetadata.SessionFactory.GetAllClassMetadata().Values)
            {
                if (!typeof(TEntity).IsAssignableFrom(classMetadata.MappedClass))
                {
                    continue;
                }
                entityHandlers.Add(classMetadata.MappedClass, new EntityHandler(sessionQueue, classMetadata.MappedClass, databaseMetadata));
            }

            return new InsertSession<TEntity>(sessionQueue, entityHandlers);
        }

        public void Dispose()
        {
            foreach (var entityHandler in _entityHandlers.Values)
            {
                entityHandler.Dispose();
            }
        }
    }
}
