using System;
using System.Collections.Generic;
using System.Data;

namespace SkydbApi.DataApi
{
    public class PreparedStatement : IDisposable
    {
        private List<IDbCommand> _commands = new List<IDbCommand>();
        public PreparedStatement(IDbConnection connection)
        {
            Connection = connection;
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
        }
    }
}
