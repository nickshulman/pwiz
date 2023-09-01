namespace pwiz.Skyline.Controls.Clustering
{
    partial class PcaPlot
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PcaPlot));
            this.lblXAxis = new System.Windows.Forms.Label();
            this.numericUpDownXAxis = new System.Windows.Forms.NumericUpDown();
            this.lblYAxis = new System.Windows.Forms.Label();
            this.numericUpDownYAxis = new System.Windows.Forms.NumericUpDown();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.lblDataset = new System.Windows.Forms.Label();
            this.comboDataset = new System.Windows.Forms.ComboBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.legendGraphControl = new ZedGraph.ZedGraphControl();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownXAxis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownYAxis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblXAxis
            // 
            resources.ApplyResources(this.lblXAxis, "lblXAxis");
            this.lblXAxis.Name = "lblXAxis";
            // 
            // numericUpDownXAxis
            // 
            resources.ApplyResources(this.numericUpDownXAxis, "numericUpDownXAxis");
            this.numericUpDownXAxis.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownXAxis.Name = "numericUpDownXAxis";
            this.numericUpDownXAxis.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownXAxis.ValueChanged += new System.EventHandler(this.numericUpDownX_ValueChanged);
            // 
            // lblYAxis
            // 
            resources.ApplyResources(this.lblYAxis, "lblYAxis");
            this.lblYAxis.Name = "lblYAxis";
            // 
            // numericUpDownYAxis
            // 
            resources.ApplyResources(this.numericUpDownYAxis, "numericUpDownYAxis");
            this.numericUpDownYAxis.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownYAxis.Name = "numericUpDownYAxis";
            this.numericUpDownYAxis.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDownYAxis.ValueChanged += new System.EventHandler(this.numericUpDownY_ValueChanged);
            // 
            // zedGraphControl1
            // 
            resources.ApplyResources(this.zedGraphControl1, "zedGraphControl1");
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.MouseDownEvent += new ZedGraph.ZedGraphControl.ZedMouseEventHandler(this.zedGraphControl1_MouseDownEvent);
            this.zedGraphControl1.MouseMoveEvent += new ZedGraph.ZedGraphControl.ZedMouseEventHandler(this.zedGraphControl1_MouseMoveEvent);
            // 
            // lblDataset
            // 
            resources.ApplyResources(this.lblDataset, "lblDataset");
            this.lblDataset.Name = "lblDataset";
            // 
            // comboDataset
            // 
            resources.ApplyResources(this.comboDataset, "comboDataset");
            this.comboDataset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDataset.FormattingEnabled = true;
            this.comboDataset.Name = "comboDataset";
            this.comboDataset.SelectedIndexChanged += new System.EventHandler(this.comboDataset_SelectedIndexChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.zedGraphControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.legendGraphControl);
            // 
            // legendGraphControl
            // 
            resources.ApplyResources(this.legendGraphControl, "legendGraphControl");
            this.legendGraphControl.Name = "legendGraphControl";
            this.legendGraphControl.ScrollGrace = 0D;
            this.legendGraphControl.ScrollMaxX = 0D;
            this.legendGraphControl.ScrollMaxY = 0D;
            this.legendGraphControl.ScrollMaxY2 = 0D;
            this.legendGraphControl.ScrollMinX = 0D;
            this.legendGraphControl.ScrollMinY = 0D;
            this.legendGraphControl.ScrollMinY2 = 0D;
            // 
            // PcaPlot
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.comboDataset);
            this.Controls.Add(this.lblDataset);
            this.Controls.Add(this.numericUpDownYAxis);
            this.Controls.Add(this.lblYAxis);
            this.Controls.Add(this.numericUpDownXAxis);
            this.Controls.Add(this.lblXAxis);
            this.Name = "PcaPlot";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownXAxis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownYAxis)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblXAxis;
        private System.Windows.Forms.NumericUpDown numericUpDownXAxis;
        private System.Windows.Forms.Label lblYAxis;
        private System.Windows.Forms.NumericUpDown numericUpDownYAxis;
        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.Label lblDataset;
        private System.Windows.Forms.ComboBox comboDataset;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ZedGraph.ZedGraphControl legendGraphControl;
    }
}