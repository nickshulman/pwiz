using System;
using System.Data;
using NHibernate;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.Database.NHibernate
{
    public class SessionQueue : IDisposable
    {
        private SessionQueue()
        {
            ActionQueue = new ActionQueue();
            ActionQueue.RunAsync(1, nameof(SessionQueue));
        }
        public SessionQueue(ISession session) : this()  
        {
            Session = session;
        }

        public ActionQueue ActionQueue { get; }

        public SessionQueue(IStatelessSession statelessSession) : this()
        {
            StatelessSession = statelessSession;
        }

        public ISession Session { get; }
        public IStatelessSession StatelessSession { get; }

        public void Enqueue(Action action)
        {
            ActionQueue.Enqueue(action);
        }

        public void Dispose()
        {
            ActionQueue.Dispose();
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
            ActionQueue.WaitForComplete();
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
