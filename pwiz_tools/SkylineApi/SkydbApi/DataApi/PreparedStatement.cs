using System;
using System.Collections.Generic;
using System.Data;

namespace SkydbApi.DataApi
{
    public class PreparedStatement : IDisposable
    {
        private List<IDbCommand> _commands = new List<IDbCommand>();
        private static HashSet<PreparedStatement> _statements = new HashSet<PreparedStatement>();
        public PreparedStatement(IDbConnection connection)
        {
            Connection = connection;
            _statements.Add(this);
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

            _statements.Remove(this);
        }

        public static void DumpStatements()
        {
            foreach (var statement in _statements)
            {
                Console.Out.WriteLine(statement);
            }
        }
    }
}
