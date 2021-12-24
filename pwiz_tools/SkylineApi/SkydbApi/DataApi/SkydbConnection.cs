using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.DataApi
{
    public class SkydbConnection : IDisposable
    {
        private List<IDisposable> _statements = new List<IDisposable>();
        protected SkydbConnection(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; }
        public virtual void Dispose()
        {
            Connection.Dispose();
            foreach (var disposable in _statements)
            {
                disposable.Dispose();
            }
        }

        protected T RememberDisposable<T>(T disposable) where T : IDisposable
        {
            _statements.Add(disposable);
            return disposable;
        }

    }
}
