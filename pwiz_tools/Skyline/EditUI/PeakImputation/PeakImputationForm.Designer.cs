namespace pwiz.Skyline.EditUI.PeakImputation
{
    partial class PeakImputationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PeakImputationForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.comboImputeBoundariesFrom = new System.Windows.Forms.ComboBox();
            this.lblImputeBoundariesFrom = new System.Windows.Forms.Label();
            this.groupBoxRetentionTimeAlignment = new System.Windows.Forms.GroupBox();
            this.comboValuesToAlign = new System.Windows.Forms.ComboBox();
            this.lblValuesToAlign = new System.Windows.Forms.Label();
            this.comboAlignToFile = new System.Windows.Forms.ComboBox();
            this.lblAlignTo = new System.Windows.Forms.Label();
            this.comboAlignmentType = new System.Windows.Forms.ComboBox();
            this.lblAlignType = new System.Windows.Forms.Label();
            this.comboManualPeaks = new System.Windows.Forms.ComboBox();
            this.lblManualPeaks = new System.Windows.Forms.Label();
            this.btnImputeBoundaries = new System.Windows.Forms.Button();
            this.groupBoxPeakAcceptance = new System.Windows.Forms.GroupBox();
            this.tbxStandardDeviationsCutoff = new System.Windows.Forms.TextBox();
            this.lblMinCoreCount = new System.Windows.Forms.Label();
            this.numericUpDownCoreResults = new System.Windows.Forms.NumericUpDown();
            this.lblSdCutoff = new System.Windows.Forms.Label();
            this.comboScoringModel = new System.Windows.Forms.ComboBox();
            this.lblCoreScoreCutoff = new System.Windows.Forms.Label();
            this.lblScoringModel = new System.Windows.Forms.Label();
            this.tbxCoreScoreCutoff = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1.SuspendLayout();
            this.groupBoxRetentionTimeAlignment.SuspendLayout();
            this.groupBoxPeakAcceptance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCoreResults)).BeginInit();
            this.SuspendLayout();
            // 
            // databoundGridControl
            // 
            this.databoundGridControl.Location = new System.Drawing.Point(0, 212);
            this.databoundGridControl.Size = new System.Drawing.Size(800, 238);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.comboImputeBoundariesFrom);
            this.panel1.Controls.Add(this.lblImputeBoundariesFrom);
            this.panel1.Controls.Add(this.groupBoxRetentionTimeAlignment);
            this.panel1.Controls.Add(this.comboManualPeaks);
            this.panel1.Controls.Add(this.lblManualPeaks);
            this.panel1.Controls.Add(this.btnImputeBoundaries);
            this.panel1.Controls.Add(this.groupBoxPeakAcceptance);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 212);
            this.panel1.TabIndex = 1;
            // 
            // comboImputeBoundariesFrom
            // 
            this.comboImputeBoundariesFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboImputeBoundariesFrom.FormattingEnabled = true;
            this.comboImputeBoundariesFrom.Items.AddRange(new object[] {
            "Best scoring peak",
            "All accepted peaks"});
            this.comboImputeBoundariesFrom.Location = new System.Drawing.Point(520, 26);
            this.comboImputeBoundariesFrom.Name = "comboImputeBoundariesFrom";
            this.comboImputeBoundariesFrom.Size = new System.Drawing.Size(152, 21);
            this.comboImputeBoundariesFrom.TabIndex = 20;
            this.comboImputeBoundariesFrom.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblImputeBoundariesFrom
            // 
            this.lblImputeBoundariesFrom.AutoSize = true;
            this.lblImputeBoundariesFrom.Location = new System.Drawing.Point(521, 10);
            this.lblImputeBoundariesFrom.Name = "lblImputeBoundariesFrom";
            this.lblImputeBoundariesFrom.Size = new System.Drawing.Size(120, 13);
            this.lblImputeBoundariesFrom.TabIndex = 19;
            this.lblImputeBoundariesFrom.Text = "Impute boundaries from:";
            // 
            // groupBoxRetentionTimeAlignment
            // 
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.comboValuesToAlign);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblValuesToAlign);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.comboAlignToFile);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblAlignTo);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.comboAlignmentType);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblAlignType);
            this.groupBoxRetentionTimeAlignment.Location = new System.Drawing.Point(305, 10);
            this.groupBoxRetentionTimeAlignment.Name = "groupBoxRetentionTimeAlignment";
            this.groupBoxRetentionTimeAlignment.Size = new System.Drawing.Size(200, 148);
            this.groupBoxRetentionTimeAlignment.TabIndex = 18;
            this.groupBoxRetentionTimeAlignment.TabStop = false;
            this.groupBoxRetentionTimeAlignment.Text = "Retention time alignment";
            // 
            // comboValuesToAlign
            // 
            this.comboValuesToAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboValuesToAlign.FormattingEnabled = true;
            this.comboValuesToAlign.Location = new System.Drawing.Point(6, 71);
            this.comboValuesToAlign.Name = "comboValuesToAlign";
            this.comboValuesToAlign.Size = new System.Drawing.Size(188, 21);
            this.comboValuesToAlign.TabIndex = 5;
            this.comboValuesToAlign.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblValuesToAlign
            // 
            this.lblValuesToAlign.AutoSize = true;
            this.lblValuesToAlign.Location = new System.Drawing.Point(6, 53);
            this.lblValuesToAlign.Name = "lblValuesToAlign";
            this.lblValuesToAlign.Size = new System.Drawing.Size(79, 13);
            this.lblValuesToAlign.TabIndex = 4;
            this.lblValuesToAlign.Text = "Values to align:";
            // 
            // comboAlignToFile
            // 
            this.comboAlignToFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignToFile.FormattingEnabled = true;
            this.comboAlignToFile.Location = new System.Drawing.Point(6, 29);
            this.comboAlignToFile.Name = "comboAlignToFile";
            this.comboAlignToFile.Size = new System.Drawing.Size(190, 21);
            this.comboAlignToFile.TabIndex = 1;
            this.comboAlignToFile.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblAlignTo
            // 
            this.lblAlignTo.AutoSize = true;
            this.lblAlignTo.Location = new System.Drawing.Point(3, 13);
            this.lblAlignTo.Name = "lblAlignTo";
            this.lblAlignTo.Size = new System.Drawing.Size(61, 13);
            this.lblAlignTo.TabIndex = 0;
            this.lblAlignTo.Text = "Align to file:";
            // 
            // comboAlignmentType
            // 
            this.comboAlignmentType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignmentType.FormattingEnabled = true;
            this.comboAlignmentType.Location = new System.Drawing.Point(4, 111);
            this.comboAlignmentType.Name = "comboAlignmentType";
            this.comboAlignmentType.Size = new System.Drawing.Size(190, 21);
            this.comboAlignmentType.TabIndex = 3;
            this.comboAlignmentType.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblAlignType
            // 
            this.lblAlignType.AutoSize = true;
            this.lblAlignType.Location = new System.Drawing.Point(1, 95);
            this.lblAlignType.Name = "lblAlignType";
            this.lblAlignType.Size = new System.Drawing.Size(79, 13);
            this.lblAlignType.TabIndex = 2;
            this.lblAlignType.Text = "Alignment type:";
            // 
            // comboManualPeaks
            // 
            this.comboManualPeaks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboManualPeaks.FormattingEnabled = true;
            this.comboManualPeaks.Location = new System.Drawing.Point(305, 179);
            this.comboManualPeaks.Name = "comboManualPeaks";
            this.comboManualPeaks.Size = new System.Drawing.Size(190, 21);
            this.comboManualPeaks.TabIndex = 17;
            this.toolTip1.SetToolTip(this.comboManualPeaks, resources.GetString("comboManualPeaks.ToolTip"));
            this.comboManualPeaks.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblManualPeaks
            // 
            this.lblManualPeaks.AutoSize = true;
            this.lblManualPeaks.Location = new System.Drawing.Point(308, 163);
            this.lblManualPeaks.Name = "lblManualPeaks";
            this.lblManualPeaks.Size = new System.Drawing.Size(131, 13);
            this.lblManualPeaks.TabIndex = 16;
            this.lblManualPeaks.Text = "Manually integrated peaks";
            // 
            // btnImputeBoundaries
            // 
            this.btnImputeBoundaries.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImputeBoundaries.Location = new System.Drawing.Point(648, 177);
            this.btnImputeBoundaries.Name = "btnImputeBoundaries";
            this.btnImputeBoundaries.Size = new System.Drawing.Size(140, 23);
            this.btnImputeBoundaries.TabIndex = 13;
            this.btnImputeBoundaries.Text = "Impute Boundaries";
            this.toolTip1.SetToolTip(this.btnImputeBoundaries, "For all of the rows displayed in the grid, impute the peak boundaries for the rej" +
        "ected peaks.");
            this.btnImputeBoundaries.UseVisualStyleBackColor = true;
            this.btnImputeBoundaries.Click += new System.EventHandler(this.btnImputeBoundaries_Click);
            // 
            // groupBoxPeakAcceptance
            // 
            this.groupBoxPeakAcceptance.Controls.Add(this.tbxStandardDeviationsCutoff);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblMinCoreCount);
            this.groupBoxPeakAcceptance.Controls.Add(this.numericUpDownCoreResults);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblSdCutoff);
            this.groupBoxPeakAcceptance.Controls.Add(this.comboScoringModel);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblCoreScoreCutoff);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblScoringModel);
            this.groupBoxPeakAcceptance.Controls.Add(this.tbxCoreScoreCutoff);
            this.groupBoxPeakAcceptance.Location = new System.Drawing.Point(12, 10);
            this.groupBoxPeakAcceptance.Name = "groupBoxPeakAcceptance";
            this.groupBoxPeakAcceptance.Size = new System.Drawing.Size(277, 183);
            this.groupBoxPeakAcceptance.TabIndex = 14;
            this.groupBoxPeakAcceptance.TabStop = false;
            this.groupBoxPeakAcceptance.Text = "Peak acceptance";
            // 
            // tbxStandardDeviationsCutoff
            // 
            this.tbxStandardDeviationsCutoff.Location = new System.Drawing.Point(9, 150);
            this.tbxStandardDeviationsCutoff.Name = "tbxStandardDeviationsCutoff";
            this.tbxStandardDeviationsCutoff.Size = new System.Drawing.Size(168, 20);
            this.tbxStandardDeviationsCutoff.TabIndex = 11;
            this.tbxStandardDeviationsCutoff.Text = "1";
            this.toolTip1.SetToolTip(this.tbxStandardDeviationsCutoff, "Peaks whose boundaries are within this number of standard deviations from the acc" +
        "epted peaks will also be accepted");
            this.tbxStandardDeviationsCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblMinCoreCount
            // 
            this.lblMinCoreCount.AutoSize = true;
            this.lblMinCoreCount.Location = new System.Drawing.Point(6, 56);
            this.lblMinCoreCount.Name = "lblMinCoreCount";
            this.lblMinCoreCount.Size = new System.Drawing.Size(128, 13);
            this.lblMinCoreCount.TabIndex = 6;
            this.lblMinCoreCount.Text = "Minimum peaks to accept";
            // 
            // numericUpDownCoreResults
            // 
            this.numericUpDownCoreResults.Location = new System.Drawing.Point(6, 72);
            this.numericUpDownCoreResults.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numericUpDownCoreResults.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownCoreResults.Name = "numericUpDownCoreResults";
            this.numericUpDownCoreResults.Size = new System.Drawing.Size(168, 20);
            this.numericUpDownCoreResults.TabIndex = 7;
            this.toolTip1.SetToolTip(this.numericUpDownCoreResults, "Ranking of peaks which are always accepted");
            this.numericUpDownCoreResults.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numericUpDownCoreResults.ValueChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblSdCutoff
            // 
            this.lblSdCutoff.AutoSize = true;
            this.lblSdCutoff.Location = new System.Drawing.Point(6, 135);
            this.lblSdCutoff.Name = "lblSdCutoff";
            this.lblSdCutoff.Size = new System.Drawing.Size(131, 13);
            this.lblSdCutoff.TabIndex = 10;
            this.lblSdCutoff.Text = "Standard deviations cutoff";
            // 
            // comboScoringModel
            // 
            this.comboScoringModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboScoringModel.FormattingEnabled = true;
            this.comboScoringModel.Location = new System.Drawing.Point(6, 32);
            this.comboScoringModel.Name = "comboScoringModel";
            this.comboScoringModel.Size = new System.Drawing.Size(190, 21);
            this.comboScoringModel.TabIndex = 5;
            this.toolTip1.SetToolTip(this.comboScoringModel, resources.GetString("comboScoringModel.ToolTip"));
            this.comboScoringModel.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblCoreScoreCutoff
            // 
            this.lblCoreScoreCutoff.AutoSize = true;
            this.lblCoreScoreCutoff.Location = new System.Drawing.Point(6, 95);
            this.lblCoreScoreCutoff.Name = "lblCoreScoreCutoff";
            this.lblCoreScoreCutoff.Size = new System.Drawing.Size(65, 13);
            this.lblCoreScoreCutoff.TabIndex = 8;
            this.lblCoreScoreCutoff.Text = "Score cutoff";
            // 
            // lblScoringModel
            // 
            this.lblScoringModel.AutoSize = true;
            this.lblScoringModel.Location = new System.Drawing.Point(6, 16);
            this.lblScoringModel.Name = "lblScoringModel";
            this.lblScoringModel.Size = new System.Drawing.Size(77, 13);
            this.lblScoringModel.TabIndex = 4;
            this.lblScoringModel.Text = "Scoring model:";
            // 
            // tbxCoreScoreCutoff
            // 
            this.tbxCoreScoreCutoff.Location = new System.Drawing.Point(6, 111);
            this.tbxCoreScoreCutoff.Name = "tbxCoreScoreCutoff";
            this.tbxCoreScoreCutoff.Size = new System.Drawing.Size(168, 20);
            this.tbxCoreScoreCutoff.TabIndex = 9;
            this.toolTip1.SetToolTip(this.tbxCoreScoreCutoff, "Score above which a peak will always be accepted");
            this.tbxCoreScoreCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // PeakImputationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel1);
            this.Name = "PeakImputationForm";
            this.TabText = "Peak Imputation";
            this.Text = "Peak Imputation";
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.databoundGridControl, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBoxRetentionTimeAlignment.ResumeLayout(false);
            this.groupBoxRetentionTimeAlignment.PerformLayout();
            this.groupBoxPeakAcceptance.ResumeLayout(false);
            this.groupBoxPeakAcceptance.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCoreResults)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboAlignmentType;
        private System.Windows.Forms.Label lblAlignType;
        private System.Windows.Forms.ComboBox comboAlignToFile;
        private System.Windows.Forms.Label lblAlignTo;
        private System.Windows.Forms.ComboBox comboScoringModel;
        private System.Windows.Forms.Label lblScoringModel;
        private System.Windows.Forms.TextBox tbxCoreScoreCutoff;
        private System.Windows.Forms.Label lblCoreScoreCutoff;
        private System.Windows.Forms.NumericUpDown numericUpDownCoreResults;
        private System.Windows.Forms.Label lblMinCoreCount;
        private System.Windows.Forms.TextBox tbxStandardDeviationsCutoff;
        private System.Windows.Forms.Label lblSdCutoff;
        private System.Windows.Forms.Button btnImputeBoundaries;
        private System.Windows.Forms.GroupBox groupBoxPeakAcceptance;
        private System.Windows.Forms.ComboBox comboManualPeaks;
        private System.Windows.Forms.Label lblManualPeaks;
        private System.Windows.Forms.GroupBox groupBoxRetentionTimeAlignment;
        private System.Windows.Forms.ComboBox comboValuesToAlign;
        private System.Windows.Forms.Label lblValuesToAlign;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox comboImputeBoundariesFrom;
        private System.Windows.Forms.Label lblImputeBoundariesFrom;
    }
}