﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using pwiz.Common.DataBinding;

namespace pwiz.Skyline.Model.Sharing
{
    public class SharingDsvWriter : DsvWriter
    {
        public SharingDsvWriter(char separator) : base(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, separator)
        {

        }

        public IList<PropertyDescriptor> PropertyDescriptors { get; set; }
        public IList<String> ColumnCaptions { get; set; }

        public override void WriteHeaderRow(TextWriter writer, IEnumerable<PropertyDescriptor> propertyDescriptors)
        {
            if (ColumnCaptions == null)
            {
                base.WriteHeaderRow(writer, PropertyDescriptors ?? propertyDescriptors);
                return;
            }
            bool first = true;
            foreach (string columnCaption in ColumnCaptions)
            {
                if (!first)
                {
                    writer.Write(Separator);
                }
                first = false;
                writer.Write(ToDsvField(columnCaption));
            }
            writer.WriteLine();
        }

        public override void WriteDataRow(TextWriter writer, RowItem rowItem, IEnumerable<PropertyDescriptor> propertyDescriptors)
        {
            base.WriteDataRow(writer, rowItem, PropertyDescriptors ?? propertyDescriptors);
        }
    }
}
