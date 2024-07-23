using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace SkydbApi.DataApi
{
    public class PreparedStatement : IDisposable
    {
        private List<IDbCommand> _commands = new List<IDbCommand>();
        private static ConcurrentDictionary<PreparedStatement, bool> _statements = new ConcurrentDictionary<PreparedStatement, bool>();
        public PreparedStatement(IDbConnection connection)
        {
            Connection = connection;
            _statements[this] = true;
        }

        protected IDbConnection Connection { get; }

        protected IDbCommand CreateCommand()
        {
            var command = Connection.CreateCommand();
            _commands.Add(command);
            return command;
        }

        public void Dispose()
        {
            foreach (var command in _commands)
            {
                command.Dispose();
            }

            _statements.TryRemove(this, out _);
        }

        public static void DumpStatements()
        {
            foreach (var statement in _statements.Keys)
            {
                Console.Out.WriteLine(statement);
            }
        }
    }
}
