using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace ImportExplicitPeakBoundsBlib
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: {0} <blibfile> <tsvfile>", Environment.GetCommandLineArgs()[0]);
                Console.Error.WriteLine("Reads peak boundaries from tsvfile and updates blib file");
                return;
            }

            var connectionBuilder = new SQLiteConnectionStringBuilder()
            {
                DataSource = args[0]
            };
            using var connection = new SQLiteConnection(connectionBuilder.ToString());
            using TextFieldParser textFieldParser = new TextFieldParser(args[1]);
            textFieldParser.SetDelimiters("\t");
            var firstLine = textFieldParser.ReadFields();
            int icolModifiedSequence = FindField(firstLine, "PeptideModifiedSequence");
            int icolMinStartTime = FindField(firstLine, "MinStartTime");
            int icolMaxEndTime = FindField(firstLine, "MaxEndTime");
            int icolFileName = FindField(firstLine, "FileName");
            if (icolModifiedSequence < 0 || icolMinStartTime < 0 || icolMaxEndTime < 0 || icolFileName < 0)
            {
                return;
            }
            connection.Open();
            var transaction = connection.BeginTransaction();
            Console.Error.WriteLine("Creating Retention Times Index");
            ExecuteCommand(connection, "CREATE INDEX MyIndex ON RetentionTimes (RefSpectraID, SpectrumSourceID)");
            using var cmdUpdate = connection.CreateCommand();
            cmdUpdate.CommandText = "UPDATE RetentionTimes SET startTime = ?, endTime = ? WHERE RefSpectraID = ? AND SpectrumSourceID = ?";
            cmdUpdate.Parameters.Add(new SQLiteParameter(DbType.Double));
            cmdUpdate.Parameters.Add(new SQLiteParameter(DbType.Double));
            cmdUpdate.Parameters.Add(new SQLiteParameter(DbType.Int32));
            cmdUpdate.Parameters.Add(new SQLiteParameter(DbType.Int32));
            using var cmdInsert = connection.CreateCommand();
            cmdInsert.CommandText =
                "INSERT INTO RetentionTimes (RefSpectraID, SpectrumSourceID, StartTime, EndTime, RetentionTime) VALUES(?,?,?,?,?)";
            cmdInsert.Parameters.Add(new SQLiteParameter(DbType.Int32));
            cmdInsert.Parameters.Add(new SQLiteParameter(DbType.Int32));
            cmdInsert.Parameters.Add(new SQLiteParameter(DbType.Double));
            cmdInsert.Parameters.Add(new SQLiteParameter(DbType.Double));
            cmdInsert.Parameters.Add(new SQLiteParameter(DbType.Double));

            var fileIds = ReadFileIds(connection);
            Console.Error.WriteLine("Reading RefSpectra table");
            var spectraIds = ReadRefSpectraIds(connection).ToLookup(kvp => kvp.Key, kvp => kvp.Value);
            string[] fields;
            int modifiedCount = 0;
            int insertedCount = 0;
            int lineCount = 0;
            while (null != (fields = textFieldParser.ReadFields()))
            {
                var fileName = Path.GetFileNameWithoutExtension(fields[icolFileName]);
                if (!fileIds.TryGetValue(fileName, out int fileId))
                {
                    Console.Error.WriteLine("Unable to find file '{0}", fileName);
                    return;
                }

                var peptideModSeq = fields[icolModifiedSequence];
                double startTime = double.Parse(fields[icolMinStartTime]);
                double endTime = double.Parse(fields[icolMaxEndTime]);
                cmdUpdate.Parameters[0].Value = startTime;
                cmdUpdate.Parameters[1].Value = endTime;
                cmdUpdate.Parameters[3].Value = fileId;
                foreach (var refSpectraId in spectraIds[peptideModSeq])
                {
                    cmdUpdate.Parameters[2].Value = refSpectraId;
                    int result = cmdUpdate.ExecuteNonQuery();
                    if (result == 0)
                    {
                        cmdInsert.Parameters[0].Value = refSpectraId;
                        cmdInsert.Parameters[1].Value = fileId;
                        cmdInsert.Parameters[2].Value = startTime;
                        cmdInsert.Parameters[3].Value = endTime;
                        cmdInsert.Parameters[4].Value = (startTime + endTime) / 2;
                        insertedCount += cmdInsert.ExecuteNonQuery();
                    }
                    else
                    {
                        modifiedCount += result;
                    }
                }
                lineCount++;

                if (0 == textFieldParser.LineNumber % 100000)
                {
                    Console.Out.WriteLine("Processed {0} lines", textFieldParser.LineNumber);
                }
            }

            Console.Error.WriteLine("Ensuring retentionTime is between startTime and endTime");
            ExecuteCommand(connection,
                "UPDATE RetentionTimes SET retentionTime = Min(Max(retentionTime, startTime), endTime)");
            Console.Error.WriteLine("Dropping retention times index");
            ExecuteCommand(connection, "DROP INDEX MyIndex");
            Console.Error.WriteLine("Committing Transaction");
            transaction.Commit();
            Console.Error.WriteLine("Updated {0} and added {1} peak boundaries from {2} lines", modifiedCount, insertedCount, lineCount);
            Console.Error.WriteLine("Press <enter> to continue");
            Console.ReadLine();
        }

        private static Dictionary<string, int> ReadFileIds(SQLiteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            
            cmd.CommandText = "SELECT id, filename from SpectrumSourceFiles";
            var rs = cmd.ExecuteReader();
            var dictionary = new Dictionary<string, int>();
            while (rs.Read())
            {
                dictionary.Add(Path.GetFileNameWithoutExtension(rs.GetString(1)), rs.GetInt32(0));
            }

            return dictionary;
        }

        private static IEnumerable<KeyValuePair<string, int>> ReadRefSpectraIds(SQLiteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, peptidemodseq from RefSpectra";
            var rs = cmd.ExecuteReader();
            while (rs.Read())
            {
                yield return new KeyValuePair<string, int>(rs.GetString(1), rs.GetInt32(0));
            }
        }


        private static int FindField(IList<string> fields, string name)
        {
            int index = fields.IndexOf(name);
            if (index < 0)
            {
                Console.Out.WriteLine("Unable to find '{0}' in fields '{1}'", name, string.Join("','", fields));
            }

            return index;
        }

        private static void CreateRetentionTimesIndex(SQLiteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE INDEX MyIndex ON RetentionTimes (RefSpectraID, SpectrumSourceID)";
            cmd.ExecuteNonQuery();
        }

        private static void DropRetentionTimesIndex(SQLiteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DROP INDEX MyIndex";
            cmd.ExecuteNonQuery();
        }

        private static int ExecuteCommand(SQLiteConnection connection, string commandText)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            return cmd.ExecuteNonQuery();
        }
    }
}
