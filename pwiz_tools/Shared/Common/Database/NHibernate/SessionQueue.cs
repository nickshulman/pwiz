using System;
using System.Collections.Generic;
using System.Data;
using NHibernate;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.Database.NHibernate
{
    public class SessionQueue : IDisposable
    {
        protected ActionQueue _actionQueue;

        private SessionQueue()
        {
            _actionQueue = new ActionQueue();
            _actionQueue.RunAsync(1, nameof(SessionQueue));
        }
        public SessionQueue(ISession session) : this()  
        {
            Session = session;
        }

        public SessionQueue(IStatelessSession statelessSession) : this()
        {
            StatelessSession = statelessSession;
        }

        public ISession Session { get; }
        public IStatelessSession StatelessSession { get; }

        public void Enqueue(Action action)
        {
            _actionQueue.Enqueue(action);
        }

        public void Dispose()
        {
            _actionQueue.Dispose();
        }

        public void InsertEntity(object entity)
        {
            if (Session != null)
            {
                Session.Save(entity);
            }
            else
            {
                StatelessSession.Insert(entity);
            }
        }

        public void Flush()
        {
            _actionQueue.WaitForComplete();
        }

        public IDbConnection Connection
        {
            get
            {
                if (Session != null)
                {
                    return Session.Connection;
                }

                return StatelessSession.Connection;
            }
        }

        public ICriteria CreateCriteria(Type type)
        {
            if (Session != null)
            {
                return Session.CreateCriteria(type);
            }

            return StatelessSession.CreateCriteria(type);
        }
    }
}
