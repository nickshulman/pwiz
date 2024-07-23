using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using NHibernate.Exceptions;
using pwiz.Common.Database;
using pwiz.Common.SystemUtil;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SkydbConnection : IDisposable
    {
        private IDbTransaction _transaction;
        private QueueWorker<WorkItem> _commandQueue;
        private int _pendingWorkItemCount;
        private List<Exception> _exceptions = new List<Exception>();
        private Dictionary<Type, IDisposable> _entityBatchers = new Dictionary<Type, IDisposable>();

        public SkydbConnection(IDbConnection connection)
        {
            Connection = connection;
            _commandQueue = new QueueWorker<WorkItem>(null, ExecuteWorkItem);
            _commandQueue.RunAsync(2, nameof(ExecuteWorkItem));
        }
        public IDbConnection Connection { get; }

        public void SetUnsafeJournalMode()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA synchronous = OFF";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA journal_mode = MEMORY";
                cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA automatic_indexing=OFF";
                // cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA cache_size=30000";
                // cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA temp_store=MEMORY";
                // cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA mmap_size=70368744177664";
                // cmd.ExecuteNonQuery();
            }
        }

        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException();
            }
            _transaction = Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            Flush();
            _transaction.Commit();
            _transaction = null;
        }

        public void Dispose()
        {
            Connection.Dispose();
            foreach (var batcher in _entityBatchers.Values)
            {
                batcher.Dispose();
            }

            _entityBatchers.Clear();
        }

        public void EnsureScores(IEnumerable<string> scoreNames)
        {
            var namesToAdd = scoreNames.Except(SqliteOperations.ListColumnNames(Connection, "Scores")).ToList();
            if (namesToAdd.Count == 0)
            {
                return;
            }
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = string.Join(Environment.NewLine, namesToAdd.Select(scoreName =>
                    "ALTER TABLE Scores ADD COLUMN " + SqliteOperations.QuoteIdentifier(scoreName) + " DOUBLE;"
                ));
                cmd.ExecuteNonQuery();
            }
        }

        public void QueueCommand(IDbCommand command, Queue<IDbCommand> pool)
        {
            CheckForExceptions();
            lock (this)
            {
                _commandQueue.Add(new WorkItem(command, pool));
                _pendingWorkItemCount++;
            }
        }

        public void Flush()
        {
            CheckForExceptions();
            lock (this)
            {
                while (_pendingWorkItemCount > 0)
                {
                    Monitor.Wait(this);
                }
            }
        }

        private class WorkItem
        {
            public WorkItem(IDbCommand command, Queue<IDbCommand> commandPool)
            {
                Command = command;
                CommandPool = commandPool;
            }

            public IDbCommand Command { get; }
            public Queue<IDbCommand> CommandPool { get; }
        }

        private void ExecuteWorkItem(WorkItem workItem, int threadIndex)
        {
            Exception exception = null;
            try
            {
                workItem.Command.ExecuteNonQuery();
                lock (workItem.CommandPool)
                {
                    workItem.CommandPool.Enqueue(workItem.Command);
                    Monitor.PulseAll(workItem.CommandPool);
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            lock (this)
            {
                if (exception != null)
                {
                    _exceptions.Add(exception);
                }
                _pendingWorkItemCount--;
                if (_pendingWorkItemCount == 0 || _exceptions.Count != 0)
                {
                    Monitor.PulseAll(this);
                }
            }
        }

        private void CheckForExceptions()
        {
            lock (this)
            {
                if (_exceptions.Count > 0)
                {
                    throw new AggregateException(_exceptions);
                }
            }
        }

        public void Insert<TEntity>(TEntity entity) where TEntity : Entity
        {
            GetBatcher<TEntity>().Insert(entity);
        }

        public EntityBatcher<TEntity> GetBatcher<TEntity>() where TEntity : Entity
        {
            lock (_entityBatchers)
            {
                if (_entityBatchers.TryGetValue(typeof(TEntity), out var batcher))
                {
                    return (EntityBatcher<TEntity>)batcher;
                }

                var entityBatcher = new EntityBatcher<TEntity>(this);
                _entityBatchers.Add(typeof(TEntity), entityBatcher);
                return entityBatcher;
            }
        }
    }
}
