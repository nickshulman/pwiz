using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Microsoft.VisualBasic.FileIO;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    [UsedImplicitly]
    public class ResultRow : Immutable
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string ProteinLocator { get; private set; }
        public string Protein { get; private set; }
        public string PeptideModifiedSequenceFullNames {get; private set; }
        public string ModifiedSequenceFullNames { get; private set; }
        public int PrecursorCharge { get; private set; }
        public double PrecursorMz { get; private set; }
        public string TransitionLocator { get; private set; }
        public int ProductCharge { get; private set; }
        public double ProductMz { get; private set; }
        public string FragmentIon { get; private set; }
        public string ReplicateLocator { get; private set; }
        public string Replicate { get; private set; }
        public string Condition { get; private set; }
        public double? TimePoint { get; private set; }
        public double Area { get; private set; }
        public bool Truncated { get; private set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public static IEnumerable<ResultRow> Read(TextFieldParser textFieldParser)
        {
            var columns = textFieldParser.ReadFields();
            var properties = typeof(ResultRow).GetProperties();
            int[] columnIndexes = new int[properties.Length];
            for (int iProperty = 0; iProperty < properties.Length; iProperty++)
            {
                columnIndexes[iProperty] = FindColumn(columns, properties[iProperty].Name);
            }
            string[] fields;
            while ((fields = textFieldParser.ReadFields()) != null)
            {
                var row = new ResultRow();
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
    }
}
