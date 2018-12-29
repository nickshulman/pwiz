using System;
using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding.Controls;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Model.Sharing.GeneratedCode;

namespace pwiz.Skyline.Model.Sharing
{
    public class ReportExporter
    {
        private string _rowIdColumnName;
        public ReportExporter(string name, BindingListSource bindingListSource)
        {
            Name = name;
            BindingListSource = bindingListSource;
            var propertyDescriptors = new List<PropertyDescriptor>();
            var columnCaptions = new List<string>();
            var columnNameManager = new ColumnNameManager(true);
            _rowIdColumnName = columnNameManager.MakeUniqueName(@"RowId");
            foreach (var propertyDescriptor in bindingListSource.ItemProperties)
            {
                string originalColumnName = propertyDescriptor.ColumnCaption.GetCaption(DataSchemaLocalizer.INVARIANT);
                string columnName = columnNameManager.MakeUniqueName(originalColumnName);
                columnCaptions.Add(columnName);
                propertyDescriptors.Add(propertyDescriptor);
                if (typeof(ILocatable).IsAssignableFrom(propertyDescriptor.PropertyType))
                {
                    var locator = new LocatorPropertyDescriptor(columnName + @"_locator", propertyDescriptor);
                    string fkColumnName = columnNameManager.MakeUniqueName(locator.Name);
                    columnCaptions.Add(fkColumnName);
                    propertyDescriptors.Add(locator);
                }
            }
            PropertyDescriptors = ImmutableList.ValueOf(propertyDescriptors);
            ColumnNames = ImmutableList.ValueOf(columnCaptions);
            FormatProvider = CultureInfo.CurrentCulture;
            Language = CultureInfo.CurrentUICulture;
        }

        public string Name { get; private set; }
        public BindingListSource BindingListSource { get; private set; }
        public ImmutableList<PropertyDescriptor> PropertyDescriptors { get; private set; }
        public ImmutableList<string> ColumnNames { get; private set; }
        public CultureInfo FormatProvider { get; set; }
        public CultureInfo Language { get; set; }

        public TableType GetTableInfo(String runName)
        {
            TableType tableInfo = new TableType()
            {
                tableDbType = @"TABLE",
                tableTitle = Name,
                pkColumnName = _rowIdColumnName
            };
            if (string.IsNullOrEmpty(runName))
            {
                tableInfo.tableName = Name;
            }
            else
            {
                tableInfo.tableName = runName + '.' + Name;
            }

            tableInfo.columns = new TableTypeColumns();
            List<ColumnType> columns = new List<ColumnType>();
            ColumnType pkColumn = new ColumnType()
            {
                datatype = DataType.INTEGER.JdbcName,
                rangeURI = DataType.INTEGER.RangeUri,
                columnName = _rowIdColumnName,
                isKeyField = true,
                isKeyFieldSpecified = true,
                isAutoInc = true,
                isAutoIncSpecified = true,
                nullable = false,
                nullableSpecified = true,
            };
            columns.Add(pkColumn);
            tableInfo.columns.column = new ColumnType[PropertyDescriptors.Count];
            for (int icol = 0; icol < PropertyDescriptors.Count; icol++)
            {
                var pd = PropertyDescriptors[icol];
                var column = GetColumnInfo(pd);
                column.columnName = ColumnNames[icol];
                if (pd is LocatorPropertyDescriptor)
                {
                    column.isHidden = true;
                    column.isHiddenSpecified = true;
                    var previousColumn = columns[columns.Count - 1];
                    string url = @"/TargetedMS/showElement.view?locator=${" + column.columnName + @"}";
                    if (!string.IsNullOrEmpty(runName))
                    {
                        url += @"&runName=" + Uri.EscapeDataString(runName);
                    }
                    previousColumn.url = new StringExpressionType
                    {
                        Value = url
                    };
                }
                columns.Add(column);
            }

            tableInfo.columns.column = columns.ToArray();
            return tableInfo;
        }

        public DsvWriter GetDsvWriter(char separator)
        {
            return new SharingDsvWriter(separator, FormatProvider, Language)
            {
                ColumnCaptions = ImmutableList.ValueOf(new[]{_rowIdColumnName}.Concat(ColumnNames)),
                PropertyDescriptors = PropertyDescriptors,
                IncludeRowIndex = true
            };
        }

        public void WriteReport(IProgressMonitor monitor, TextWriter textWriter, char separator)
        {
            var dsvWriter = GetDsvWriter(separator);
            var skylineViewContext = (SharingViewContext) BindingListSource.ViewContext;
            skylineViewContext.WriteToStream(monitor, BindingListSource, dsvWriter, textWriter);
        }

        private ColumnType GetColumnInfo(PropertyDescriptor propertyDescriptor)
        {
            var columnInfo = new ColumnType();
            DataType dataType = DataType.GetDataType(propertyDescriptor.PropertyType);
            columnInfo.datatype = dataType.JdbcName;
            columnInfo.rangeURI = dataType.RangeUri;
            columnInfo.nullable = true;
            columnInfo.nullableSpecified = true;
            columnInfo.columnTitle = propertyDescriptor.DisplayName;
            var formatAttribute = (FormatAttribute)propertyDescriptor.Attributes[typeof(FormatAttribute)];
            if (formatAttribute != null)
            {
                columnInfo.formatString = formatAttribute.Format;
            }
            return columnInfo;
        }
    }
}
