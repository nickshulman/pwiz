namespace pwiz.Skyline.Controls
{
    partial class PerfCounterGridForm
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lblMemoryUsage = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.boundDataGridViewEx1 = new pwiz.Skyline.Controls.Databinding.BoundDataGridViewEx();
            this.bindingListSource1 = new pwiz.Common.DataBinding.Controls.BindingListSource(this.components);
            this.navBar1 = new pwiz.Common.DataBinding.Controls.NavBar();
            this.panelMemoryBar = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.boundDataGridViewEx1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingListSource1)).BeginInit();
            this.panelMemoryBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panelMemoryBar);
            this.splitContainer1.Panel1.Controls.Add(this.btnReset);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.boundDataGridViewEx1);
            this.splitContainer1.Panel2.Controls.Add(this.navBar1);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterDistance = 49;
            this.splitContainer1.TabIndex = 2;
            // 
            // lblMemoryUsage
            // 
            this.lblMemoryUsage.AutoSize = true;
            this.lblMemoryUsage.BackColor = System.Drawing.Color.Transparent;
            this.lblMemoryUsage.Location = new System.Drawing.Point(3, 5);
            this.lblMemoryUsage.Name = "lblMemoryUsage";
            this.lblMemoryUsage.Size = new System.Drawing.Size(175, 13);
            this.lblMemoryUsage.TabIndex = 0;
            this.lblMemoryUsage.Text = "Skyline: Total Used: Total Available";
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReset.Location = new System.Drawing.Point(672, 9);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(116, 23);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "Reset Counters";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // boundDataGridViewEx1
            // 
            this.boundDataGridViewEx1.AutoGenerateColumns = false;
            this.boundDataGridViewEx1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.boundDataGridViewEx1.DataSource = this.bindingListSource1;
            this.boundDataGridViewEx1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.boundDataGridViewEx1.Location = new System.Drawing.Point(0, 25);
            this.boundDataGridViewEx1.MaximumColumnCount = 2000;
            this.boundDataGridViewEx1.Name = "boundDataGridViewEx1";
            this.boundDataGridViewEx1.ReportColorScheme = null;
            this.boundDataGridViewEx1.Size = new System.Drawing.Size(800, 372);
            this.boundDataGridViewEx1.TabIndex = 1;
            // 
            // bindingListSource1
            // 
            this.bindingListSource1.NewRowHandler = null;
            // 
            // navBar1
            // 
            this.navBar1.AutoSize = true;
            this.navBar1.BindingListSource = this.bindingListSource1;
            this.navBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.navBar1.Location = new System.Drawing.Point(0, 0);
            this.navBar1.Name = "navBar1";
            this.navBar1.ShowViewsButton = true;
            this.navBar1.Size = new System.Drawing.Size(800, 25);
            this.navBar1.TabIndex = 0;
            // 
            // panelMemoryBar
            // 
            this.panelMemoryBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMemoryBar.Controls.Add(this.lblMemoryUsage);
            this.panelMemoryBar.Location = new System.Drawing.Point(12, 9);
            this.panelMemoryBar.Name = "panelMemoryBar";
            this.panelMemoryBar.Size = new System.Drawing.Size(654, 26);
            this.panelMemoryBar.TabIndex = 1;
            this.panelMemoryBar.SizeChanged += new System.EventHandler(this.panelMemoryBar_SizeChanged);
            this.panelMemoryBar.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMemoryBar_Paint);
            // 
            // PerfCounterGridForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer1);
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "PerfCounterGridForm";
            this.Text = "PerfCounterGridForm";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.boundDataGridViewEx1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingListSource1)).EndInit();
            this.panelMemoryBar.ResumeLayout(false);
            this.panelMemoryBar.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Common.DataBinding.Controls.NavBar navBar1;
        private Common.DataBinding.Controls.BindingListSource bindingListSource1;
        private Databinding.BoundDataGridViewEx boundDataGridViewEx1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Label lblMemoryUsage;
        private System.Windows.Forms.Panel panelMemoryBar;
    }
}