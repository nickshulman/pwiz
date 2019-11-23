using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CrossLinkerTool.Properties;
using pwiz.Common.Chemistry;
using SkylineTool;

namespace CrossLinkerTool
{
    public partial class AddCrosslinksForm : Form
    {
        private SkylineToolClient _skylineToolClient;
        private static ResidueFormulae _residueFormulae = ResidueFormulae.GetDefault();

        public AddCrosslinksForm()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = true;
            bindingSource1.DataSource = new BindingList<DataRow>();
            PasteHandler.Attach(dataGridView1);
        }

        public SkylineToolClient SkylineToolClient
        {
            get { return _skylineToolClient; }
            set
            {
                _skylineToolClient = value;
                btnAddToSkylineDocument.Enabled = null != SkylineToolClient;
            }
        }

        public class DataRow
        {
            public DataRow()
            {
                Peptide1 = string.Empty;
                Position1 = 1;
                Peptide2 = string.Empty;
                Position2 = 1;
                CrosslinkFormula = string.Empty;
                ProteinName = string.Empty;
            }
            public string Peptide1 { get; set; }
            public int Position1 { get; set; }
            public string Peptide2 { get; set; }
            public int Position2 { get; set; }
            public string ProteinName { get; set; }
            public string CrosslinkFormula { get; set; }
        }

        private void btnAddToSkylineDocument_Click(object sender, EventArgs e)
        {
            var dataRows = ((IEnumerable) bindingSource1.DataSource).OfType<DataRow>().ToArray();
            using (var longWaitDlg = new LongWaitDlg())
            {
                longWaitDlg.PerformWork(this, 100, ()=>
                {
                    for (int i = 0; i < dataRows.Length; i++)
                    {
                        longWaitDlg.CancellationToken.ThrowIfCancellationRequested();
                        longWaitDlg.ProgressValue = 100 * i / dataRows.Length;
                        longWaitDlg.Message = string.Format("Processing row {0} of {1}", i + 1, dataRows.Length);
                        AddToSkylineDocument(dataRows[i]);
                    }
                });
            }
        }

        private void AddToSkylineDocument(DataRow dataRow)
        {
            if (SkylineToolClient == null)
            {
                Thread.Sleep(100);
                return;
            }
            ToolModification modification1 = GetModification(dataRow.Peptide2, dataRow.CrosslinkFormula);
            ToolModification modification2 = GetModification(dataRow.Peptide1, dataRow.CrosslinkFormula);
            SkylineToolClient.DefineModification(modification1);
            SkylineToolClient.DefineModification(modification2);
            string proteinName = dataRow.ProteinName;

            if (string.IsNullOrEmpty(proteinName))
            {
                proteinName = dataRow.Peptide1 + " - " + dataRow.Peptide2;
            }
            SkylineToolClient.AddPeptides(new[]
            {
                new ToolPeptide
                {
                    PeptideSequence = GetModifiedSequence(dataRow.Peptide1, new Dictionary<int, ToolModification>()
                    {{dataRow.Position1, modification1}}),
                    ProteinName = proteinName
                },
                new ToolPeptide
                {
                    PeptideSequence = GetModifiedSequence(dataRow.Peptide2, new Dictionary<int, ToolModification>()
                    {{dataRow.Position2, modification2}}),
                    ProteinName = proteinName
                },
            });

        }

        public static ToolModification GetModification(string peptide, string crosslinker)
        {
            Molecule molecule = _residueFormulae.GetPeptideFormula(peptide);
            string name = peptide;
            if (!crosslinker.StartsWith("+") && !crosslinker.StartsWith("-"))
            {
                name += "+";
            }
            name += crosslinker;
            return new ToolModification
            {
                Name = name,
                Formula = molecule + crosslinker,
                Variable = false,
            };
        }
        public static string GetModifiedSequence(String peptideSequence, Dictionary<int, ToolModification> modifications)
        {
            StringBuilder result = new StringBuilder();
            for (int ich = 0; ich < peptideSequence.Length; ich++)
            {
                result.Append(peptideSequence[ich]);
                ToolModification modification;
                if (modifications.TryGetValue(ich + 1, out modification))
                {
                    result.Append("[" + modification.Name + "]");
                }
                else
                {
                    string staticModName = _residueFormulae.GetModificationName(peptideSequence[ich]);
                    if (!string.IsNullOrEmpty(staticModName))
                    {
                        result.Append("[" + staticModName + "]");
                    }
                }
            }
            return result.ToString();
        }
    }
}
