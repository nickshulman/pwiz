using System;
using System.Data;

namespace SkydbStorage.Internal
{
    public class PreparedStatement : IDisposable
    {
        protected IDbCommand Command { get; set; }

        public PreparedStatement(IDbConnection connection)
        {
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
