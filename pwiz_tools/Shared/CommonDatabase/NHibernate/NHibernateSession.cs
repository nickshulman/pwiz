using System;
using System.Data;
using NHibernate;

namespace CommonDatabase.NHibernate
{
    public abstract class NHibernateSession : IDisposable
    {
        protected NHibernateSession(NHibernateSessionFactory sessionFactory)
        {
            SessionFactory = sessionFactory;
        }

        public NHibernateSessionFactory SessionFactory { get; }
        public abstract IDbConnection Connection { get; }
        public abstract ICriteria CreateCriteria(Type persistentClass);

        public virtual void Dispose()
        {
        }

        public class Stateful : NHibernateSession
        {
            public Stateful(NHibernateSessionFactory sessionFactory, ISession session) : base(sessionFactory)
            {
                Session = session;
            }

            public ISession Session { get; }

            public override IDbConnection Connection
            {
                get { return Session.Connection; }
            }

            public override ICriteria CreateCriteria(Type persistentClass)
            {
                return Session.CreateCriteria(persistentClass);
            }

            public override void Dispose()
            {
                Session.Dispose();
                base.Dispose();
            }
        }

        public class Stateless : NHibernateSession
        {
            public Stateless(NHibernateSessionFactory sessionFactory, IStatelessSession statelessSession) : base(
                sessionFactory)
            {
                StatelessSession = statelessSession;
            }

            public IStatelessSession StatelessSession { get; }
            public override IDbConnection Connection
            {
                get { return StatelessSession.Connection; }
            }
            public override ICriteria CreateCriteria(Type persistentClass)
            {
                return StatelessSession.CreateCriteria(persistentClass);
            }

            public override void Dispose()
            {
                StatelessSession.Dispose();
                base.Dispose();
            }
        }
    }
}
