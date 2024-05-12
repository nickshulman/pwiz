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
            this.tbxMeanStandardDeviation = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.alignmentControl = new pwiz.Skyline.EditUI.PeakImputation.AlignmentControl();
            this.comboImputeBoundariesFrom = new System.Windows.Forms.ComboBox();
            this.lblImputeBoundariesFrom = new System.Windows.Forms.Label();
            this.comboManualPeaks = new System.Windows.Forms.ComboBox();
            this.lblManualPeaks = new System.Windows.Forms.Label();
            this.btnImputeBoundaries = new System.Windows.Forms.Button();
            this.groupBoxPeakAcceptance = new System.Windows.Forms.GroupBox();
            this.groupBoxCutoff = new System.Windows.Forms.GroupBox();
            this.radioPercentile = new System.Windows.Forms.RadioButton();
            this.radioQValue = new System.Windows.Forms.RadioButton();
            this.radioScore = new System.Windows.Forms.RadioButton();
            this.tbxCoreScoreCutoff = new System.Windows.Forms.TextBox();
            this.tbxRtDeviationCutoff = new System.Windows.Forms.TextBox();
            this.lblMinCoreCount = new System.Windows.Forms.Label();
            this.numericUpDownCoreResults = new System.Windows.Forms.NumericUpDown();
            this.lblSdCutoff = new System.Windows.Forms.Label();
            this.comboScoringModel = new System.Windows.Forms.ComboBox();
            this.lblScoringModel = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.radioPValue = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.groupBoxPeakAcceptance.SuspendLayout();
            this.groupBoxCutoff.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCoreResults)).BeginInit();
            this.SuspendLayout();
            // 
            // databoundGridControl
            // 
            this.databoundGridControl.Location = new System.Drawing.Point(0, 181);
            this.databoundGridControl.Size = new System.Drawing.Size(691, 378);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tbxMeanStandardDeviation);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.alignmentControl);
            this.panel1.Controls.Add(this.comboImputeBoundariesFrom);
            this.panel1.Controls.Add(this.lblImputeBoundariesFrom);
            this.panel1.Controls.Add(this.comboManualPeaks);
            this.panel1.Controls.Add(this.lblManualPeaks);
            this.panel1.Controls.Add(this.btnImputeBoundaries);
            this.panel1.Controls.Add(this.groupBoxPeakAcceptance);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(691, 181);
            this.panel1.TabIndex = 1;
            // 
            // tbxMeanStandardDeviation
            // 
            this.tbxMeanStandardDeviation.Location = new System.Drawing.Point(518, 150);
            this.tbxMeanStandardDeviation.Name = "tbxMeanStandardDeviation";
            this.tbxMeanStandardDeviation.ReadOnly = true;
            this.tbxMeanStandardDeviation.Size = new System.Drawing.Size(100, 20);
            this.tbxMeanStandardDeviation.TabIndex = 23;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(515, 132);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "Mean Standard Deviation:";
            // 
            // alignmentControl
            // 
            this.alignmentControl.AlignmentTarget = null;
            this.alignmentControl.DocumentUiContainer = null;
            this.alignmentControl.Location = new System.Drawing.Point(303, 3);
            this.alignmentControl.Name = "alignmentControl";
            this.alignmentControl.RegressionMethodRT = pwiz.Skyline.Model.RetentionTimes.RegressionMethodRT.linear;
            this.alignmentControl.RtValueType = null;
            this.alignmentControl.Size = new System.Drawing.Size(206, 157);
            this.alignmentControl.TabIndex = 21;
            this.alignmentControl.TargetFile = null;
            this.alignmentControl.AlignmentTargetChange += new System.EventHandler(this.SettingsControlChanged);
            // 
            // comboImputeBoundariesFrom
            // 
            this.comboImputeBoundariesFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboImputeBoundariesFrom.FormattingEnabled = true;
            this.comboImputeBoundariesFrom.Items.AddRange(new object[] {
            "Best scoring peak",
            "All accepted peaks"});
            this.comboImputeBoundariesFrom.Location = new System.Drawing.Point(518, 66);
            this.comboImputeBoundariesFrom.Name = "comboImputeBoundariesFrom";
            this.comboImputeBoundariesFrom.Size = new System.Drawing.Size(152, 21);
            this.comboImputeBoundariesFrom.TabIndex = 20;
            this.comboImputeBoundariesFrom.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblImputeBoundariesFrom
            // 
            this.lblImputeBoundariesFrom.AutoSize = true;
            this.lblImputeBoundariesFrom.Location = new System.Drawing.Point(515, 47);
            this.lblImputeBoundariesFrom.Name = "lblImputeBoundariesFrom";
            this.lblImputeBoundariesFrom.Size = new System.Drawing.Size(120, 13);
            this.lblImputeBoundariesFrom.TabIndex = 19;
            this.lblImputeBoundariesFrom.Text = "Impute boundaries from:";
            // 
            // comboManualPeaks
            // 
            this.comboManualPeaks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboManualPeaks.FormattingEnabled = true;
            this.comboManualPeaks.Location = new System.Drawing.Point(518, 111);
            this.comboManualPeaks.Name = "comboManualPeaks";
            this.comboManualPeaks.Size = new System.Drawing.Size(161, 21);
            this.comboManualPeaks.TabIndex = 17;
            this.toolTip1.SetToolTip(this.comboManualPeaks, resources.GetString("comboManualPeaks.ToolTip"));
            this.comboManualPeaks.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblManualPeaks
            // 
            this.lblManualPeaks.AutoSize = true;
            this.lblManualPeaks.Location = new System.Drawing.Point(515, 94);
            this.lblManualPeaks.Name = "lblManualPeaks";
            this.lblManualPeaks.Size = new System.Drawing.Size(131, 13);
            this.lblManualPeaks.TabIndex = 16;
            this.lblManualPeaks.Text = "Manually integrated peaks";
            // 
            // btnImputeBoundaries
            // 
            this.btnImputeBoundaries.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImputeBoundaries.Location = new System.Drawing.Point(539, 16);
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
            this.groupBoxPeakAcceptance.Controls.Add(this.groupBoxCutoff);
            this.groupBoxPeakAcceptance.Controls.Add(this.tbxRtDeviationCutoff);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblMinCoreCount);
            this.groupBoxPeakAcceptance.Controls.Add(this.numericUpDownCoreResults);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblSdCutoff);
            this.groupBoxPeakAcceptance.Controls.Add(this.comboScoringModel);
            this.groupBoxPeakAcceptance.Controls.Add(this.lblScoringModel);
            this.groupBoxPeakAcceptance.Location = new System.Drawing.Point(12, 10);
            this.groupBoxPeakAcceptance.Name = "groupBoxPeakAcceptance";
            this.groupBoxPeakAcceptance.Size = new System.Drawing.Size(285, 160);
            this.groupBoxPeakAcceptance.TabIndex = 14;
            this.groupBoxPeakAcceptance.TabStop = false;
            this.groupBoxPeakAcceptance.Text = "Peak acceptance";
            // 
            // groupBoxCutoff
            // 
            this.groupBoxCutoff.Controls.Add(this.radioPValue);
            this.groupBoxCutoff.Controls.Add(this.radioPercentile);
            this.groupBoxCutoff.Controls.Add(this.radioQValue);
            this.groupBoxCutoff.Controls.Add(this.radioScore);
            this.groupBoxCutoff.Controls.Add(this.tbxCoreScoreCutoff);
            this.groupBoxCutoff.Location = new System.Drawing.Point(146, 16);
            this.groupBoxCutoff.Name = "groupBoxCutoff";
            this.groupBoxCutoff.Size = new System.Drawing.Size(133, 138);
            this.groupBoxCutoff.TabIndex = 12;
            this.groupBoxCutoff.TabStop = false;
            this.groupBoxCutoff.Text = "Cutoff";
            // 
            // radioPercentile
            // 
            this.radioPercentile.AutoSize = true;
            this.radioPercentile.Location = new System.Drawing.Point(6, 89);
            this.radioPercentile.Name = "radioPercentile";
            this.radioPercentile.Size = new System.Drawing.Size(72, 17);
            this.radioPercentile.TabIndex = 2;
            this.radioPercentile.TabStop = true;
            this.radioPercentile.Text = "Percentile";
            this.radioPercentile.UseVisualStyleBackColor = true;
            this.radioPercentile.CheckedChanged += new System.EventHandler(this.CutoffTypeChanged);
            // 
            // radioQValue
            // 
            this.radioQValue.AutoSize = true;
            this.radioQValue.Location = new System.Drawing.Point(6, 66);
            this.radioQValue.Name = "radioQValue";
            this.radioQValue.Size = new System.Drawing.Size(62, 17);
            this.radioQValue.TabIndex = 1;
            this.radioQValue.TabStop = true;
            this.radioQValue.Text = "Q-value";
            this.radioQValue.UseVisualStyleBackColor = true;
            this.radioQValue.CheckedChanged += new System.EventHandler(this.CutoffTypeChanged);
            // 
            // radioScore
            // 
            this.radioScore.AutoSize = true;
            this.radioScore.Location = new System.Drawing.Point(6, 19);
            this.radioScore.Name = "radioScore";
            this.radioScore.Size = new System.Drawing.Size(53, 17);
            this.radioScore.TabIndex = 0;
            this.radioScore.TabStop = true;
            this.radioScore.Text = "Score";
            this.radioScore.UseVisualStyleBackColor = true;
            this.radioScore.CheckedChanged += new System.EventHandler(this.CutoffTypeChanged);
            // 
            // tbxCoreScoreCutoff
            // 
            this.tbxCoreScoreCutoff.Location = new System.Drawing.Point(6, 112);
            this.tbxCoreScoreCutoff.Name = "tbxCoreScoreCutoff";
            this.tbxCoreScoreCutoff.Size = new System.Drawing.Size(128, 20);
            this.tbxCoreScoreCutoff.TabIndex = 9;
            this.toolTip1.SetToolTip(this.tbxCoreScoreCutoff, "Score above which a peak will always be accepted");
            this.tbxCoreScoreCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // tbxRtDeviationCutoff
            // 
            this.tbxRtDeviationCutoff.Location = new System.Drawing.Point(9, 120);
            this.tbxRtDeviationCutoff.Name = "tbxRtDeviationCutoff";
            this.tbxRtDeviationCutoff.Size = new System.Drawing.Size(128, 20);
            this.tbxRtDeviationCutoff.TabIndex = 11;
            this.tbxRtDeviationCutoff.Text = "1";
            this.toolTip1.SetToolTip(this.tbxRtDeviationCutoff, "Peaks whose boundaries are within this distance from the accepted peaks will also" +
        " be accepted");
            this.tbxRtDeviationCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
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
            this.numericUpDownCoreResults.Size = new System.Drawing.Size(128, 20);
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
            this.lblSdCutoff.Location = new System.Drawing.Point(6, 104);
            this.lblSdCutoff.Name = "lblSdCutoff";
            this.lblSdCutoff.Size = new System.Drawing.Size(125, 13);
            this.lblSdCutoff.TabIndex = 10;
            this.lblSdCutoff.Text = "Retention time difference";
            this.toolTip1.SetToolTip(this.lblSdCutoff, "Peaks which are within this distance of the accepted peaks will also be accepted");
            // 
            // comboScoringModel
            // 
            this.comboScoringModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboScoringModel.FormattingEnabled = true;
            this.comboScoringModel.Location = new System.Drawing.Point(6, 32);
            this.comboScoringModel.Name = "comboScoringModel";
            this.comboScoringModel.Size = new System.Drawing.Size(128, 21);
            this.comboScoringModel.TabIndex = 5;
            this.toolTip1.SetToolTip(this.comboScoringModel, resources.GetString("comboScoringModel.ToolTip"));
            this.comboScoringModel.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
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
            // radioPValue
            // 
            this.radioPValue.AutoSize = true;
            this.radioPValue.Location = new System.Drawing.Point(6, 42);
            this.radioPValue.Name = "radioPValue";
            this.radioPValue.Size = new System.Drawing.Size(61, 17);
            this.radioPValue.TabIndex = 10;
            this.radioPValue.TabStop = true;
            this.radioPValue.Text = "P-value";
            this.radioPValue.UseVisualStyleBackColor = true;
            this.radioPValue.CheckedChanged += new System.EventHandler(this.CutoffTypeChanged);
            // 
            // PeakImputationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(691, 559);
            this.Controls.Add(this.panel1);
            this.Name = "PeakImputationForm";
            this.TabText = "Peak Imputation";
            this.Text = "Peak Imputation";
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.databoundGridControl, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBoxPeakAcceptance.ResumeLayout(false);
            this.groupBoxPeakAcceptance.PerformLayout();
            this.groupBoxCutoff.ResumeLayout(false);
            this.groupBoxCutoff.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCoreResults)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboScoringModel;
        private System.Windows.Forms.Label lblScoringModel;
        private System.Windows.Forms.TextBox tbxCoreScoreCutoff;
        private System.Windows.Forms.NumericUpDown numericUpDownCoreResults;
        private System.Windows.Forms.Label lblMinCoreCount;
        private System.Windows.Forms.TextBox tbxRtDeviationCutoff;
        private System.Windows.Forms.Label lblSdCutoff;
        private System.Windows.Forms.Button btnImputeBoundaries;
        private System.Windows.Forms.GroupBox groupBoxPeakAcceptance;
        private System.Windows.Forms.ComboBox comboManualPeaks;
        private System.Windows.Forms.Label lblManualPeaks;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox comboImputeBoundariesFrom;
        private System.Windows.Forms.Label lblImputeBoundariesFrom;
        private AlignmentControl alignmentControl;
        private System.Windows.Forms.TextBox tbxMeanStandardDeviation;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBoxCutoff;
        private System.Windows.Forms.RadioButton radioQValue;
        private System.Windows.Forms.RadioButton radioScore;
        private System.Windows.Forms.RadioButton radioPercentile;
        private System.Windows.Forms.RadioButton radioPValue;
    }
}