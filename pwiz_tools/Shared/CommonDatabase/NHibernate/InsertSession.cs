using System;
using System.Collections.Generic;
using pwiz.Common.SystemUtil;

namespace CommonDatabase.NHibernate
{
    public abstract class InsertSession : IDisposable
    {
        private IDictionary<Type, EntityInsertHandler> _entityHandlers = new Dictionary<Type, EntityInsertHandler>();

        protected InsertSession(NHibernateSession session)
        {
            Session = session;
            ActionQueue = new ActionQueue();
            ActionQueue.RunAsync(1, GetType().Name);
        }

        public NHibernateSession Session { get; }

        public ActionQueue ActionQueue { get; private set; }

        public void Flush()
        {
            foreach (var entityHandler in _entityHandlers.Values)
            {
                entityHandler.Flush();
            }
            ActionQueue.WaitForComplete();
        }
        protected void SetHandler(Type entityType, EntityInsertHandler insertHandler)
        {
            _entityHandlers[entityType] = insertHandler;
        }

        protected EntityInsertHandler GetEntityHandler(Type entityType)
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
        public InsertSession(NHibernateSession session) : base(session)
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
                SetHandler(classMetadata.MappedClass, new EntityInsertHandler(this, classMetadata.MappedClass, databaseMetadata));
            }
        }
    }
}
