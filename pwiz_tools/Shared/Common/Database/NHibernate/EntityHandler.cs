using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using NHibernate.Criterion;
using NHibernate.Mapping;
using NHibernate.Metadata;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.Database.NHibernate
{
    public class EntityHandler
    {
        private static readonly Random _random = new Random((int) DateTime.UtcNow.Ticks);
        private readonly DisposableCollection<IDbCommand> _disposables = new DisposableCollection<IDbCommand>();
        private Queue<IDbCommand> _commandPool = new Queue<IDbCommand>();
        private int _unrealizedPoolCount;
        private readonly List<ImmutableList<object>> _queue = new List<ImmutableList<object>>();
        protected long _maxId;
        protected bool _foreignId;

        public EntityHandler(SessionQueue sessionQueue, Type entityType, DatabaseMetadata databaseMetadata)
        {
            SessionQueue = sessionQueue;
            EntityType = entityType;
            DatabaseMetadata = databaseMetadata;
            ClassMetadata = databaseMetadata.GetClassMetadata(entityType);
            PersistentClass = databaseMetadata.GetPersistentClass(entityType);
            BatchSize = 1;
            _unrealizedPoolCount = 20;
            _foreignId = (PersistentClass.Identifier as SimpleValue)?.IdentifierGeneratorStrategy == "foreign";
            if (!_foreignId)
            {
                _maxId = QueryMaxId() + _random.Next(0, 1000);
            }
            SessionQueue.ActionQueue.CancellationToken.Register(OnCancelled);
            IdColumnName = PersistentClass.IdentifierProperty.ColumnIterator.SingleOrDefault()?.Text;
            ColumnNames = ImmutableList.ValueOf(PersistentClass.PropertyIterator
                .Select(property => property.ColumnIterator.SingleOrDefault()?.Text)
                .Except(new[] { null, IdColumnName }).Prepend(IdColumnName));
        }

        public string IdColumnName { get; }
        public ImmutableList<string> ColumnNames { get; }

        private void OnCancelled()
        {
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }

        public DatabaseMetadata DatabaseMetadata { get; }
        public IClassMetadata ClassMetadata { get; }
        public PersistentClass PersistentClass { get; }


        public string TableName
        {
            get { return PersistentClass.Table.Name; }
        }
        public int BatchSize { get; private set; }


        [Localizable(false)]
        public string GetInsertSql(int batchSize)
        {
            var columnNamesString = string.Join(", ", ColumnNames);
            var paramsString = string.Join(", ", Enumerable.Repeat("?", ColumnNames.Count));
            var lines = new List<string>
            {
                "INSERT INTO " + QuoteIdentifier(TableName) + " (" + columnNamesString + ")",
                "SELECT " + string.Join(", ", ColumnNames.Select(name=>"? AS " + QuoteIdentifier(name)))
            };
            lines.AddRange(Enumerable.Repeat("UNION ALL SELECT " + paramsString, batchSize - 1));
            return CommonTextUtil.LineSeparate(lines);
        }

        public virtual void Insert(object entity)
        {
            if (!_foreignId)
            {
                SetId(entity, NextId());
            }

            var parameterValues = ImmutableList.ValueOf(GetParameterValues(entity));
            lock (_queue)
            {
                _queue.Add(parameterValues);
                ProcessQueue(false);
            }
        }

        protected void SetId(object entity, long id)
        {
            ClassMetadata.SetIdentifier(entity, id);
        }

        private void ProcessQueue(bool complete)
        {
            while (true)
            {
                SessionQueue.ActionQueue.CheckForExceptions();
                List<ImmutableList<object>> batch = null;
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
                QueueCommand(SessionQueue, command, queue, this);
            }
        }
        private IDbCommand GetPooledCommand()
        {
            lock (this)
            {
                while (_commandPool.Count == 0)
                {
                    SessionQueue.ActionQueue.CheckForExceptions();
                    if (_unrealizedPoolCount > 0)
                    {
                        _unrealizedPoolCount--;
                        return CreateCommand(BatchSize);
                    }
                    Monitor.Wait(this);
                }

                return _commandPool.Dequeue();
            }
        }

        private IDbCommand CreateCommand(int batchSize)
        {
            var insertCommand = Connection.CreateCommand();
            insertCommand.CommandText = GetInsertSql(batchSize);
            for (int batchIndex = 0; batchIndex < batchSize; batchIndex++)
            {
                foreach (var _ in PersistentClass.Table.ColumnIterator)
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

        private void FillInParameters(IDbCommand command, IList<ImmutableList<object>> entities)
        {
            int expectedParameterCount = ColumnNames.Count * entities.Count;
            if (expectedParameterCount != command.Parameters.Count)
            {
                string message = string.Format(@"Expected {0} parameters but found {1}", expectedParameterCount,
                    command.Parameters.Count);
                throw new InvalidOperationException(message);
            }
            for (int i = 0; i < entities.Count; i++)
            {
                var columnValues = entities[i];
                if (columnValues.Count != ColumnNames.Count)
                {
                    throw new InvalidOperationException(string.Format("Expected {0} parameters, actual {1}",
                        ColumnNames.Count, columnValues.Count));

                }
                for (int iParam = 0; iParam < columnValues.Count; iParam++)
                {
                    int paramIndex = i * ColumnNames.Count + iParam;
                    ((SQLiteParameter)command.Parameters[paramIndex]).Value = columnValues[iParam];
                }
            }
        }

        private IEnumerable<object> GetParameterValues(object entity)
        {
            var columnValues = GetColumnValues(entity);
            foreach (var columnName in ColumnNames)
            {
                if (columnName == IdColumnName)
                {
                    yield return ClassMetadata.GetIdentifier(entity);
                }
                else
                {
                    columnValues.TryGetValue(columnName, out var value);
                    yield return value;
                }
            }
        }

        private Dictionary<string, object> GetColumnValues(object entity)
        {
            return DatabaseMetadata.GetColumnValues(EntityType, entity);
        }

        private static void QueueCommand(SessionQueue sessionQueue, IDbCommand command, Queue<IDbCommand> pool, object sync)
        {
            sessionQueue.ActionQueue.CancellationToken.ThrowIfCancellationRequested();
            sessionQueue.Enqueue(() =>
            {
                command.ExecuteNonQuery();
                if (pool != null)
                {
                    lock (sync)
                    {
                        pool.Enqueue(command);
                        Monitor.PulseAll(sync);
                    }
                }
            }, GetDescription(command));
            sessionQueue.ActionQueue.CancellationToken.ThrowIfCancellationRequested();
        }

        private static string GetDescription(IDbCommand command)
        {
            var stringBuilder = new StringBuilder();
            int iParameter = 0;
            foreach (var part in command.CommandText.Split(new[] { '?' }))
            {
                stringBuilder.Append(part);
                if (iParameter == command.Parameters.Count)
                {
                    continue;
                }
                var value = ((IDbDataParameter) command.Parameters[iParameter++]).Value;
                if (value is string str)
                {
                    stringBuilder.Append("'" + str.Replace("'", "''") + "'");
                }
                else if (value == null)
                {
                    stringBuilder.Append("NULL");
                }
                else
                {
                    stringBuilder.Append(value);
                }
            }

            return stringBuilder.ToString();
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

        public Type EntityType
        {
            get;
        }

        public SessionQueue SessionQueue { get; }

        public IDbConnection Connection
        {
            get { return SessionQueue.Connection; }
        }

        public long MaxId
        {
            get;
            private set;
        }

        public void SetBatchSize(int batchSize)
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
            _disposables.Dispose();
        }

        public virtual void Flush()
        {
            ProcessQueue(true);
        }

        [Localizable(false)]
        public static string QuoteIdentifier(string str)
        {
            return "\"" + str.Replace("\"", "\"\"") + "\"";
        }
    }
}
