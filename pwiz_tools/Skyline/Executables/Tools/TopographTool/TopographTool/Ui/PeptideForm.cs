using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using DigitalRune.Windows.Docking;
using pwiz.Common.Collections;
using TopographTool.Model;

namespace TopographTool.Ui
{
    public partial class PeptideForm : DockableForm
    {
        private DataSet _dataSet;
        private bool _inUpdate;
        private Replicate _replicate;
        private IDictionary<int, DataGridViewTextBoxColumn> _labelColumns;
        public PeptideForm()
        {
            InitializeComponent();
            FeatureWeights = FeatureWeights.EMPTY;
            _labelColumns = new Dictionary<int, DataGridViewTextBoxColumn>();
        }

        public DataSet DataSet
        {
            get { return _dataSet; }
            set 
            { 
                _dataSet = value;
                _replicate = DataSet.Replicates.FirstOrDefault();
                UpdateUi();
            }
        }

        public FeatureWeights FeatureWeights { get; private set; }
        public ImmutableList<TransitionKey> TransitionKeys { get; private set; }

        public void UpdateUi()
        {
            try
            {
                _inUpdate = true;
                UpdateControls();
            }
            finally
            {
                _inUpdate = false;
            }
            UpdateGrid();
        }

        public void UpdateControls()
        {
            FeatureWeights = DataSet.GetFeatureWeights();
            TransitionKeys = ImmutableList.ValueOf(FeatureWeights.TransitionKeys.Distinct());
            comboReplicate.Items.Clear();
            foreach (var replicate in DataSet.Replicates)
            {
                comboReplicate.Items.Add(replicate.Name);
                if (replicate == _replicate)
                {
                    comboReplicate.SelectedIndex = comboReplicate.Items.Count - 1;
                }
            }
            tbxPeptide.Text = DataSet.Peptide.PeptideModifiedSequence.ToString();
            listBoxTransitions.Items.Clear();
            foreach (var transition in FeatureWeights.TransitionKeys.Distinct())
            {
                listBoxTransitions.Items.Add(transition);
            }
            foreach (var column in _labelColumns.Values)
            {
                dataGridViewFeatures.Columns.Remove(column);
            }
            _labelColumns.Clear();
            var labelCounts = FeatureWeights.LabelContribs.SelectMany(l => l.LabelCounts).Distinct().OrderBy(c => c)
                .ToArray();
            foreach (var labelCount in labelCounts)
            {
                var column = new DataGridViewTextBoxColumn() {HeaderText = labelCount.ToString()};
                column.DefaultCellStyle.Format = "0.####";
                dataGridViewFeatures.Columns.Add(column);
                _labelColumns.Add(labelCount, column);
            }
        }

        public void UpdateGrid()
        {
            dataGridViewFeatures.Rows.Clear();
            chart1.Series.Clear();
            ICollection<TransitionKey> transitionKeys = new HashSet<TransitionKey>(
                listBoxTransitions.SelectedItems.Cast<TransitionKey>()).ToArray();
            var featureWeights = FeatureWeights;
            if (transitionKeys.Count != 0)
            {
                featureWeights = featureWeights.Filter(transitionKeys);
            }
            var featureAreas = FeatureAreas.GetFeatureAreas(featureWeights, _replicate, DataSet);
            double totalArea = featureAreas.Areas.Sum(a => a.GetValueOrDefault());
            for (int i = 0; i < featureWeights.RowCount; i++)
            {
                var row = dataGridViewFeatures.Rows[dataGridViewFeatures.Rows.Add()];
                row.Cells[colTransition.Index].Value = featureWeights.TransitionKeys[i];
                row.Cells[colFeature.Index].Value = FeatureWeights.FeatureKeys[i];
                var area = featureAreas.Areas[i];
                if (area.HasValue)
                {
                    row.Cells[colFeatureAmount.Index].Value = area / totalArea;
                }
                var labelContribs = featureWeights.LabelContribs[i];
                foreach (var entry in _labelColumns)
                {
                    row.Cells[entry.Value.Index].Value = labelContribs.GetContribution(entry.Key);
                }
            }
            var labelAmounts = featureAreas.GetLabelAmounts();
            if (labelAmounts != null)
            {
                var series = new Series("Label Amounts");
                for (int i = 0; i < labelAmounts.Count; i++)
                {
                    var amount = labelAmounts[i];
                    if (double.IsNaN(amount) || double.IsInfinity(amount))
                    {
                        continue;
                    }
                    series.Points.Add(new DataPoint(featureAreas.LabelCounts[i], labelAmounts[i]));
                }
                if (series.Points.Any())
                {
                    chart1.Series.Add(series);
                }
            }
        }

        private void comboReplicate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_inUpdate)
            {
                return;
            }
            _replicate = DataSet.Replicates[comboReplicate.SelectedIndex];
            UpdateGrid();
        }

        private void listBoxTransitions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_inUpdate)
            {
                return;
            }
            UpdateGrid();
        }
    }
}
