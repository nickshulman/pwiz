using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using NHibernate.Criterion;
using NHibernate.Metadata;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.Database.NHibernate
{
    public class DefaultEntityHandler : EntityHandler
    {
        private readonly DisposableCollection<IDbCommand> _disposables = new DisposableCollection<IDbCommand>();
        private Queue<IDbCommand> _commandPool = new Queue<IDbCommand>();
        private int _unrealizedPoolCount;
        private readonly List<object> _queue = new List<object>();

        public DefaultEntityHandler(SessionQueue sessionQueue, IClassMetadata classMetadata) : base(sessionQueue)
        {
            ClassMetadata = classMetadata;
            BatchSize = 1;
            _unrealizedPoolCount = 8;
            _maxId = QueryMaxId();
        }

        public IClassMetadata ClassMetadata { get; }
        public int BatchSize { get; private set; }


        [Localizable(false)]
        public string GetInsertSql(int batchSize, bool includeId)
        {
            List<string> columnNames = ClassMetadata.PropertyNames.ToList();
            if (includeId)
            {
                columnNames.Insert(0, ClassMetadata.IdentifierPropertyName);
            }

            var columnNamesString = string.Join(", ", columnNames);
            var paramsString = string.Join(", ", Enumerable.Repeat("?", columnNames.Count));
            var lines = new List<string>
            {
                "INSERT INTO " + QuoteIdentifier(ClassMetadata.EntityName) + " (" + columnNamesString + ")",
                "SELECT (" + string.Join(", ", columnNames.Select(name=>"? AS " + QuoteIdentifier(name))) + ")"
            };
            lines.AddRange(Enumerable.Repeat("UNION ALL SELECT (" + paramsString + ")", batchSize - 1));
            return CommonTextUtil.LineSeparate(lines);
        }

        public override void Insert(object entity)
        {
            SetId(entity, NextId());
            lock (_queue)
            {
                _queue.Add(entity);
            }
            ProcessQueue(false);
        }

        protected override void SetId(object entity, long id)
        {
            ClassMetadata.SetIdentifier(entity, id);
        }

        private void ProcessQueue(bool complete)
        {
            while (true)
            {
                List<object> batch = null;
                lock (_queue)
                {
                    if (_queue.Count == 0)
                    {
                        return;
                    }

                    if (_queue.Count >= BatchSize || complete)
                    {
                        batch = _queue.Take(BatchSize).ToList();
                        _queue.RemoveRange(0, batch.Count);
                    }
                }

                if (batch == null || batch.Count == 0)
                {
                    return;
                }

                IDbCommand command;
                Queue<IDbCommand> queue;
                if (batch.Count == BatchSize)
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
                QueueCommand(SessionQueue, command, queue);
            }
        }
        private IDbCommand GetPooledCommand()
        {
            lock (_commandPool)
            {
                while (_commandPool.Count == 0)
                {
                    if (_unrealizedPoolCount > 0)
                    {
                        _unrealizedPoolCount--;
                        return CreateCommand(BatchSize);
                    }
                    Monitor.Wait(_commandPool);
                }

                return _commandPool.Dequeue();
            }
        }

        private IDbCommand CreateCommand(int batchSize)
        {
            var insertCommand = Connection.CreateCommand();
            insertCommand.CommandText = GetInsertSql(batchSize, true);
            for (int batchIndex = 0; batchIndex < batchSize; batchIndex++)
            {
                insertCommand.Parameters.Add(new SQLiteParameter());
                foreach (var _ in ClassMetadata.PropertyNames)
                {
                    insertCommand.Parameters.Add(new SQLiteParameter());
                }
            }
            lock (_disposables)
            {
                _disposables.Add(insertCommand);
            }
            return insertCommand;
        }

        private void FillInParameters(IDbCommand command, IList<object> entities)
        {
            int paramIndex = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                ((SQLiteParameter)command.Parameters[paramIndex++]).Value = ClassMetadata.GetIdentifier(entity);
                foreach (var value in ClassMetadata.GetPropertyValues(entity))
                {
                    ((SQLiteParameter)command.Parameters[paramIndex++]).Value = value;
                }
            }
        }

        private static void QueueCommand(SessionQueue sessionQueue, IDbCommand command, Queue<IDbCommand> pool)
        {
            sessionQueue.Enqueue(() =>
            {
                command.ExecuteNonQuery();
                if (pool != null)
                {
                    lock (pool)
                    {
                        pool.Enqueue(command);
                        Monitor.PulseAll(pool);
                    }
                }
            });
        }

        public override void Dispose()
        {
            _disposables.Dispose();
            base.Dispose();
        }

        public override void Flush()
        {
            ProcessQueue(true);
            base.Flush();
        }

        private long QueryMaxId()
        {
            object id = SessionQueue.CreateCriteria(ClassMetadata.MappedClass)
                .SetProjection(Projections.Max(ClassMetadata.IdentifierPropertyName)).UniqueResult();
            if (id == null)
            {
                return 0;
            }

            return Convert.ToInt64(id);
        }

        public override Type EntityType
        {
            get { return ClassMetadata.MappedClass; }
        }

        public override void SetBatchSize(int batchSize)
        {
            lock (_commandPool)
            {
                if (BatchSize == batchSize)
                {
                    return;
                }
                BatchSize = batchSize;
                _commandPool.Clear();
            }
        }
    }
}
