/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2018 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace SkydbApi.DataApi
{
    public static class SqliteOperations
    {
        public static bool TableExists(IDbConnection connection, string tableName)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT 1 FROM sqlite_master WHERE type='table' AND name=?";
                cmd.Parameters.Add(new SQLiteParameter { Value = tableName });
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        /// <summary>
        /// Returns a ConnectionStringBuilder with the datasource set to the specified path.  This method takes
        /// care of the special settings needed to work with UNC paths.
        /// </summary>
        public static SQLiteConnectionStringBuilder MakeConnectionStringBuilder(string path)
        {
            // when SQLite parses the connection string, it treats backslash as an escape character
            // This is not normally an issue, because backslashes followed by a non-reserved character
            // are not treated specially.

            // Also, in order to prevent a drive letter being prepended to UNC paths, we specify ToFullPath=false
            return new SQLiteConnectionStringBuilder
            {
                // ReSharper disable LocalizableElement
                DataSource = path.Replace("\\", "\\\\"),
                // ReSharper restore LocalizableElement
                ToFullPath = false,
            };
        }


        public static bool ColumnExists(IDbConnection connection, string tableName, string columnName)
        {
            return ListColumnNames(connection, tableName).Contains(columnName);
        }

        public static IEnumerable<string> ListColumnNames(IDbConnection connection, string tableName)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"PRAGMA table_info(" + QuoteIdentifier(tableName) + ")";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(1);
                    }
                }
            }
        }

        public static string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
