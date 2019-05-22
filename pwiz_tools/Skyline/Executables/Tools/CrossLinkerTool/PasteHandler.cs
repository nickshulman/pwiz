using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CrossLinkerTool
{
    public class PasteHandler
    {
        private PasteHandler(DataGridView dataGridView)
        {
            DataGridView = dataGridView;
            DataGridView.KeyDown += DataGridViewOnKeyDown;
        }

        /// <summary>
        /// Attaches a DataGridViewPasteHandler to the specified DataGridView.
        /// </summary>
        public static PasteHandler Attach(DataGridView dataGridView)
        {
            return new PasteHandler(dataGridView);
        }

        public DataGridView DataGridView { get; private set; }

        private void DataGridViewOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            if (DataGridView.IsCurrentCellInEditMode && !(DataGridView.CurrentCell is DataGridViewCheckBoxCell))
            {
                return;
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V)
            {
                var clipboardText = Clipboard.GetText();
                if (string.IsNullOrEmpty(clipboardText))
                {
                    return;
                }
                using (var reader = new StringReader(clipboardText))
                {
                    if (Paste(reader))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Pastes tab delimited data into rows and columns starting from the current cell.
        /// If an error is encountered (e.g. type conversion), then a message is displayed,
        /// and the focus is left in the cell which had an error.
        /// Returns true if any changes were made to the document, false if there were no
        /// changes.
        /// </summary>
        private bool Paste(TextReader reader)
        {
            IDataGridViewEditingControl editingControl;
            DataGridViewEditingControlShowingEventHandler onEditingControlShowing =
                (sender, args) => editingControl = args.Control as IDataGridViewEditingControl;
            try
            {
                DataGridView.EditingControlShowing += onEditingControlShowing;
                bool anyChanges = false;
                var columnsByDisplayIndex =
                    DataGridView.Columns.Cast<DataGridViewColumn>().Where(column => column.Visible).ToArray();
                Array.Sort(columnsByDisplayIndex, (col1, col2) => col1.DisplayIndex.CompareTo(col2.DisplayIndex));
                int iFirstCol;
                int iFirstRow;
                if (null == DataGridView.CurrentCell)
                {
                    iFirstRow = 0;
                    iFirstCol = 0;
                }
                else
                {
                    iFirstCol = Array.FindIndex(columnsByDisplayIndex, col => col.Index == DataGridView.CurrentCell.ColumnIndex);
                    iFirstRow = DataGridView.CurrentCell.RowIndex;
                }

                for (int iRow = iFirstRow; iRow < DataGridView.Rows.Count; iRow++)
                {
                    string line = reader.ReadLine();
                    if (null == line)
                    {
                        return anyChanges;
                    }
                    var row = DataGridView.Rows[iRow];
                    var values = SplitLine(line).GetEnumerator();
                    for (int iCol = iFirstCol; iCol < columnsByDisplayIndex.Count(); iCol++)
                    {
                        if (!values.MoveNext())
                        {
                            break;
                        }
                        var column = columnsByDisplayIndex[iCol];
                        if (column.ReadOnly)
                        {
                            continue;
                        }
                        DataGridView.CurrentCell = row.Cells[column.Index];
                        string strValue = values.Current;
                        editingControl = null;
                        DataGridView.BeginEdit(true);
                        // ReSharper disable ConditionIsAlwaysTrueOrFalse
                        if (null != editingControl)
                        // ReSharper restore ConditionIsAlwaysTrueOrFalse
                        // ReSharper disable HeuristicUnreachableCode
                        {
                            object convertedValue;
                            if (!TryConvertValue(strValue, DataGridView.CurrentCell.FormattedValueType, out convertedValue))
                            {
                                return anyChanges;
                            }
                            editingControl.EditingControlFormattedValue = convertedValue;
                        }
                        // ReSharper restore HeuristicUnreachableCode
                        else
                        {
                            object convertedValue;
                            if (!TryConvertValue(strValue, DataGridView.CurrentCell.ValueType, out convertedValue))
                            {
                                return anyChanges;
                            }
                            DataGridView.CurrentCell.Value = convertedValue;
                        }
                        if (!DataGridView.EndEdit())
                        {
                            return anyChanges;
                        }
                        anyChanges = true;
                    }
                }
                return anyChanges;
            }
            finally
            {
                DataGridView.EditingControlShowing -= onEditingControlShowing;
            }
        }

        private static readonly char[] COLUMN_SEPARATORS = { '\t' };
        private IEnumerable<string> SplitLine(string row)
        {
            return row.Split(COLUMN_SEPARATORS);
        }

        protected bool TryConvertValue(string strValue, Type valueType, out object convertedValue)
        {
            if (null == valueType)
            {
                convertedValue = strValue;
                return true;
            }
            try
            {
                convertedValue = Convert.ChangeType(strValue, valueType);
                return true;
            }
            catch (Exception exception)
            {
                string message = string.Format("Error converting '{0}' to required type: {1}", strValue,
                                               exception.Message);
                MessageBox.Show(DataGridView, message, Application.ProductName);
                convertedValue = null;
                return false;
            }
        }
    }
}
