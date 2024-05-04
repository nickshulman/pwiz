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
            this.comboAlignmentType = new System.Windows.Forms.ComboBox();
            this.lblAlignType = new System.Windows.Forms.Label();
            this.comboAlignToFile = new System.Windows.Forms.ComboBox();
            this.lblAlignTo = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // databoundGridControl
            // 
            this.databoundGridControl.Location = new System.Drawing.Point(0, 100);
            this.databoundGridControl.Size = new System.Drawing.Size(800, 350);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.comboAlignmentType);
            this.panel1.Controls.Add(this.lblAlignType);
            this.panel1.Controls.Add(this.comboAlignToFile);
            this.panel1.Controls.Add(this.lblAlignTo);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 100);
            this.panel1.TabIndex = 1;
            // 
            // comboAlignmentType
            // 
            this.comboAlignmentType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignmentType.FormattingEnabled = true;
            this.comboAlignmentType.Location = new System.Drawing.Point(239, 25);
            this.comboAlignmentType.Name = "comboAlignmentType";
            this.comboAlignmentType.Size = new System.Drawing.Size(150, 21);
            this.comboAlignmentType.TabIndex = 3;
            // 
            // lblAlignType
            // 
            this.lblAlignType.AutoSize = true;
            this.lblAlignType.Location = new System.Drawing.Point(236, 9);
            this.lblAlignType.Name = "lblAlignType";
            this.lblAlignType.Size = new System.Drawing.Size(79, 13);
            this.lblAlignType.TabIndex = 2;
            this.lblAlignType.Text = "Alignment type:";
            // 
            // comboAlignToFile
            // 
            this.comboAlignToFile.DisplayMember = "Key";
            this.comboAlignToFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignToFile.FormattingEnabled = true;
            this.comboAlignToFile.Location = new System.Drawing.Point(12, 25);
            this.comboAlignToFile.Name = "comboAlignToFile";
            this.comboAlignToFile.Size = new System.Drawing.Size(190, 21);
            this.comboAlignToFile.TabIndex = 1;
            this.comboAlignToFile.ValueMember = "Value";
            this.comboAlignToFile.SelectedIndexChanged += new System.EventHandler(this.comboAlignToFile_SelectedIndexChanged);
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
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboAlignmentType;
        private System.Windows.Forms.Label lblAlignType;
        private System.Windows.Forms.ComboBox comboAlignToFile;
        private System.Windows.Forms.Label lblAlignTo;
    }
}