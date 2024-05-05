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
            this.panel1 = new System.Windows.Forms.Panel();
            this.comboManualPeaks = new System.Windows.Forms.ComboBox();
            this.lblManualPeaks = new System.Windows.Forms.Label();
            this.btnImputeBoundaries = new System.Windows.Forms.Button();
            this.comboScoringModel = new System.Windows.Forms.ComboBox();
            this.lblScoringModel = new System.Windows.Forms.Label();
            this.comboAlignmentType = new System.Windows.Forms.ComboBox();
            this.lblAlignType = new System.Windows.Forms.Label();
            this.comboAlignToFile = new System.Windows.Forms.ComboBox();
            this.lblAlignTo = new System.Windows.Forms.Label();
            this.groupBoxCoreCriteria = new System.Windows.Forms.GroupBox();
            this.tbxStandardDeviationsCutoff = new System.Windows.Forms.TextBox();
            this.lblMinCoreCount = new System.Windows.Forms.Label();
            this.numericUpDownCoreResults = new System.Windows.Forms.NumericUpDown();
            this.lblSdCutoff = new System.Windows.Forms.Label();
            this.lblCoreScoreCutoff = new System.Windows.Forms.Label();
            this.tbxCoreScoreCutoff = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.groupBoxCoreCriteria.SuspendLayout();
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
            this.panel1.Controls.Add(this.comboManualPeaks);
            this.panel1.Controls.Add(this.lblManualPeaks);
            this.panel1.Controls.Add(this.btnImputeBoundaries);
            this.panel1.Controls.Add(this.comboScoringModel);
            this.panel1.Controls.Add(this.lblScoringModel);
            this.panel1.Controls.Add(this.comboAlignmentType);
            this.panel1.Controls.Add(this.lblAlignType);
            this.panel1.Controls.Add(this.comboAlignToFile);
            this.panel1.Controls.Add(this.lblAlignTo);
            this.panel1.Controls.Add(this.groupBoxCoreCriteria);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 212);
            this.panel1.TabIndex = 1;
            // 
            // comboManualPeaks
            // 
            this.comboManualPeaks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboManualPeaks.FormattingEnabled = true;
            this.comboManualPeaks.Location = new System.Drawing.Point(15, 149);
            this.comboManualPeaks.Name = "comboManualPeaks";
            this.comboManualPeaks.Size = new System.Drawing.Size(190, 21);
            this.comboManualPeaks.TabIndex = 17;
            this.comboManualPeaks.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblManualPeaks
            // 
            this.lblManualPeaks.AutoSize = true;
            this.lblManualPeaks.Location = new System.Drawing.Point(12, 133);
            this.lblManualPeaks.Name = "lblManualPeaks";
            this.lblManualPeaks.Size = new System.Drawing.Size(131, 13);
            this.lblManualPeaks.TabIndex = 16;
            this.lblManualPeaks.Text = "Manually integrated peaks";
            // 
            // btnImputeBoundaries
            // 
            this.btnImputeBoundaries.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImputeBoundaries.Location = new System.Drawing.Point(648, 10);
            this.btnImputeBoundaries.Name = "btnImputeBoundaries";
            this.btnImputeBoundaries.Size = new System.Drawing.Size(140, 23);
            this.btnImputeBoundaries.TabIndex = 13;
            this.btnImputeBoundaries.Text = "Impute Boundaries";
            this.btnImputeBoundaries.UseVisualStyleBackColor = true;
            this.btnImputeBoundaries.Click += new System.EventHandler(this.btnImputeBoundaries_Click);
            // 
            // comboScoringModel
            // 
            this.comboScoringModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboScoringModel.FormattingEnabled = true;
            this.comboScoringModel.Location = new System.Drawing.Point(15, 106);
            this.comboScoringModel.Name = "comboScoringModel";
            this.comboScoringModel.Size = new System.Drawing.Size(190, 21);
            this.comboScoringModel.TabIndex = 5;
            this.comboScoringModel.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblScoringModel
            // 
            this.lblScoringModel.AutoSize = true;
            this.lblScoringModel.Location = new System.Drawing.Point(12, 90);
            this.lblScoringModel.Name = "lblScoringModel";
            this.lblScoringModel.Size = new System.Drawing.Size(77, 13);
            this.lblScoringModel.TabIndex = 4;
            this.lblScoringModel.Text = "Scoring model:";
            // 
            // comboAlignmentType
            // 
            this.comboAlignmentType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignmentType.FormattingEnabled = true;
            this.comboAlignmentType.Location = new System.Drawing.Point(15, 64);
            this.comboAlignmentType.Name = "comboAlignmentType";
            this.comboAlignmentType.Size = new System.Drawing.Size(190, 21);
            this.comboAlignmentType.TabIndex = 3;
            this.comboAlignmentType.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblAlignType
            // 
            this.lblAlignType.AutoSize = true;
            this.lblAlignType.Location = new System.Drawing.Point(12, 48);
            this.lblAlignType.Name = "lblAlignType";
            this.lblAlignType.Size = new System.Drawing.Size(79, 13);
            this.lblAlignType.TabIndex = 2;
            this.lblAlignType.Text = "Alignment type:";
            // 
            // comboAlignToFile
            // 
            this.comboAlignToFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignToFile.FormattingEnabled = true;
            this.comboAlignToFile.Location = new System.Drawing.Point(15, 25);
            this.comboAlignToFile.Name = "comboAlignToFile";
            this.comboAlignToFile.Size = new System.Drawing.Size(190, 21);
            this.comboAlignToFile.TabIndex = 1;
            this.comboAlignToFile.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblAlignTo
            // 
            this.lblAlignTo.AutoSize = true;
            this.lblAlignTo.Location = new System.Drawing.Point(12, 9);
            this.lblAlignTo.Name = "lblAlignTo";
            this.lblAlignTo.Size = new System.Drawing.Size(45, 13);
            this.lblAlignTo.TabIndex = 0;
            this.lblAlignTo.Text = "Align to:";
            // 
            // groupBoxCoreCriteria
            // 
            this.groupBoxCoreCriteria.Controls.Add(this.tbxStandardDeviationsCutoff);
            this.groupBoxCoreCriteria.Controls.Add(this.lblMinCoreCount);
            this.groupBoxCoreCriteria.Controls.Add(this.numericUpDownCoreResults);
            this.groupBoxCoreCriteria.Controls.Add(this.lblSdCutoff);
            this.groupBoxCoreCriteria.Controls.Add(this.lblCoreScoreCutoff);
            this.groupBoxCoreCriteria.Controls.Add(this.tbxCoreScoreCutoff);
            this.groupBoxCoreCriteria.Location = new System.Drawing.Point(232, 3);
            this.groupBoxCoreCriteria.Name = "groupBoxCoreCriteria";
            this.groupBoxCoreCriteria.Size = new System.Drawing.Size(277, 143);
            this.groupBoxCoreCriteria.TabIndex = 14;
            this.groupBoxCoreCriteria.TabStop = false;
            this.groupBoxCoreCriteria.Text = "Core && outlier criteria";
            // 
            // tbxStandardDeviationsCutoff
            // 
            this.tbxStandardDeviationsCutoff.Location = new System.Drawing.Point(6, 114);
            this.tbxStandardDeviationsCutoff.Name = "tbxStandardDeviationsCutoff";
            this.tbxStandardDeviationsCutoff.Size = new System.Drawing.Size(168, 20);
            this.tbxStandardDeviationsCutoff.TabIndex = 11;
            this.tbxStandardDeviationsCutoff.Text = "3";
            this.tbxStandardDeviationsCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblMinCoreCount
            // 
            this.lblMinCoreCount.AutoSize = true;
            this.lblMinCoreCount.Location = new System.Drawing.Point(3, 17);
            this.lblMinCoreCount.Name = "lblMinCoreCount";
            this.lblMinCoreCount.Size = new System.Drawing.Size(105, 13);
            this.lblMinCoreCount.TabIndex = 6;
            this.lblMinCoreCount.Text = "Minimum core results";
            // 
            // numericUpDownCoreResults
            // 
            this.numericUpDownCoreResults.Location = new System.Drawing.Point(6, 34);
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
            this.lblSdCutoff.Location = new System.Drawing.Point(3, 96);
            this.lblSdCutoff.Name = "lblSdCutoff";
            this.lblSdCutoff.Size = new System.Drawing.Size(131, 13);
            this.lblSdCutoff.TabIndex = 10;
            this.lblSdCutoff.Text = "Standard deviations cutoff";
            // 
            // lblCoreScoreCutoff
            // 
            this.lblCoreScoreCutoff.AutoSize = true;
            this.lblCoreScoreCutoff.Location = new System.Drawing.Point(3, 57);
            this.lblCoreScoreCutoff.Name = "lblCoreScoreCutoff";
            this.lblCoreScoreCutoff.Size = new System.Drawing.Size(65, 13);
            this.lblCoreScoreCutoff.TabIndex = 8;
            this.lblCoreScoreCutoff.Text = "Score cutoff";
            // 
            // tbxCoreScoreCutoff
            // 
            this.tbxCoreScoreCutoff.Location = new System.Drawing.Point(6, 73);
            this.tbxCoreScoreCutoff.Name = "tbxCoreScoreCutoff";
            this.tbxCoreScoreCutoff.Size = new System.Drawing.Size(168, 20);
            this.tbxCoreScoreCutoff.TabIndex = 9;
            this.tbxCoreScoreCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // PeakImputationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel1);
            this.Name = "PeakImputationForm";
            this.Text = "PeakImputationForm";
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.databoundGridControl, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBoxCoreCriteria.ResumeLayout(false);
            this.groupBoxCoreCriteria.PerformLayout();
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
        private System.Windows.Forms.GroupBox groupBoxCoreCriteria;
        private System.Windows.Forms.ComboBox comboManualPeaks;
        private System.Windows.Forms.Label lblManualPeaks;
    }
}