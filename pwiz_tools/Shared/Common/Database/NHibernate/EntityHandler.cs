using System;
using System.ComponentModel;
using System.Data;

namespace pwiz.Common.Database.NHibernate
{
    public abstract class EntityHandler
    {
        protected long _maxId;
        public EntityHandler(SessionQueue sessionQueue)
        {
            SessionQueue = sessionQueue;
        }

        public SessionQueue SessionQueue { get; }

        public IDbConnection Connection
        {
            get { return SessionQueue.Connection; }
        }

        public virtual void Insert(object entity)
        {
            SetId(entity, NextId());
            SessionQueue.Enqueue(()=>SessionQueue.InsertEntity(entity));
        }

        protected abstract void SetId(object entity, long id);

        public long MaxId
        {
            get;
            private set;
        }

        public long NextId()
        {
            lock (this)
            {
                _maxId++;
                return _maxId;
            }
        }

        public virtual void Dispose()
        {
        }

        public virtual void Flush()
        {
        }

        [Localizable(false)]
        public static string QuoteIdentifier(string str)
        {
            return "\"" + str.Replace("\"", "\"\"") + "\"";
        }

        public virtual void SetBatchSize(int batchSize)
        {
        }

        public abstract Type EntityType { get; }
    }
}
