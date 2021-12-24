using System.Collections.Generic;
using System.Data;

namespace SkydbApi.DataApi
{
    public class SkydbReader : SkydbConnection
    {
        public SkydbReader(IDbConnection connection) : base(connection)
        {
        }

        public IDictionary<string, long> GetTableSizes()
        {
            var result = new Dictionary<string, long>();
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "select name, sum(pgsize) from dbstat group by name;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.NextResult())
                    {
                        result.Add(reader.GetString(0), reader.GetInt64(1));
                    }
                }
            }

            return result;
        }
    }
}
