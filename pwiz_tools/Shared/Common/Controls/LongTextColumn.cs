using System.ComponentModel;
using System.Windows.Forms;

namespace pwiz.Common.Controls
{
    public class LongTextColumn : DataGridViewTextBoxColumn
    {
        public const int TRUNCATE_TEXT_LENGTH = 20000;
        public LongTextColumn()
        {
            CellTemplate = new LongTextCell();
        }

        public class LongTextCell : DataGridViewTextBoxCell
        {
            protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
                TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
            {
                var formattedValue = base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter,
                    formattedValueTypeConverter, context);
                var formattedValueString = formattedValue as string;
                if (formattedValueString == null || formattedValueString.Length < TRUNCATE_TEXT_LENGTH)
                {
                    return formattedValue;
                }
                return formattedValueString.Substring(0, TRUNCATE_TEXT_LENGTH) + "..."; // Not L10N
            }

            public override bool ReadOnly
            {
                get { return true; }
                set { base.ReadOnly = value; }
            }
        }
    }
}
