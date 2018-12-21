using System;
using System.Collections.Generic;
using System.Globalization;

namespace pwiz.Skyline.Model.Sharing
{
    public class ColumnNameManager
    {
        private HashSet<string> _names = new HashSet<string>();
        public ColumnNameManager(bool caseSensitive)
        {
            CaseSensitive = caseSensitive;
            MaxLength = 63;
        }

        public bool CaseSensitive { get; private set; }
        public int MaxLength { get; set; }

        public string MakeUniqueName(string originalName)
        {
            string columnName = originalName;
            if (columnName.Length > MaxLength)
            {
                columnName = columnName.Substring(0, MaxLength);
            }

            for (int index = 1; !AddColumnName(columnName); index++)
            {
                string suffix = index.ToString(CultureInfo.InvariantCulture);
                if (suffix.Length > MaxLength)
                {
                    throw new InvalidOperationException(@"Unable to find valid column name for " + originalName);
                }
                columnName = originalName;
                if (columnName.Length > MaxLength - suffix.Length)
                {
                    columnName = columnName.Substring(0, MaxLength - suffix.Length);
                }

                columnName = columnName + suffix;
            }

            return columnName;
        }

        public bool AddColumnName(string columnName)
        {
            if (!CaseSensitive)
            {
                columnName = columnName.ToLowerInvariant();
            }

            return _names.Add(columnName);
        }
    }
}
