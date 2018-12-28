using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding.Controls;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
        private TableType _tableInfo;
        public ReportExporter(string name, BindingListSource bindingListSource)
        {
            Name = name;
            var columnInfos = new List<ColumnType>();
            BindingListSource = bindingListSource;
            var propertyDescriptors = new List<PropertyDescriptor>();
            var columnCaptions = new List<string>();
            var columnNameManager = new ColumnNameManager(true);
            foreach (var propertyDescriptor in bindingListSource.ItemProperties)
            {
                string originalColumnName = propertyDescriptor.ColumnCaption.GetCaption(DataSchemaLocalizer.INVARIANT);
                string columnName = columnNameManager.MakeUniqueName(originalColumnName);
                columnCaptions.Add(columnName);
                propertyDescriptors.Add(propertyDescriptor);
                var columnInfo = GetColumnInfo(propertyDescriptor);
                columnInfo.columnName = columnName;
                columnInfos.Add(columnInfo);
                if (typeof(ILocatable).IsAssignableFrom(propertyDescriptor.PropertyType))
                {
                    var locator = new LocatorPropertyDescriptor(columnName + @"_locator", propertyDescriptor);
                    string fkColumnName = columnNameManager.MakeUniqueName(locator.Name);
                    columnCaptions.Add(fkColumnName);
                    propertyDescriptors.Add(locator);
                    var fk = new ColumnTypeFK();
                    fk.fkColumnName = fkColumnName;
                    columnInfo.fk = fk;
                    var fkColumnInfo = GetColumnInfo(locator);
                    fkColumnInfo.isHidden = true;
                    fkColumnInfo.isHiddenSpecified = true;
                    fkColumnInfo.columnName = fkColumnName;
                    columnInfos.Add(fkColumnInfo);
                }
            }
            PropertyDescriptors = ImmutableList.ValueOf(propertyDescriptors);
            ColumnNames = ImmutableList.ValueOf(columnCaptions);
            var tableInfo = new TableType();
            tableInfo.tableName = Name;
            tableInfo.columns = new TableTypeColumns();
            tableInfo.columns.column = columnInfos.ToArray();
            _tableInfo = tableInfo;
        }

        public string Name { get; private set; }
        public BindingListSource BindingListSource { get; private set; }
        public ImmutableList<PropertyDescriptor> PropertyDescriptors { get; private set; }
        public ImmutableList<string> ColumnNames { get; private set; }
        public CultureInfo FormatProvider { get; set; }
        public CultureInfo Language { get; set; }

        public TableType TableInfo 
        {
            get { return _tableInfo; }
        }

        public DsvWriter GetDsvWriter(char separator)
        {
            return new SharingDsvWriter(separator, FormatProvider, Language)
            {
                ColumnCaptions = ColumnNames,
                PropertyDescriptors = PropertyDescriptors
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
            columnInfo.datatype = propertyDescriptor.PropertyType.FullName;
            columnInfo.nullable = true;
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
