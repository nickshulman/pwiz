using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.SettingsUI
{
    public partial class MultiplexingDlg : Form
    {
        private const string COL_PREFIX = "Col";
        private const string COLNAME_MultiplexName = "MultipleName";
        private DataTable _dataTable;
        private MultiplexMatrix _originalMatrix;
        private MultiplexMatrix _multiplexMatrix;
        private IList<MultiplexMatrix> _existing;
        private ICollection<MeasuredIon> _customIons;
        public MultiplexingDlg(ICollection<MeasuredIon> customIons, MultiplexMatrix multiplexMatrix, IEnumerable<MultiplexMatrix> existing)
        {
            InitializeComponent();
            _customIons = customIons;
            _originalMatrix = _multiplexMatrix = multiplexMatrix;
            _existing = (existing ?? Array.Empty<MultiplexMatrix>()).ToList();
            _dataTable = new DataTable();
            _dataTable.Columns.Add(new DataColumn(COLNAME_MultiplexName, typeof(string))
            {
                Unique = true
            });
            foreach (var customIon in customIons)
            {
                _dataTable.Columns.Add(new DataColumn(COL_PREFIX + customIon.Name, typeof(double)));
            }
            
            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.DataSource = _dataTable;
            dataGridView1.Columns[0].Frozen = true;
            DisplayMultiplexMatrix(_originalMatrix);
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
                    DisplayMultiplexMatrix(value);
                }
            }
        }

        private void DisplayMultiplexMatrix(MultiplexMatrix matrix)
        {
            tbxMultiplexName.Text = matrix?.Name;
            _dataTable.Clear();
            if (matrix == null)
            {
                return;
            }
            foreach (var replicate in matrix.Replicates)
            {
                var rowValues = new List<object>{replicate.Name};
                foreach (DataColumn col in _dataTable.Columns.Cast<DataColumn>().Skip(1))
                {
                    Assume.IsTrue(col.ColumnName.StartsWith(COL_PREFIX));
                    var ionName = col.ColumnName.Substring(COL_PREFIX.Length);
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
            ReorderColumns(matrix);
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
                    weights.Add(new KeyValuePair<string, double>(column.ColumnName.Substring(COL_PREFIX.Length), value.Value));
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

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.Name == COLNAME_MultiplexName)
                {
                    col.HeaderText = "Multiplex Replicate Name";
                }
                else if (col.Name.StartsWith(COL_PREFIX))
                {
                    string ionName = col.Name.Substring(COL_PREFIX.Length);
                    col.HeaderText = ionName;
                }
            }

        }
        private void ReorderColumns(MultiplexMatrix matrix)
        {
            if (matrix == null)
            {
                return;
            }
            var activeIonNames = new HashSet<string>();
            activeIonNames.UnionWith(matrix.Replicates.SelectMany(replicate => replicate.Weights.Keys));
            var activeColumns = new List<DataGridViewColumn>();
            var inactiveColumns = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.Name == COLNAME_MultiplexName)
                {
                    activeColumns.Insert(0, col);
                }
                else if (col.Name.StartsWith(COL_PREFIX))
                {
                    string ionName = col.Name.Substring(COL_PREFIX.Length);
                    if (activeIonNames.Contains(ionName))
                    {
                        activeColumns.Add(col);
                    }
                    else
                    {
                        inactiveColumns.Add(col);
                    }
                }
            }

            int displayIndex = 0;
            foreach (var col in activeColumns.Concat(inactiveColumns))
            {
                col.DisplayIndex = displayIndex++;
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = TextUtil.FileDialogFilter("Proteome Discoverer", ".msf");
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                ReadMatrixFromFile(dlg.FileName);
            }
        }

        public void ReadMatrixFromFile(string filePath)
        {
            try
            {
                var matrix = new MsfMultiplexReader(_customIons).ReadMultiplexMatrix(filePath);
                if (matrix == null)
                {
                    MessageDlg.Show(this, string .Format("No multiplex scheme was found in {0}.", filePath));
                    return;
                }
                DisplayMultiplexMatrix(matrix);
            }
            catch (Exception e)
            {
                MessageDlg.ShowWithException(this, e.Message, e);
                return;
            }
        }
    }
}
