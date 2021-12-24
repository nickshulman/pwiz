using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertMsDataFileStatement : IDisposable
    {
        private static string COMMAND_TEXT = "INSERT INTO MsDataFile(FilePath) "
                                             + "VALUES(?); select last_insert_rowid();";

        private SQLiteParameter filePath;

        private IDbCommand Command { get; }

        public InsertMsDataFileStatement(IDbConnection connection)
        {
            Command = connection.CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(filePath = new SQLiteParameter());
        }

        public void Dispose()
        {
            Command.Dispose();
        }

        public void Insert(MsDataFile msDataFile)
        {
            filePath.Value = msDataFile.FilePath;
            msDataFile.Id = Convert.ToInt64(Command.ExecuteScalar());
        }
    }
}
