﻿namespace pwiz.Skyline.EditUI
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
            this.cbxAlignAllGraphs = new System.Windows.Forms.CheckBox();
            this.groupBoxDocumentStatistics = new System.Windows.Forms.GroupBox();
            this.tbxAlignedDocRtStdDev = new System.Windows.Forms.TextBox();
            this.lblAligned = new System.Windows.Forms.Label();
            this.tbxUnalignedDocRtStdDev = new System.Windows.Forms.TextBox();
            this.lblUnaligned = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBoxScope = new System.Windows.Forms.GroupBox();
            this.radioScopeDocument = new System.Windows.Forms.RadioButton();
            this.radioScopeSelection = new System.Windows.Forms.RadioButton();
            this.tbxScoringModel = new System.Windows.Forms.TextBox();
            this.lblScoringModel = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.groupBoxResults = new System.Windows.Forms.GroupBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.lblNeedRemoval = new System.Windows.Forms.Label();
            this.tbxMeanRtStdDev = new System.Windows.Forms.TextBox();
            this.lblMeanRtStdDev = new System.Windows.Forms.Label();
            this.tbxExemplary = new System.Windows.Forms.TextBox();
            this.lblExemplary = new System.Windows.Forms.Label();
            this.tbxRejected = new System.Windows.Forms.TextBox();
            this.lblRejected = new System.Windows.Forms.Label();
            this.tbxAccepted = new System.Windows.Forms.TextBox();
            this.lblAccepted = new System.Windows.Forms.Label();
            this.btnImputeBoundaries = new System.Windows.Forms.Button();
            this.cbxOverwriteManual = new System.Windows.Forms.CheckBox();
            this.tbxRtDeviationCutoff = new System.Windows.Forms.TextBox();
            this.lblSdCutoff = new System.Windows.Forms.Label();
            this.groupBoxCutoff = new System.Windows.Forms.GroupBox();
            this.lblPercent = new System.Windows.Forms.Label();
            this.radioPValue = new System.Windows.Forms.RadioButton();
            this.radioPercentile = new System.Windows.Forms.RadioButton();
            this.radioQValue = new System.Windows.Forms.RadioButton();
            this.radioScore = new System.Windows.Forms.RadioButton();
            this.tbxCoreScoreCutoff = new System.Windows.Forms.TextBox();
            this.comboRetentionTimeAlignment = new System.Windows.Forms.ComboBox();
            this.lblRetentionTimeAlignment = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1.SuspendLayout();
            this.groupBoxDocumentStatistics.SuspendLayout();
            this.groupBoxScope.SuspendLayout();
            this.groupBoxResults.SuspendLayout();
            this.groupBoxCutoff.SuspendLayout();
            this.SuspendLayout();
            // 
            // databoundGridControl
            // 
            this.databoundGridControl.Location = new System.Drawing.Point(0, 223);
            this.databoundGridControl.Size = new System.Drawing.Size(800, 227);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cbxAlignAllGraphs);
            this.panel1.Controls.Add(this.groupBoxDocumentStatistics);
            this.panel1.Controls.Add(this.groupBoxScope);
            this.panel1.Controls.Add(this.tbxScoringModel);
            this.panel1.Controls.Add(this.lblScoringModel);
            this.panel1.Controls.Add(this.progressBar1);
            this.panel1.Controls.Add(this.groupBoxResults);
            this.panel1.Controls.Add(this.btnImputeBoundaries);
            this.panel1.Controls.Add(this.cbxOverwriteManual);
            this.panel1.Controls.Add(this.tbxRtDeviationCutoff);
            this.panel1.Controls.Add(this.lblSdCutoff);
            this.panel1.Controls.Add(this.groupBoxCutoff);
            this.panel1.Controls.Add(this.comboRetentionTimeAlignment);
            this.panel1.Controls.Add(this.lblRetentionTimeAlignment);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 223);
            this.panel1.TabIndex = 1;
            // 
            // cbxAlignAllGraphs
            // 
            this.cbxAlignAllGraphs.AutoSize = true;
            this.cbxAlignAllGraphs.Location = new System.Drawing.Point(181, 52);
            this.cbxAlignAllGraphs.Name = "cbxAlignAllGraphs";
            this.cbxAlignAllGraphs.Size = new System.Drawing.Size(97, 17);
            this.cbxAlignAllGraphs.TabIndex = 25;
            this.cbxAlignAllGraphs.Text = "Align all graphs";
            this.cbxAlignAllGraphs.UseVisualStyleBackColor = true;
            this.cbxAlignAllGraphs.CheckedChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // groupBoxDocumentStatistics
            // 
            this.groupBoxDocumentStatistics.Controls.Add(this.tbxAlignedDocRtStdDev);
            this.groupBoxDocumentStatistics.Controls.Add(this.lblAligned);
            this.groupBoxDocumentStatistics.Controls.Add(this.tbxUnalignedDocRtStdDev);
            this.groupBoxDocumentStatistics.Controls.Add(this.lblUnaligned);
            this.groupBoxDocumentStatistics.Controls.Add(this.label1);
            this.groupBoxDocumentStatistics.Location = new System.Drawing.Point(463, 9);
            this.groupBoxDocumentStatistics.Name = "groupBoxDocumentStatistics";
            this.groupBoxDocumentStatistics.Size = new System.Drawing.Size(164, 149);
            this.groupBoxDocumentStatistics.TabIndex = 24;
            this.groupBoxDocumentStatistics.TabStop = false;
            this.groupBoxDocumentStatistics.Text = "Document-wide statistics";
            // 
            // tbxAlignedDocRtStdDev
            // 
            this.tbxAlignedDocRtStdDev.Location = new System.Drawing.Point(9, 109);
            this.tbxAlignedDocRtStdDev.Name = "tbxAlignedDocRtStdDev";
            this.tbxAlignedDocRtStdDev.ReadOnly = true;
            this.tbxAlignedDocRtStdDev.Size = new System.Drawing.Size(100, 20);
            this.tbxAlignedDocRtStdDev.TabIndex = 4;
            // 
            // lblAligned
            // 
            this.lblAligned.AutoSize = true;
            this.lblAligned.Location = new System.Drawing.Point(6, 93);
            this.lblAligned.Name = "lblAligned";
            this.lblAligned.Size = new System.Drawing.Size(45, 13);
            this.lblAligned.TabIndex = 3;
            this.lblAligned.Text = "Aligned:";
            // 
            // tbxUnalignedDocRtStdDev
            // 
            this.tbxUnalignedDocRtStdDev.Location = new System.Drawing.Point(9, 70);
            this.tbxUnalignedDocRtStdDev.Name = "tbxUnalignedDocRtStdDev";
            this.tbxUnalignedDocRtStdDev.ReadOnly = true;
            this.tbxUnalignedDocRtStdDev.Size = new System.Drawing.Size(100, 20);
            this.tbxUnalignedDocRtStdDev.TabIndex = 2;
            // 
            // lblUnaligned
            // 
            this.lblUnaligned.AutoSize = true;
            this.lblUnaligned.Location = new System.Drawing.Point(6, 54);
            this.lblUnaligned.Name = "lblUnaligned";
            this.lblUnaligned.Size = new System.Drawing.Size(58, 13);
            this.lblUnaligned.TabIndex = 1;
            this.lblUnaligned.Text = "Unaligned:";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 39);
            this.label1.TabIndex = 0;
            this.label1.Text = "Average retenion time standard deviation";
            // 
            // groupBoxScope
            // 
            this.groupBoxScope.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxScope.Controls.Add(this.radioScopeDocument);
            this.groupBoxScope.Controls.Add(this.radioScopeSelection);
            this.groupBoxScope.Location = new System.Drawing.Point(656, 43);
            this.groupBoxScope.Name = "groupBoxScope";
            this.groupBoxScope.Size = new System.Drawing.Size(141, 68);
            this.groupBoxScope.TabIndex = 23;
            this.groupBoxScope.TabStop = false;
            this.groupBoxScope.Text = "Scope";
            // 
            // radioScopeDocument
            // 
            this.radioScopeDocument.AutoSize = true;
            this.radioScopeDocument.Location = new System.Drawing.Point(10, 39);
            this.radioScopeDocument.Name = "radioScopeDocument";
            this.radioScopeDocument.Size = new System.Drawing.Size(74, 17);
            this.radioScopeDocument.TabIndex = 1;
            this.radioScopeDocument.Text = "Document";
            this.radioScopeDocument.UseVisualStyleBackColor = true;
            this.radioScopeDocument.Click += new System.EventHandler(this.SettingsControlChanged);
            // 
            // radioScopeSelection
            // 
            this.radioScopeSelection.AutoSize = true;
            this.radioScopeSelection.Checked = true;
            this.radioScopeSelection.Location = new System.Drawing.Point(10, 16);
            this.radioScopeSelection.Name = "radioScopeSelection";
            this.radioScopeSelection.Size = new System.Drawing.Size(69, 17);
            this.radioScopeSelection.TabIndex = 0;
            this.radioScopeSelection.TabStop = true;
            this.radioScopeSelection.Text = "Selection";
            this.radioScopeSelection.UseVisualStyleBackColor = true;
            this.radioScopeSelection.Click += new System.EventHandler(this.SettingsControlChanged);
            // 
            // tbxScoringModel
            // 
            this.tbxScoringModel.Location = new System.Drawing.Point(12, 26);
            this.tbxScoringModel.Name = "tbxScoringModel";
            this.tbxScoringModel.ReadOnly = true;
            this.tbxScoringModel.Size = new System.Drawing.Size(142, 20);
            this.tbxScoringModel.TabIndex = 22;
            this.toolTip1.SetToolTip(this.tbxScoringModel, "Scoring model used to determine best peaks.\r\nUse the \"Refine > Reintegrate\" menu " +
        "item to choose a different model.");
            // 
            // lblScoringModel
            // 
            this.lblScoringModel.AutoSize = true;
            this.lblScoringModel.Location = new System.Drawing.Point(12, 9);
            this.lblScoringModel.Name = "lblScoringModel";
            this.lblScoringModel.Size = new System.Drawing.Size(77, 13);
            this.lblScoringModel.TabIndex = 21;
            this.lblScoringModel.Text = "Scoring model:";
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(463, 194);
            this.progressBar1.Maximum = 10000;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(334, 23);
            this.progressBar1.TabIndex = 19;
            // 
            // groupBoxResults
            // 
            this.groupBoxResults.Controls.Add(this.textBox1);
            this.groupBoxResults.Controls.Add(this.lblNeedRemoval);
            this.groupBoxResults.Controls.Add(this.tbxMeanRtStdDev);
            this.groupBoxResults.Controls.Add(this.lblMeanRtStdDev);
            this.groupBoxResults.Controls.Add(this.tbxExemplary);
            this.groupBoxResults.Controls.Add(this.lblExemplary);
            this.groupBoxResults.Controls.Add(this.tbxRejected);
            this.groupBoxResults.Controls.Add(this.lblRejected);
            this.groupBoxResults.Controls.Add(this.tbxAccepted);
            this.groupBoxResults.Controls.Add(this.lblAccepted);
            this.groupBoxResults.Location = new System.Drawing.Point(336, 9);
            this.groupBoxResults.Name = "groupBoxResults";
            this.groupBoxResults.Size = new System.Drawing.Size(121, 208);
            this.groupBoxResults.TabIndex = 18;
            this.groupBoxResults.TabStop = false;
            this.groupBoxResults.Text = "Results";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(6, 149);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(84, 20);
            this.textBox1.TabIndex = 11;
            // 
            // lblNeedRemoval
            // 
            this.lblNeedRemoval.AutoSize = true;
            this.lblNeedRemoval.Location = new System.Drawing.Point(6, 133);
            this.lblNeedRemoval.Name = "lblNeedRemoval";
            this.lblNeedRemoval.Size = new System.Drawing.Size(76, 13);
            this.lblNeedRemoval.TabIndex = 10;
            this.lblNeedRemoval.Text = "Need removal:";
            // 
            // tbxMeanRtStdDev
            // 
            this.tbxMeanRtStdDev.Location = new System.Drawing.Point(6, 184);
            this.tbxMeanRtStdDev.Name = "tbxMeanRtStdDev";
            this.tbxMeanRtStdDev.ReadOnly = true;
            this.tbxMeanRtStdDev.Size = new System.Drawing.Size(84, 20);
            this.tbxMeanRtStdDev.TabIndex = 9;
            // 
            // lblMeanRtStdDev
            // 
            this.lblMeanRtStdDev.AutoSize = true;
            this.lblMeanRtStdDev.Location = new System.Drawing.Point(6, 168);
            this.lblMeanRtStdDev.Name = "lblMeanRtStdDev";
            this.lblMeanRtStdDev.Size = new System.Drawing.Size(107, 13);
            this.lblMeanRtStdDev.TabIndex = 8;
            this.lblMeanRtStdDev.Text = "Average RT StdDev:";
            // 
            // tbxExemplary
            // 
            this.tbxExemplary.Location = new System.Drawing.Point(6, 32);
            this.tbxExemplary.Name = "tbxExemplary";
            this.tbxExemplary.ReadOnly = true;
            this.tbxExemplary.Size = new System.Drawing.Size(84, 20);
            this.tbxExemplary.TabIndex = 7;
            // 
            // lblExemplary
            // 
            this.lblExemplary.AutoSize = true;
            this.lblExemplary.Location = new System.Drawing.Point(6, 16);
            this.lblExemplary.Name = "lblExemplary";
            this.lblExemplary.Size = new System.Drawing.Size(58, 13);
            this.lblExemplary.TabIndex = 6;
            this.lblExemplary.Text = "Exemplary:";
            // 
            // tbxRejected
            // 
            this.tbxRejected.Location = new System.Drawing.Point(6, 110);
            this.tbxRejected.Name = "tbxRejected";
            this.tbxRejected.ReadOnly = true;
            this.tbxRejected.Size = new System.Drawing.Size(84, 20);
            this.tbxRejected.TabIndex = 3;
            // 
            // lblRejected
            // 
            this.lblRejected.AutoSize = true;
            this.lblRejected.Location = new System.Drawing.Point(6, 94);
            this.lblRejected.Name = "lblRejected";
            this.lblRejected.Size = new System.Drawing.Size(90, 13);
            this.lblRejected.TabIndex = 2;
            this.lblRejected.Text = "Need adjustment:";
            // 
            // tbxAccepted
            // 
            this.tbxAccepted.Location = new System.Drawing.Point(6, 71);
            this.tbxAccepted.Name = "tbxAccepted";
            this.tbxAccepted.ReadOnly = true;
            this.tbxAccepted.Size = new System.Drawing.Size(84, 20);
            this.tbxAccepted.TabIndex = 1;
            // 
            // lblAccepted
            // 
            this.lblAccepted.AutoSize = true;
            this.lblAccepted.Location = new System.Drawing.Point(6, 55);
            this.lblAccepted.Name = "lblAccepted";
            this.lblAccepted.Size = new System.Drawing.Size(56, 13);
            this.lblAccepted.TabIndex = 0;
            this.lblAccepted.Text = "Accepted:";
            // 
            // btnImputeBoundaries
            // 
            this.btnImputeBoundaries.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImputeBoundaries.Location = new System.Drawing.Point(656, 12);
            this.btnImputeBoundaries.Name = "btnImputeBoundaries";
            this.btnImputeBoundaries.Size = new System.Drawing.Size(132, 23);
            this.btnImputeBoundaries.TabIndex = 17;
            this.btnImputeBoundaries.Text = "Impute Boundaries";
            this.toolTip1.SetToolTip(this.btnImputeBoundaries, "Choose new peak boundaries for the rejected peaks in the displayed rows");
            this.btnImputeBoundaries.UseVisualStyleBackColor = true;
            this.btnImputeBoundaries.Click += new System.EventHandler(this.btnImputeBoundaries_Click);
            // 
            // cbxOverwriteManual
            // 
            this.cbxOverwriteManual.AutoSize = true;
            this.cbxOverwriteManual.Location = new System.Drawing.Point(181, 142);
            this.cbxOverwriteManual.Name = "cbxOverwriteManual";
            this.cbxOverwriteManual.Size = new System.Drawing.Size(140, 17);
            this.cbxOverwriteManual.TabIndex = 16;
            this.cbxOverwriteManual.Text = "Overwrite manual peaks";
            this.cbxOverwriteManual.UseVisualStyleBackColor = true;
            this.cbxOverwriteManual.CheckedChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // tbxRtDeviationCutoff
            // 
            this.tbxRtDeviationCutoff.Location = new System.Drawing.Point(181, 111);
            this.tbxRtDeviationCutoff.Name = "tbxRtDeviationCutoff";
            this.tbxRtDeviationCutoff.Size = new System.Drawing.Size(127, 20);
            this.tbxRtDeviationCutoff.TabIndex = 15;
            this.tbxRtDeviationCutoff.Text = "1";
            this.toolTip1.SetToolTip(this.tbxRtDeviationCutoff, "Peaks whose retention time is less than this distance from the accepted peaks wil" +
        "l also be assumed to be correct.");
            this.tbxRtDeviationCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblSdCutoff
            // 
            this.lblSdCutoff.AutoSize = true;
            this.lblSdCutoff.Location = new System.Drawing.Point(178, 91);
            this.lblSdCutoff.Name = "lblSdCutoff";
            this.lblSdCutoff.Size = new System.Drawing.Size(106, 13);
            this.lblSdCutoff.TabIndex = 14;
            this.lblSdCutoff.Text = "Max RT shift minutes";
            // 
            // groupBoxCutoff
            // 
            this.groupBoxCutoff.Controls.Add(this.lblPercent);
            this.groupBoxCutoff.Controls.Add(this.radioPValue);
            this.groupBoxCutoff.Controls.Add(this.radioPercentile);
            this.groupBoxCutoff.Controls.Add(this.radioQValue);
            this.groupBoxCutoff.Controls.Add(this.radioScore);
            this.groupBoxCutoff.Controls.Add(this.tbxCoreScoreCutoff);
            this.groupBoxCutoff.Location = new System.Drawing.Point(12, 52);
            this.groupBoxCutoff.Name = "groupBoxCutoff";
            this.groupBoxCutoff.Size = new System.Drawing.Size(142, 138);
            this.groupBoxCutoff.TabIndex = 13;
            this.groupBoxCutoff.TabStop = false;
            this.groupBoxCutoff.Text = "Exemplary Cutoff";
            // 
            // lblPercent
            // 
            this.lblPercent.AutoSize = true;
            this.lblPercent.Location = new System.Drawing.Point(118, 116);
            this.lblPercent.Name = "lblPercent";
            this.lblPercent.Size = new System.Drawing.Size(15, 13);
            this.lblPercent.TabIndex = 11;
            this.lblPercent.Text = "%";
            this.lblPercent.Visible = false;
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
            this.radioQValue.Size = new System.Drawing.Size(94, 17);
            this.radioQValue.TabIndex = 1;
            this.radioQValue.TabStop = true;
            this.radioQValue.Text = "Library q-value";
            this.radioQValue.UseVisualStyleBackColor = true;
            this.radioQValue.CheckedChanged += new System.EventHandler(this.CutoffTypeChanged);
            // 
            // radioScore
            // 
            this.radioScore.AutoSize = true;
            this.radioScore.Checked = true;
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
            this.tbxCoreScoreCutoff.Size = new System.Drawing.Size(106, 20);
            this.tbxCoreScoreCutoff.TabIndex = 9;
            this.toolTip1.SetToolTip(this.tbxCoreScoreCutoff, resources.GetString("tbxCoreScoreCutoff.ToolTip"));
            this.tbxCoreScoreCutoff.Leave += new System.EventHandler(this.SettingsControlChanged);
            // 
            // comboRetentionTimeAlignment
            // 
            this.comboRetentionTimeAlignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRetentionTimeAlignment.FormattingEnabled = true;
            this.comboRetentionTimeAlignment.Location = new System.Drawing.Point(181, 25);
            this.comboRetentionTimeAlignment.Name = "comboRetentionTimeAlignment";
            this.comboRetentionTimeAlignment.Size = new System.Drawing.Size(127, 21);
            this.comboRetentionTimeAlignment.TabIndex = 1;
            this.toolTip1.SetToolTip(this.comboRetentionTimeAlignment, "The retention time alignment setting controls how the times from the accepted pea" +
        "ks are mapped onto the runs where a new peak needs to be chosen.");
            this.comboRetentionTimeAlignment.SelectedIndexChanged += new System.EventHandler(this.SettingsControlChanged);
            // 
            // lblRetentionTimeAlignment
            // 
            this.lblRetentionTimeAlignment.AutoSize = true;
            this.lblRetentionTimeAlignment.Location = new System.Drawing.Point(178, 9);
            this.lblRetentionTimeAlignment.Name = "lblRetentionTimeAlignment";
            this.lblRetentionTimeAlignment.Size = new System.Drawing.Size(126, 13);
            this.lblRetentionTimeAlignment.TabIndex = 0;
            this.lblRetentionTimeAlignment.Text = "Retention time alignment:";
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
            this.groupBoxDocumentStatistics.ResumeLayout(false);
            this.groupBoxDocumentStatistics.PerformLayout();
            this.groupBoxScope.ResumeLayout(false);
            this.groupBoxScope.PerformLayout();
            this.groupBoxResults.ResumeLayout(false);
            this.groupBoxResults.PerformLayout();
            this.groupBoxCutoff.ResumeLayout(false);
            this.groupBoxCutoff.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblRetentionTimeAlignment;
        private System.Windows.Forms.ComboBox comboRetentionTimeAlignment;
        private System.Windows.Forms.TextBox tbxRtDeviationCutoff;
        private System.Windows.Forms.Label lblSdCutoff;
        private System.Windows.Forms.GroupBox groupBoxCutoff;
        private System.Windows.Forms.RadioButton radioPValue;
        private System.Windows.Forms.RadioButton radioPercentile;
        private System.Windows.Forms.RadioButton radioQValue;
        private System.Windows.Forms.RadioButton radioScore;
        private System.Windows.Forms.TextBox tbxCoreScoreCutoff;
        private System.Windows.Forms.CheckBox cbxOverwriteManual;
        private System.Windows.Forms.Button btnImputeBoundaries;
        private System.Windows.Forms.GroupBox groupBoxResults;
        private System.Windows.Forms.TextBox tbxRejected;
        private System.Windows.Forms.Label lblRejected;
        private System.Windows.Forms.TextBox tbxAccepted;
        private System.Windows.Forms.Label lblAccepted;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblExemplary;
        private System.Windows.Forms.TextBox tbxExemplary;
        private System.Windows.Forms.TextBox tbxScoringModel;
        private System.Windows.Forms.Label lblScoringModel;
        private System.Windows.Forms.GroupBox groupBoxScope;
        private System.Windows.Forms.RadioButton radioScopeDocument;
        private System.Windows.Forms.RadioButton radioScopeSelection;
        private System.Windows.Forms.Label lblPercent;
        private System.Windows.Forms.GroupBox groupBoxDocumentStatistics;
        private System.Windows.Forms.Label lblUnaligned;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxAlignedDocRtStdDev;
        private System.Windows.Forms.Label lblAligned;
        private System.Windows.Forms.TextBox tbxUnalignedDocRtStdDev;
        private System.Windows.Forms.TextBox tbxMeanRtStdDev;
        private System.Windows.Forms.Label lblMeanRtStdDev;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label lblNeedRemoval;
        private System.Windows.Forms.CheckBox cbxAlignAllGraphs;
    }
}