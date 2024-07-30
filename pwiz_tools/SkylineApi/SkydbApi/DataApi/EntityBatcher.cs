using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public interface IBatcher : IDisposable
    {
        void Flush();
    }

    public class EntityBatcher<TEntity> : IBatcher where TEntity: Entity
    {
        private long _maxId;
        private int _unpooled = 8;
        private readonly int _batchSize;
        private Queue<IDbCommand> _commandPool = new Queue<IDbCommand>();
        private List<TEntity> _queue = new List<TEntity>();

        public EntityBatcher(SkydbConnection connection)
        {
            Connection = connection;
            InsertSql = new ReflectedEntityInsertSql<TEntity>();
            _batchSize = Math.Max(1, 256 / (InsertSql.GetColumnNames().Count + 1));
            using var cmd = connection.Connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(Id), 0) FROM " + InsertSql.TableName;
            _maxId = Convert.ToInt64(cmd.ExecuteScalar());
        }

        public EntityInsertSql<TEntity> InsertSql { get; }

        public SkydbConnection Connection { get; }
        public void Insert(TEntity entity)
        {
            entity.Id = Interlocked.Increment(ref _maxId);
            lock (_queue)
            {
                _queue.Add(entity);
            }
            ProcessQueue(false);
        }

        public void Flush()
        {
            ProcessQueue(true);
        }

        private IDbCommand GetPooledCommand()
        {
            lock (_commandPool)
            {
                while (_commandPool.Count == 0)
                {
                    if (_unpooled > 0)
                    {
                        _unpooled--;
                        return CreateCommand(_batchSize);
                    }
                    Monitor.Wait(_commandPool);
                }

                return _commandPool.Dequeue();
            }
        }

        private IDbCommand CreateCommand(int batchSize)
        {
            var insertCommand = Connection.Connection.CreateCommand();
            insertCommand.CommandText = InsertSql.GetInsertSql(batchSize, true);
            for (int batchIndex = 0; batchIndex < batchSize; batchIndex++)
            {
                insertCommand.Parameters.Add(new SQLiteParameter());
                foreach (var _ in InsertSql.GetColumnNames())
                {
                    insertCommand.Parameters.Add(new SQLiteParameter());
                }
            }

            return insertCommand;
        }

        private void FillInParameters(IDbCommand command, IList<TEntity> entities)
        {
            int paramIndex = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                ((SQLiteParameter)command.Parameters[paramIndex++]).Value = entity.Id;
                foreach (var value in InsertSql.GetColumnValues(entity))
                {
                    ((SQLiteParameter)command.Parameters[paramIndex++]).Value = value;
                }
            }
        }

        private void ProcessQueue(bool complete)
        {
            while (true)
            {
                List<TEntity> batch = null;
                lock (_queue)
                {
                    if (_queue.Count == 0)
                    {
                        return;
                    }

                    if (_queue.Count >= _batchSize || complete)
                    {
                        batch = _queue.Take(_batchSize).ToList();
                        _queue.RemoveRange(0, batch.Count);
                    }
                }

                if (batch == null || batch.Count == 0)
                {
                    return;
                }

                IDbCommand command;
                Queue<IDbCommand> queue;
                if (batch.Count == _batchSize)
                {
                    command = GetPooledCommand();
                    queue = _commandPool;
                }
                else
                {
                    command = CreateCommand(batch.Count);
                    queue = null;
                }
                FillInParameters(command, batch);
                Connection.QueueCommand(command, queue);
            }
        }

        public void Dispose()
        {
            while (_commandPool.Count > 0)
            {
                var command = _commandPool.Dequeue();
                command.Dispose();
            }
        }
    }
}
