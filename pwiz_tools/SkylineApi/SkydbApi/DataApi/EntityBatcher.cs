using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class EntityBatcher<TEntity> : IDisposable where TEntity: Entity
    {
        private long _maxId;
        private Queue<IDbCommand> _commandPool = new Queue<IDbCommand>();

        public EntityBatcher(SkydbConnection connection)
        {
            Connection = connection;
            Inserter = new ReflectedEntityInserter<TEntity>();
            using var cmd = connection.Connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(Id), 0) FROM " + Inserter.TableName;
            _maxId = Convert.ToInt64(cmd.ExecuteScalar());
            for (int i = 0; i < 10; i++)
            {
                var insertCommand = connection.Connection.CreateCommand();
                insertCommand.CommandText = Inserter.GetInsertSql(1, true);
                insertCommand.Parameters.Add(new SQLiteParameter());
                foreach (var _ in Inserter.GetColumnNames())
                {
                    insertCommand.Parameters.Add(new SQLiteParameter());
                }

                _commandPool.Enqueue(insertCommand);
            }
        }

        public EntityInserter<TEntity> Inserter { get; }

        public SkydbConnection Connection { get; }
        public void Insert(TEntity entity)
        {
            entity.Id = Interlocked.Increment(ref _maxId);
            var command = GetPooledCommand();
            ((SQLiteParameter) command.Parameters[0]).Value= entity.Id;
            int paramNumber = 0;
            foreach (var param in Inserter.GetColumnValues(entity))
            {
                paramNumber++;
                ((SQLiteParameter)command.Parameters[paramNumber]).Value = param;
            }
            Connection.QueueCommand(command, _commandPool);
        }

        private IDbCommand GetPooledCommand()
        {
            lock (_commandPool)
            {
                while (_commandPool.Count == 0)
                {
                    Monitor.Wait(_commandPool);
                }

                return _commandPool.Dequeue();
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
