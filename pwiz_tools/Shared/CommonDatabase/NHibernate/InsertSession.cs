using System;
using System.Collections.Generic;
using System.Data;
using CommonDatabase.NHibernate;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.Database.NHibernate
{
    public abstract class InsertSession : IDisposable
    {
        private IDictionary<Type, EntityHandler> _entityHandlers = new Dictionary<Type, EntityHandler>();

        protected InsertSession(IDbConnection connection)
        {
            ActionQueue = new ActionQueue();
            Connection = connection;
            ActionQueue.RunAsync(1, GetType().Name);
        }

        public IDbConnection Connection { get; }

        public ActionQueue ActionQueue { get; private set; }

        public void Flush()
        {
            foreach (var entityHandler in _entityHandlers.Values)
            {
                entityHandler.Flush();
            }
            ActionQueue.WaitForComplete();
        }
        protected void SetHandler(Type entityType, EntityHandler handler)
        {
            _entityHandlers[entityType] = handler;
        }

        protected EntityHandler GetEntityHandler(Type entityType)
        {
            _entityHandlers.TryGetValue(entityType, out var handler);
            return handler;
        }
        public void Dispose()
        {
            foreach (var entityHandler in _entityHandlers.Values)
            {
                entityHandler.Dispose();
            }
            ActionQueue.Dispose();
        }
        protected void SetBatchSize(Type type, int batchSize)
        {
            foreach (var handler in _entityHandlers.Values)
            {
                if (type.IsAssignableFrom(handler.EntityType))
                {
                    handler.SetBatchSize(batchSize);
                }
            }
        }
    }


    public class InsertSession<TEntity> : InsertSession
    {
        public InsertSession(IDbConnection connection) : base(connection)
        {
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
            SetBatchSize(typeof(T), batchSize);
        }


        public void AddEntityHandlers(NHibernateSessionFactory databaseMetadata)
        {
            foreach (var classMetadata in databaseMetadata.SessionFactory.GetAllClassMetadata().Values)
            {
                if (!typeof(TEntity).IsAssignableFrom(classMetadata.MappedClass))
                {
                    continue;
                }
                SetHandler(classMetadata.MappedClass, new EntityHandler(this, classMetadata.MappedClass, databaseMetadata));
            }
        }
    }
}
