using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertMsDataFileStatement : PreparedStatement
    {
        private static string COMMAND_TEXT = "INSERT INTO MsDataFile(FilePath) "
                                             + "VALUES(?); select last_insert_rowid();";

        private SqliteParameter filePath;

        private IDbCommand Command { get; }

        public InsertMsDataFileStatement(IDbConnection connection) : base(connection)
        {
            Command = CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(filePath = new SqliteParameter());
        }

        public void Insert(MsDataFile msDataFile)
        {
            filePath.Value = msDataFile.FilePath;
            msDataFile.Id = Convert.ToInt64(Command.ExecuteScalar());
        }
    }
}
