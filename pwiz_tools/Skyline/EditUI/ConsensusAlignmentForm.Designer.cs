namespace pwiz.Skyline.EditUI
{
    partial class ConsensusAlignmentForm
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
            this.checkedListBoxReplicates = new System.Windows.Forms.CheckedListBox();
            this.comboSource = new System.Windows.Forms.ComboBox();
            this.lblSource = new System.Windows.Forms.Label();
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
            this.panel1.Controls.Add(this.checkedListBoxReplicates);
            this.panel1.Controls.Add(this.comboSource);
            this.panel1.Controls.Add(this.lblSource);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 100);
            this.panel1.TabIndex = 1;
            // 
            // checkedListBoxReplicates
            // 
            this.checkedListBoxReplicates.FormattingEnabled = true;
            this.checkedListBoxReplicates.Location = new System.Drawing.Point(154, 3);
            this.checkedListBoxReplicates.Name = "checkedListBoxReplicates";
            this.checkedListBoxReplicates.Size = new System.Drawing.Size(440, 94);
            this.checkedListBoxReplicates.TabIndex = 2;
            // 
            // comboSource
            // 
            this.comboSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboSource.FormattingEnabled = true;
            this.comboSource.Location = new System.Drawing.Point(12, 25);
            this.comboSource.Name = "comboSource";
            this.comboSource.Size = new System.Drawing.Size(121, 21);
            this.comboSource.TabIndex = 1;
            this.comboSource.SelectedIndexChanged += new System.EventHandler(this.ControlValueChanged);
            // 
            // lblSource
            // 
            this.lblSource.AutoSize = true;
            this.lblSource.Location = new System.Drawing.Point(12, 9);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(76, 13);
            this.lblSource.TabIndex = 0;
            this.lblSource.Text = "Values to align";
            // 
            // ConsensusAlignmentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel1);
            this.Name = "ConsensusAlignmentForm";
            this.Text = "ConsensusAlignmentForm";
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.databoundGridControl, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboSource;
        private System.Windows.Forms.Label lblSource;
        private System.Windows.Forms.CheckedListBox checkedListBoxReplicates;
    }
}