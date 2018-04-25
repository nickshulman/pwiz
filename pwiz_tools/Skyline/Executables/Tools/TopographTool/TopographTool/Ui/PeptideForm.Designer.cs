namespace TopographTool.Ui
{
    partial class PeptideForm
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea6 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend6 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series6 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tbxPeptide = new System.Windows.Forms.TextBox();
            this.lblPeptide = new System.Windows.Forms.Label();
            this.comboReplicate = new System.Windows.Forms.ComboBox();
            this.lblReplicate = new System.Windows.Forms.Label();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.dataGridViewFeatures = new System.Windows.Forms.DataGridView();
            this.listBoxTransitions = new System.Windows.Forms.ListBox();
            this.colTransition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFeature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFeatureAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewFeatures)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listBoxTransitions);
            this.splitContainer1.Panel1.Controls.Add(this.dataGridViewFeatures);
            this.splitContainer1.Panel1.Controls.Add(this.tbxPeptide);
            this.splitContainer1.Panel1.Controls.Add(this.lblPeptide);
            this.splitContainer1.Panel1.Controls.Add(this.comboReplicate);
            this.splitContainer1.Panel1.Controls.Add(this.lblReplicate);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.chart1);
            this.splitContainer1.Size = new System.Drawing.Size(831, 261);
            this.splitContainer1.SplitterDistance = 130;
            this.splitContainer1.TabIndex = 1;
            // 
            // tbxPeptide
            // 
            this.tbxPeptide.Location = new System.Drawing.Point(15, 35);
            this.tbxPeptide.Name = "tbxPeptide";
            this.tbxPeptide.ReadOnly = true;
            this.tbxPeptide.Size = new System.Drawing.Size(188, 20);
            this.tbxPeptide.TabIndex = 3;
            // 
            // lblPeptide
            // 
            this.lblPeptide.AutoSize = true;
            this.lblPeptide.Location = new System.Drawing.Point(15, 11);
            this.lblPeptide.Name = "lblPeptide";
            this.lblPeptide.Size = new System.Drawing.Size(46, 13);
            this.lblPeptide.TabIndex = 2;
            this.lblPeptide.Text = "Peptide:";
            // 
            // comboReplicate
            // 
            this.comboReplicate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboReplicate.FormattingEnabled = true;
            this.comboReplicate.Location = new System.Drawing.Point(12, 74);
            this.comboReplicate.Name = "comboReplicate";
            this.comboReplicate.Size = new System.Drawing.Size(191, 21);
            this.comboReplicate.TabIndex = 1;
            this.comboReplicate.SelectedIndexChanged += new System.EventHandler(this.comboReplicate_SelectedIndexChanged);
            // 
            // lblReplicate
            // 
            this.lblReplicate.AutoSize = true;
            this.lblReplicate.Location = new System.Drawing.Point(12, 58);
            this.lblReplicate.Name = "lblReplicate";
            this.lblReplicate.Size = new System.Drawing.Size(55, 13);
            this.lblReplicate.TabIndex = 0;
            this.lblReplicate.Text = "Replicate:";
            // 
            // chart1
            // 
            chartArea6.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea6);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend6.Name = "Legend1";
            this.chart1.Legends.Add(legend6);
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Name = "chart1";
            series6.ChartArea = "ChartArea1";
            series6.Legend = "Legend1";
            series6.Name = "Series1";
            this.chart1.Series.Add(series6);
            this.chart1.Size = new System.Drawing.Size(831, 127);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // dataGridViewFeatures
            // 
            this.dataGridViewFeatures.AllowUserToAddRows = false;
            this.dataGridViewFeatures.AllowUserToDeleteRows = false;
            this.dataGridViewFeatures.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewFeatures.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewFeatures.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTransition,
            this.colFeature,
            this.colFeatureAmount});
            this.dataGridViewFeatures.Location = new System.Drawing.Point(480, 11);
            this.dataGridViewFeatures.Name = "dataGridViewFeatures";
            this.dataGridViewFeatures.ReadOnly = true;
            this.dataGridViewFeatures.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewFeatures.Size = new System.Drawing.Size(339, 108);
            this.dataGridViewFeatures.TabIndex = 4;
            // 
            // listBoxTransitions
            // 
            this.listBoxTransitions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxTransitions.FormattingEnabled = true;
            this.listBoxTransitions.Location = new System.Drawing.Point(223, 11);
            this.listBoxTransitions.Name = "listBoxTransitions";
            this.listBoxTransitions.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxTransitions.Size = new System.Drawing.Size(251, 108);
            this.listBoxTransitions.TabIndex = 5;
            this.listBoxTransitions.SelectedIndexChanged += new System.EventHandler(this.listBoxTransitions_SelectedIndexChanged);
            // 
            // colTransition
            // 
            this.colTransition.HeaderText = "Transition";
            this.colTransition.Name = "colTransition";
            this.colTransition.ReadOnly = true;
            // 
            // colFeature
            // 
            this.colFeature.HeaderText = "Feature";
            this.colFeature.Name = "colFeature";
            this.colFeature.ReadOnly = true;
            // 
            // colFeatureAmount
            // 
            this.colFeatureAmount.HeaderText = "Amount";
            this.colFeatureAmount.Name = "colFeatureAmount";
            this.colFeatureAmount.ReadOnly = true;
            // 
            // PeptideForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(831, 261);
            this.Controls.Add(this.splitContainer1);
            this.Name = "PeptideForm";
            this.TabText = "PeptideForm";
            this.Text = "PeptideForm";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewFeatures)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.ComboBox comboReplicate;
        private System.Windows.Forms.Label lblReplicate;
        private System.Windows.Forms.TextBox tbxPeptide;
        private System.Windows.Forms.Label lblPeptide;
        private System.Windows.Forms.DataGridView dataGridViewFeatures;
        private System.Windows.Forms.ListBox listBoxTransitions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTransition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFeature;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFeatureAmount;
    }
}