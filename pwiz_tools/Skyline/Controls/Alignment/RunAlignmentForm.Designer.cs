namespace pwiz.Skyline.Controls.Alignment
{
    partial class RunAlignmentForm
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
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.panel1 = new System.Windows.Forms.Panel();
            this.comboMsLevel = new System.Windows.Forms.ComboBox();
            this.lblMsLevel = new System.Windows.Forms.Label();
            this.comboSignatureLength = new System.Windows.Forms.ComboBox();
            this.lblSignatureLength = new System.Windows.Forms.Label();
            this.comboYAxis = new System.Windows.Forms.ComboBox();
            this.lblYAxis = new System.Windows.Forms.Label();
            this.comboXAxis = new System.Windows.Forms.ComboBox();
            this.lblXAxis = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl1.Location = new System.Drawing.Point(0, 100);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(800, 350);
            this.zedGraphControl1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.comboMsLevel);
            this.panel1.Controls.Add(this.lblMsLevel);
            this.panel1.Controls.Add(this.comboSignatureLength);
            this.panel1.Controls.Add(this.lblSignatureLength);
            this.panel1.Controls.Add(this.comboYAxis);
            this.panel1.Controls.Add(this.lblYAxis);
            this.panel1.Controls.Add(this.comboXAxis);
            this.panel1.Controls.Add(this.lblXAxis);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 100);
            this.panel1.TabIndex = 1;
            // 
            // comboMsLevel
            // 
            this.comboMsLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMsLevel.FormattingEnabled = true;
            this.comboMsLevel.Location = new System.Drawing.Point(305, 35);
            this.comboMsLevel.Name = "comboMsLevel";
            this.comboMsLevel.Size = new System.Drawing.Size(121, 21);
            this.comboMsLevel.TabIndex = 7;
            this.comboMsLevel.SelectedIndexChanged += new System.EventHandler(this.combo_SelectedIndexChanged);
            // 
            // lblMsLevel
            // 
            this.lblMsLevel.AutoSize = true;
            this.lblMsLevel.Location = new System.Drawing.Point(231, 38);
            this.lblMsLevel.Name = "lblMsLevel";
            this.lblMsLevel.Size = new System.Drawing.Size(55, 13);
            this.lblMsLevel.TabIndex = 6;
            this.lblMsLevel.Text = "MS Level:";
            // 
            // comboSignatureLength
            // 
            this.comboSignatureLength.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboSignatureLength.FormattingEnabled = true;
            this.comboSignatureLength.Location = new System.Drawing.Point(305, 8);
            this.comboSignatureLength.Name = "comboSignatureLength";
            this.comboSignatureLength.Size = new System.Drawing.Size(121, 21);
            this.comboSignatureLength.TabIndex = 5;
            this.comboSignatureLength.SelectedIndexChanged += new System.EventHandler(this.combo_SelectedIndexChanged);
            // 
            // lblSignatureLength
            // 
            this.lblSignatureLength.AutoSize = true;
            this.lblSignatureLength.Location = new System.Drawing.Point(211, 11);
            this.lblSignatureLength.Name = "lblSignatureLength";
            this.lblSignatureLength.Size = new System.Drawing.Size(88, 13);
            this.lblSignatureLength.TabIndex = 4;
            this.lblSignatureLength.Text = "Signature Length";
            // 
            // comboYAxis
            // 
            this.comboYAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboYAxis.FormattingEnabled = true;
            this.comboYAxis.Location = new System.Drawing.Point(57, 35);
            this.comboYAxis.Name = "comboYAxis";
            this.comboYAxis.Size = new System.Drawing.Size(121, 21);
            this.comboYAxis.TabIndex = 3;
            this.comboYAxis.SelectedIndexChanged += new System.EventHandler(this.combo_SelectedIndexChanged);
            // 
            // lblYAxis
            // 
            this.lblYAxis.AutoSize = true;
            this.lblYAxis.Location = new System.Drawing.Point(12, 34);
            this.lblYAxis.Name = "lblYAxis";
            this.lblYAxis.Size = new System.Drawing.Size(39, 13);
            this.lblYAxis.TabIndex = 2;
            this.lblYAxis.Text = "Y-Axis:";
            // 
            // comboXAxis
            // 
            this.comboXAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboXAxis.FormattingEnabled = true;
            this.comboXAxis.Location = new System.Drawing.Point(57, 8);
            this.comboXAxis.Name = "comboXAxis";
            this.comboXAxis.Size = new System.Drawing.Size(121, 21);
            this.comboXAxis.TabIndex = 1;
            this.comboXAxis.SelectedIndexChanged += new System.EventHandler(this.combo_SelectedIndexChanged);
            // 
            // lblXAxis
            // 
            this.lblXAxis.AutoSize = true;
            this.lblXAxis.Location = new System.Drawing.Point(12, 11);
            this.lblXAxis.Name = "lblXAxis";
            this.lblXAxis.Size = new System.Drawing.Size(39, 13);
            this.lblXAxis.TabIndex = 0;
            this.lblXAxis.Text = "X-Axis:";
            // 
            // RunAlignmentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.zedGraphControl1);
            this.Controls.Add(this.panel1);
            this.Name = "RunAlignmentForm";
            this.TabText = "RunAlignmentForm";
            this.Text = "RunAlignmentForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboYAxis;
        private System.Windows.Forms.Label lblYAxis;
        private System.Windows.Forms.ComboBox comboXAxis;
        private System.Windows.Forms.Label lblXAxis;
        private System.Windows.Forms.Label lblSignatureLength;
        private System.Windows.Forms.ComboBox comboSignatureLength;
        private System.Windows.Forms.ComboBox comboMsLevel;
        private System.Windows.Forms.Label lblMsLevel;
    }
}