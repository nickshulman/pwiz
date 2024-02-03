using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public partial class MultiplexingDlg : Form
    {
        private const string ION_COL_PREFIX = "Col";
        private DataTable _dataTable;
        private MultiplexMatrix _originalMatrix;
        private MultiplexMatrix _multiplexMatrix;
        private IList<MultiplexMatrix> _existing;
        public MultiplexingDlg(ICollection<MeasuredIon> customIons, MultiplexMatrix multiplexMatrix, IEnumerable<MultiplexMatrix> existing)
        {
            InitializeComponent();
            _originalMatrix = multiplexMatrix;
            _existing = (existing ?? Array.Empty<MultiplexMatrix>()).ToList();
            _dataTable = new DataTable();
            _dataTable.Columns.Add(new DataColumn("MultiplexName", typeof(string))
            {
                Unique = true,
                Caption = "Multiplex Name"
            });
            foreach (var customIonName in GetColumnNames(customIons, multiplexMatrix ?? MultiplexMatrix.NONE))
            {
                _dataTable.Columns.Add(new DataColumn(ION_COL_PREFIX + customIonName, typeof(double))
                {
                    Caption = customIonName
                });
            }
            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.DataSource = _dataTable;
            dataGridView1.Columns[0].Frozen = true;
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageDlg.ShowWithException(this, e.Exception.Message, e.Exception);
        }

        public MultiplexMatrix MultiplexMatrix
        {
            get
            {
                return _multiplexMatrix;
            }
            set
            {
                if (!Equals(_multiplexMatrix, value))
                {
                    _multiplexMatrix = value;
                    DisplayMultipleMatrix(value);
                }
            }
        }

        private void DisplayMultipleMatrix(MultiplexMatrix matrix)
        {
            tbxMultiplexName.Text = matrix.Name;
            _dataTable.Clear();
            foreach (var replicate in matrix.Replicates)
            {
                var rowValues = new List<object>{replicate.Name};
                foreach (DataColumn col in _dataTable.Columns)
                {
                    Assume.IsTrue(col.ColumnName.StartsWith(ION_COL_PREFIX));
                    var ionName = col.ColumnName.Substring(ION_COL_PREFIX.Length);
                    if (replicate.Weights.TryGetValue(ionName, out var weight))
                    {
                        rowValues.Add(weight);
                    }
                    else
                    {
                        rowValues.Add(null);
                    }
                }

                _dataTable.Rows.Add(rowValues.ToArray());
            }
        }

        public void OkDialog()
        {
            var helper = new MessageBoxHelper(this);
            if (!helper.ValidateNameTextBox(tbxMultiplexName, out string matrixName))
            {
                return;
            }

            if (matrixName != _originalMatrix?.Name && _existing.Any(matrix => matrix.Name == matrixName))
            {
                helper.ShowTextBoxError(tbxMultiplexName, "The multiple matrix '{0}' already exists.", matrixName);
                return;
            }

            var multiplexMatrix = GetMultiplexMatrix(matrixName);
            if (multiplexMatrix == null)
            {
                return;
            }

            _multiplexMatrix = multiplexMatrix;
            DialogResult = DialogResult.OK;
        }

        public MultiplexMatrix GetMultiplexMatrix(string matrixName)
        {
            var replicateNames = new HashSet<string>();
            var replicates = new List<MultiplexMatrix.Replicate>();
            for (int rowIndex = 0; rowIndex < _dataTable.Rows.Count; rowIndex++)
            {
                var dataRow = _dataTable.Rows[rowIndex];
                string replicateName = dataRow[0] as string;
                var weights = new List<KeyValuePair<string, double>>();
                if (string.IsNullOrEmpty(replicateName))
                {
                    ShowDataError(rowIndex, 0, "Name cannot be blank");
                    return null;
                }

                if (!replicateNames.Add(replicateName))
                {
                    ShowDataError(rowIndex, 0, "Duplicate replicate name");
                    return null;
                }

                for (int columnIndex = 1; columnIndex < _dataTable.Columns.Count; columnIndex++)
                {
                    double? value = dataRow[columnIndex] as double?;
                    if (!value.HasValue || value == 0)
                    {
                        continue;
                    }

                    if (double.IsNaN(value.Value) || double.IsInfinity(value.Value))
                    {
                        ShowDataError(rowIndex, columnIndex, "Invalid number");
                        return null;
                    }
                    var column = _dataTable.Columns[columnIndex];
                    weights.Add(new KeyValuePair<string, double>(column.ColumnName.Substring(ION_COL_PREFIX.Length), value.Value));
                }
                replicates.Add(new MultiplexMatrix.Replicate(replicateName, weights));
            }

            return new MultiplexMatrix(matrixName, replicates);
        }

        private void ShowDataError(int rowIndex, int columnIndex, string message)
        {
            MessageDlg.Show(this, message);
            dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells[columnIndex];
        }

        private IEnumerable<string> GetColumnNames(ICollection<MeasuredIon> measuredIons,
            MultiplexMatrix multiplexMatrix)
        {
            var ionNames = measuredIons.Select(ion => ion.Name).ToHashSet();
            var matrixColumnNames = multiplexMatrix.Replicates.SelectMany(replicate => replicate.Weights.Keys).ToHashSet();
            
            var allColumnNames = new List<string>();
            // First, return the column names that are being used by the matrix
            allColumnNames.AddRange(measuredIons.Where(ion => matrixColumnNames.Contains(ion.Name))
                .Select(ion => ion.Name));
            // Then, add the column names that are being used by the matrix, but which have no matching ion
            allColumnNames.AddRange(matrixColumnNames.Where(name => !ionNames.Contains(name)).OrderBy(name => name));
            // Lastly, add the rest of the ion names
            allColumnNames.AddRange(measuredIons.Where(ion=>!matrixColumnNames.Contains(ion.Name)).Select(ion=>ion.Name));
            return allColumnNames;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }
    }
}
