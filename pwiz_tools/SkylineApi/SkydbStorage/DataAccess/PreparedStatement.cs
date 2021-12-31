using System;
using System.Data;

namespace SkydbStorage.DataAccess
{
    public class PreparedStatement : IDisposable
    {
        protected IDbConnection Connection { get; }
        protected IDbCommand Command { get; set; }

        public PreparedStatement(IDbConnection connection)
        {
            Connection = connection;
            Command = connection.CreateCommand();
        }

        public virtual void Dispose()
        {
            if (Command != null)
            {
                Command.Dispose();
                Command = null;
            }
        }
    }
}
