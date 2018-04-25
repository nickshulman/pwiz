using System;
using System.Linq;
using System.Windows.Forms;
using DigitalRune.Windows.Docking;
using Microsoft.VisualBasic.FileIO;
using TopographTool.Model;

namespace TopographTool.Ui
{
    public partial class TopographForm : Form
    {
        private TopographData _data;
        private bool _inUpdate;
        public TopographForm()
        {
            InitializeComponent();
            
        }

        public TopographData Data 
        {
            get { return _data; }
            set
            {
                _data = value;
                UpdateControls();
            }
        }

        public void UpdateControls()
        {
            try
            {
                _inUpdate = true;
                comboPeptide.Items.Clear();
                comboPeptide.Items.AddRange(_data.Proteins.SelectMany(p=>p.Peptides).
                    OrderBy(p=>p.ToString()).
                    Cast<object>().ToArray());
            }
            finally
            {
                _inUpdate = false;
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fileOpenDialog = new OpenFileDialog())
            {
                if (fileOpenDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    var parser = new TextFieldParser(fileOpenDialog.FileName);
                    parser.SetDelimiters(",");
                    Data = TopographData.MakeTopographData(ResultRow.Read(parser));
                }
                catch (Exception exception)
                {
                    string message = String.Join(Environment.NewLine, 
                        string.Format("Error parsing file '{0}':", fileOpenDialog.FileName), 
                        exception.Message);
                    MessageBox.Show(this, message);
                }
            }
        }

        private void btnShowPeptide_Click(object sender, EventArgs e)
        {
            var peptide = comboPeptide.SelectedItem as Peptide;
            if (peptide == null)
            {
                return;
            }
            var peptideForm = new PeptideForm();
            peptideForm.DataSet= new DataSet(Settings.DEFAULT, peptide);
            peptideForm.Show(dockPanel, DockState.Document);
        }

        private void importIsolationSchemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fileOpenDialog = new OpenFileDialog())
            {
                if (fileOpenDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    var parser = new TextFieldParser(fileOpenDialog.FileName);
                    parser.SetDelimiters(",");
                    
                }
                catch (Exception exception)
                {
                    string message = String.Join(Environment.NewLine,
                        string.Format("Error parsing file '{0}':", fileOpenDialog.FileName),
                        exception.Message);
                    MessageBox.Show(this, message);
                }
            }

        }
    }
}
