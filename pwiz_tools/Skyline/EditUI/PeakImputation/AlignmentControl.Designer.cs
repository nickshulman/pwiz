namespace pwiz.Skyline.EditUI.PeakImputation
{
    partial class AlignmentControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxRetentionTimeAlignment = new System.Windows.Forms.GroupBox();
            this.comboValuesToAlign = new System.Windows.Forms.ComboBox();
            this.lblValuesToAlign = new System.Windows.Forms.Label();
            this.comboAlignToFile = new System.Windows.Forms.ComboBox();
            this.lblAlignTo = new System.Windows.Forms.Label();
            this.comboAlignmentType = new System.Windows.Forms.ComboBox();
            this.lblAlignType = new System.Windows.Forms.Label();
            this.lblAlignmentTarget = new System.Windows.Forms.Label();
            this.groupBoxRetentionTimeAlignment.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxRetentionTimeAlignment
            // 
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.comboValuesToAlign);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblValuesToAlign);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.comboAlignToFile);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblAlignmentTarget);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblAlignTo);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.comboAlignmentType);
            this.groupBoxRetentionTimeAlignment.Controls.Add(this.lblAlignType);
            this.groupBoxRetentionTimeAlignment.Location = new System.Drawing.Point(3, 3);
            this.groupBoxRetentionTimeAlignment.Name = "groupBoxRetentionTimeAlignment";
            this.groupBoxRetentionTimeAlignment.Size = new System.Drawing.Size(200, 154);
            this.groupBoxRetentionTimeAlignment.TabIndex = 19;
            this.groupBoxRetentionTimeAlignment.TabStop = false;
            this.groupBoxRetentionTimeAlignment.Text = "Retention time alignment";
            // 
            // comboValuesToAlign
            // 
            this.comboValuesToAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboValuesToAlign.FormattingEnabled = true;
            this.comboValuesToAlign.Location = new System.Drawing.Point(6, 40);
            this.comboValuesToAlign.Name = "comboValuesToAlign";
            this.comboValuesToAlign.Size = new System.Drawing.Size(188, 21);
            this.comboValuesToAlign.TabIndex = 5;
            this.comboValuesToAlign.SelectedIndexChanged += new System.EventHandler(this.SelectedValueChange);
            // 
            // lblValuesToAlign
            // 
            this.lblValuesToAlign.AutoSize = true;
            this.lblValuesToAlign.Location = new System.Drawing.Point(6, 22);
            this.lblValuesToAlign.Name = "lblValuesToAlign";
            this.lblValuesToAlign.Size = new System.Drawing.Size(79, 13);
            this.lblValuesToAlign.TabIndex = 4;
            this.lblValuesToAlign.Text = "Values to align:";
            // 
            // comboAlignToFile
            // 
            this.comboAlignToFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignToFile.FormattingEnabled = true;
            this.comboAlignToFile.Location = new System.Drawing.Point(6, 80);
            this.comboAlignToFile.Name = "comboAlignToFile";
            this.comboAlignToFile.Size = new System.Drawing.Size(188, 21);
            this.comboAlignToFile.TabIndex = 1;
            this.comboAlignToFile.SelectedIndexChanged += new System.EventHandler(this.SelectedValueChange);
            // 
            // lblAlignTo
            // 
            this.lblAlignTo.AutoSize = true;
            this.lblAlignTo.Location = new System.Drawing.Point(7, 64);
            this.lblAlignTo.Name = "lblAlignTo";
            this.lblAlignTo.Size = new System.Drawing.Size(61, 13);
            this.lblAlignTo.TabIndex = 0;
            this.lblAlignTo.Text = "Align to file:";
            // 
            // comboAlignmentType
            // 
            this.comboAlignmentType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAlignmentType.FormattingEnabled = true;
            this.comboAlignmentType.Location = new System.Drawing.Point(6, 123);
            this.comboAlignmentType.Name = "comboAlignmentType";
            this.comboAlignmentType.Size = new System.Drawing.Size(190, 21);
            this.comboAlignmentType.TabIndex = 3;
            this.comboAlignmentType.SelectedIndexChanged += new System.EventHandler(this.SelectedValueChange);
            // 
            // lblAlignType
            // 
            this.lblAlignType.AutoSize = true;
            this.lblAlignType.Location = new System.Drawing.Point(3, 107);
            this.lblAlignType.Name = "lblAlignType";
            this.lblAlignType.Size = new System.Drawing.Size(79, 13);
            this.lblAlignType.TabIndex = 2;
            this.lblAlignType.Text = "Alignment type:";
            // 
            // lblAlignmentTarget
            // 
            this.lblAlignmentTarget.AutoSize = true;
            this.lblAlignmentTarget.Location = new System.Drawing.Point(6, 64);
            this.lblAlignmentTarget.Name = "lblAlignmentTarget";
            this.lblAlignmentTarget.Size = new System.Drawing.Size(57, 13);
            this.lblAlignmentTarget.TabIndex = 0;
            this.lblAlignmentTarget.Text = "Target file:";
            // 
            // AlignmentControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxRetentionTimeAlignment);
            this.Name = "AlignmentControl";
            this.Size = new System.Drawing.Size(206, 157);
            this.groupBoxRetentionTimeAlignment.ResumeLayout(false);
            this.groupBoxRetentionTimeAlignment.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxRetentionTimeAlignment;
        private System.Windows.Forms.ComboBox comboValuesToAlign;
        private System.Windows.Forms.Label lblValuesToAlign;
        private System.Windows.Forms.ComboBox comboAlignToFile;
        private System.Windows.Forms.Label lblAlignmentTarget;
        private System.Windows.Forms.Label lblAlignTo;
        private System.Windows.Forms.ComboBox comboAlignmentType;
        private System.Windows.Forms.Label lblAlignType;
    }
}
