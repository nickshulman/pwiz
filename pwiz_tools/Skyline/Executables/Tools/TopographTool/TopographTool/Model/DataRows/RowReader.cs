using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using pwiz.Common.Chemistry;

namespace TopographTool.Model.DataRows
{
    public static class RowReader
    {
        public static IEnumerable<T> Read<T>(TextFieldParser textFieldParser) where T : new()
        {
            var columns = textFieldParser.ReadFields();
            var properties = typeof(T).GetProperties();
            int[] columnIndexes = new int[properties.Length];
            for (int iProperty = 0; iProperty < properties.Length; iProperty++)
            {
                columnIndexes[iProperty] = FindColumn(columns, properties[iProperty].Name);
            }
            string[] fields;
            while ((fields = textFieldParser.ReadFields()) != null)
            {
                var row = new T();
                for (int iProperty = 0; iProperty < properties.Length; iProperty++)
                {
                    var property = properties[iProperty];
                    var value = fields[columnIndexes[iProperty]];
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }
                    var targetType = property.PropertyType;
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        targetType = targetType.GetGenericArguments()[0];
                        if (targetType == typeof(double) || targetType == typeof(int) && value == "#N/A")
                        {
                            continue;
                        }
                    }
                    property.SetValue(row, Convert.ChangeType(value, targetType));
                }
                yield return row;
            }
        }

        public static int FindColumn(IList<string> columnNames, string columnName)
        {
            int icol = columnNames.IndexOf(columnName);
            if (icol < 0)
            {
                throw new InvalidDataException(string.Format("Missing column {0}", columnName));
            }
            return icol;
        }

        public static IEnumerable<T> Read<T>(string path) where T : new()
        {
            using (var parser = new TextFieldParser(path))
            {
                parser.SetDelimiters(",");
                return Read<T>(parser).ToArray();
            }
        }

        public static IEnumerable<double> ParseDoubles(string s)
        {
            return s.Split(',').Select(double.Parse);
        }

        public static IEnumerable<int> ParseIntegers(string s)
        {
            return s.Split(',').Select(int.Parse);
        }

        public static IEnumerable<IList<double>> ParseDoubleArrays(string str)
        {
            List<double> curList = null;
            foreach (var strField in str.Split(','))
            {
                int numStart = 0;
                bool endsWithBracket = false;
                if (strField.StartsWith("["))
                {
                    curList = new List<double>();
                    numStart = 1;
                }
                endsWithBracket = strField.EndsWith("]");
                string strNum = strField.Substring(numStart, strField.Length - numStart - (endsWithBracket ? 1 : 0));
                double number = double.Parse(strNum);
                curList.Add(number);
                if (endsWithBracket)
                {
                    yield return curList;
                    curList = null;
                }
            }
        }

        public static MassDistribution ParseMassDistribution(string strMzs, string strAbundances)
        {
            var entries = new List<KeyValuePair<double, double>>();
            var mzs = ParseDoubles(strMzs).ToArray();
            var abundances = ParseDoubles(strAbundances).ToArray();
            return MassDistribution.NewInstance(Enumerable.Range(0, mzs.Length)
                    .Select(i => new KeyValuePair<double, double>(mzs[i], abundances[i])),
                .001, .0001);
        }
    }
}
