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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RunAlignmentForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbxDenseBestPath = new System.Windows.Forms.CheckBox();
            this.cbxSimilarityMatrix = new System.Windows.Forms.CheckBox();
            this.comboMsLevel = new System.Windows.Forms.ComboBox();
            this.lblMsLevel = new System.Windows.Forms.Label();
            this.comboSignatureLength = new System.Windows.Forms.ComboBox();
            this.lblSignatureLength = new System.Windows.Forms.Label();
            this.comboYAxis = new System.Windows.Forms.ComboBox();
            this.lblYAxis = new System.Windows.Forms.Label();
            this.comboXAxis = new System.Windows.Forms.ComboBox();
            this.lblXAxis = new System.Windows.Forms.Label();
            this.cbxSparseBestPath = new System.Windows.Forms.CheckBox();
            this.cbxKdeAlignment = new System.Windows.Forms.CheckBox();
            this.cbxHalfPrecision = new System.Windows.Forms.CheckBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.colSymbol = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.bindingNavigator1 = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorMoveFirstItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMovePreviousItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorPositionItem = new System.Windows.Forms.ToolStripTextBox();
            this.bindingNavigatorCountItem = new System.Windows.Forms.ToolStripLabel();
            this.bindingNavigatorSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorMoveNextItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveLastItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorDeleteItem = new System.Windows.Forms.ToolStripButton();
            this.panelGrid = new System.Windows.Forms.Panel();
            this.btnAdd = new System.Windows.Forms.Button();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurveTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurveDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.comboRegression = new System.Windows.Forms.ComboBox();
            this.lblRegression = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingNavigator1)).BeginInit();
            this.bindingNavigator1.SuspendLayout();
            this.panelGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblRegression);
            this.panel1.Controls.Add(this.comboRegression);
            this.panel1.Controls.Add(this.btnAdd);
            this.panel1.Controls.Add(this.panelGrid);
            this.panel1.Controls.Add(this.cbxHalfPrecision);
            this.panel1.Controls.Add(this.cbxKdeAlignment);
            this.panel1.Controls.Add(this.cbxSparseBestPath);
            this.panel1.Controls.Add(this.cbxDenseBestPath);
            this.panel1.Controls.Add(this.cbxSimilarityMatrix);
            this.panel1.Controls.Add(this.comboMsLevel);
            this.panel1.Controls.Add(this.lblMsLevel);
            this.panel1.Controls.Add(this.comboSignatureLength);
            this.panel1.Controls.Add(this.lblSignatureLength);
            this.panel1.Controls.Add(this.comboYAxis);
            this.panel1.Controls.Add(this.lblYAxis);
            this.panel1.Controls.Add(this.comboXAxis);
            this.panel1.Controls.Add(this.lblXAxis);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 225);
            this.panel1.TabIndex = 1;
            // 
            // cbxDenseBestPath
            // 
            this.cbxDenseBestPath.AutoSize = true;
            this.cbxDenseBestPath.Location = new System.Drawing.Point(149, 74);
            this.cbxDenseBestPath.Name = "cbxDenseBestPath";
            this.cbxDenseBestPath.Size = new System.Drawing.Size(106, 17);
            this.cbxDenseBestPath.TabIndex = 9;
            this.cbxDenseBestPath.Text = "Dense Best Path";
            this.cbxDenseBestPath.UseVisualStyleBackColor = true;
            this.cbxDenseBestPath.CheckedChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // cbxSimilarityMatrix
            // 
            this.cbxSimilarityMatrix.AutoSize = true;
            this.cbxSimilarityMatrix.Location = new System.Drawing.Point(266, 113);
            this.cbxSimilarityMatrix.Name = "cbxSimilarityMatrix";
            this.cbxSimilarityMatrix.Size = new System.Drawing.Size(97, 17);
            this.cbxSimilarityMatrix.TabIndex = 8;
            this.cbxSimilarityMatrix.Text = "Similarity Matrix";
            this.cbxSimilarityMatrix.UseVisualStyleBackColor = true;
            this.cbxSimilarityMatrix.CheckedChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // comboMsLevel
            // 
            this.comboMsLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMsLevel.FormattingEnabled = true;
            this.comboMsLevel.Location = new System.Drawing.Point(397, 6);
            this.comboMsLevel.Name = "comboMsLevel";
            this.comboMsLevel.Size = new System.Drawing.Size(121, 21);
            this.comboMsLevel.TabIndex = 7;
            this.comboMsLevel.SelectedIndexChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // lblMsLevel
            // 
            this.lblMsLevel.AutoSize = true;
            this.lblMsLevel.Location = new System.Drawing.Point(323, 9);
            this.lblMsLevel.Name = "lblMsLevel";
            this.lblMsLevel.Size = new System.Drawing.Size(55, 13);
            this.lblMsLevel.TabIndex = 6;
            this.lblMsLevel.Text = "MS Level:";
            // 
            // comboSignatureLength
            // 
            this.comboSignatureLength.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboSignatureLength.FormattingEnabled = true;
            this.comboSignatureLength.Location = new System.Drawing.Point(623, 6);
            this.comboSignatureLength.Name = "comboSignatureLength";
            this.comboSignatureLength.Size = new System.Drawing.Size(121, 21);
            this.comboSignatureLength.TabIndex = 5;
            this.comboSignatureLength.SelectedIndexChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // lblSignatureLength
            // 
            this.lblSignatureLength.AutoSize = true;
            this.lblSignatureLength.Location = new System.Drawing.Point(529, 9);
            this.lblSignatureLength.Name = "lblSignatureLength";
            this.lblSignatureLength.Size = new System.Drawing.Size(88, 13);
            this.lblSignatureLength.TabIndex = 4;
            this.lblSignatureLength.Text = "Signature Length";
            // 
            // comboYAxis
            // 
            this.comboYAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboYAxis.FormattingEnabled = true;
            this.comboYAxis.Location = new System.Drawing.Point(59, 39);
            this.comboYAxis.Name = "comboYAxis";
            this.comboYAxis.Size = new System.Drawing.Size(121, 21);
            this.comboYAxis.TabIndex = 3;
            this.comboYAxis.SelectedIndexChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // lblYAxis
            // 
            this.lblYAxis.AutoSize = true;
            this.lblYAxis.Location = new System.Drawing.Point(14, 44);
            this.lblYAxis.Name = "lblYAxis";
            this.lblYAxis.Size = new System.Drawing.Size(39, 13);
            this.lblYAxis.TabIndex = 2;
            this.lblYAxis.Text = "Y-Axis:";
            // 
            // comboXAxis
            // 
            this.comboXAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboXAxis.FormattingEnabled = true;
            this.comboXAxis.Location = new System.Drawing.Point(57, 9);
            this.comboXAxis.Name = "comboXAxis";
            this.comboXAxis.Size = new System.Drawing.Size(121, 21);
            this.comboXAxis.TabIndex = 1;
            this.comboXAxis.SelectedIndexChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // lblXAxis
            // 
            this.lblXAxis.AutoSize = true;
            this.lblXAxis.Location = new System.Drawing.Point(12, 9);
            this.lblXAxis.Name = "lblXAxis";
            this.lblXAxis.Size = new System.Drawing.Size(39, 13);
            this.lblXAxis.TabIndex = 0;
            this.lblXAxis.Text = "X-Axis:";
            // 
            // cbxSparseBestPath
            // 
            this.cbxSparseBestPath.AutoSize = true;
            this.cbxSparseBestPath.Checked = true;
            this.cbxSparseBestPath.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxSparseBestPath.Location = new System.Drawing.Point(17, 75);
            this.cbxSparseBestPath.Name = "cbxSparseBestPath";
            this.cbxSparseBestPath.Size = new System.Drawing.Size(106, 17);
            this.cbxSparseBestPath.TabIndex = 10;
            this.cbxSparseBestPath.Text = "Sparse best path";
            this.cbxSparseBestPath.UseVisualStyleBackColor = true;
            this.cbxSparseBestPath.CheckedChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // cbxKdeAlignment
            // 
            this.cbxKdeAlignment.AutoSize = true;
            this.cbxKdeAlignment.Location = new System.Drawing.Point(369, 113);
            this.cbxKdeAlignment.Name = "cbxKdeAlignment";
            this.cbxKdeAlignment.Size = new System.Drawing.Size(96, 17);
            this.cbxKdeAlignment.TabIndex = 11;
            this.cbxKdeAlignment.Text = "KDE alignment";
            this.cbxKdeAlignment.UseVisualStyleBackColor = true;
            this.cbxKdeAlignment.CheckedChanged += new System.EventHandler(this.OnValuesChanged);
            // 
            // cbxHalfPrecision
            // 
            this.cbxHalfPrecision.AutoSize = true;
            this.cbxHalfPrecision.Location = new System.Drawing.Point(227, 8);
            this.cbxHalfPrecision.Name = "cbxHalfPrecision";
            this.cbxHalfPrecision.Size = new System.Drawing.Size(90, 17);
            this.cbxHalfPrecision.TabIndex = 12;
            this.cbxHalfPrecision.Text = "Half precision";
            this.cbxHalfPrecision.UseVisualStyleBackColor = true;
            this.cbxHalfPrecision.CheckedChanged += new System.EventHandler(this.OnValuesChanged);
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
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.zedGraphControl1);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterDistance = 225;
            this.splitContainer1.TabIndex = 2;
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl1.Location = new System.Drawing.Point(0, 0);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(800, 221);
            this.zedGraphControl1.TabIndex = 0;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCurveTitle,
            this.colCurveDescription,
            this.colSymbol});
            this.dataGridView1.DataSource = this.bindingSource1;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 25);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(797, 106);
            this.dataGridView1.TabIndex = 13;
            // 
            // colSymbol
            // 
            this.colSymbol.HeaderText = "Symbol";
            this.colSymbol.Name = "colSymbol";
            this.colSymbol.ReadOnly = true;
            this.colSymbol.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colSymbol.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // bindingSource1
            // 
            this.bindingSource1.AllowNew = false;
            // 
            // bindingNavigator1
            // 
            this.bindingNavigator1.AddNewItem = this.bindingNavigatorAddNewItem;
            this.bindingNavigator1.BindingSource = this.bindingSource1;
            this.bindingNavigator1.CountItem = this.bindingNavigatorCountItem;
            this.bindingNavigator1.DeleteItem = this.bindingNavigatorDeleteItem;
            this.bindingNavigator1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bindingNavigatorMoveFirstItem,
            this.bindingNavigatorMovePreviousItem,
            this.bindingNavigatorSeparator,
            this.bindingNavigatorPositionItem,
            this.bindingNavigatorCountItem,
            this.bindingNavigatorSeparator1,
            this.bindingNavigatorMoveNextItem,
            this.bindingNavigatorMoveLastItem,
            this.bindingNavigatorSeparator2,
            this.bindingNavigatorAddNewItem,
            this.bindingNavigatorDeleteItem});
            this.bindingNavigator1.Location = new System.Drawing.Point(0, 0);
            this.bindingNavigator1.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.bindingNavigator1.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.bindingNavigator1.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.bindingNavigator1.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.bindingNavigator1.Name = "bindingNavigator1";
            this.bindingNavigator1.PositionItem = this.bindingNavigatorPositionItem;
            this.bindingNavigator1.Size = new System.Drawing.Size(797, 25);
            this.bindingNavigator1.TabIndex = 14;
            this.bindingNavigator1.Text = "bindingNavigator1";
            // 
            // bindingNavigatorMoveFirstItem
            // 
            this.bindingNavigatorMoveFirstItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveFirstItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveFirstItem.Image")));
            this.bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            this.bindingNavigatorMoveFirstItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveFirstItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveFirstItem.Text = "Move first";
            // 
            // bindingNavigatorMovePreviousItem
            // 
            this.bindingNavigatorMovePreviousItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMovePreviousItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMovePreviousItem.Image")));
            this.bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            this.bindingNavigatorMovePreviousItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMovePreviousItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMovePreviousItem.Text = "Move previous";
            // 
            // bindingNavigatorSeparator
            // 
            this.bindingNavigatorSeparator.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorPositionItem
            // 
            this.bindingNavigatorPositionItem.AccessibleName = "Position";
            this.bindingNavigatorPositionItem.AutoSize = false;
            this.bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(50, 23);
            this.bindingNavigatorPositionItem.Text = "0";
            this.bindingNavigatorPositionItem.ToolTipText = "Current position";
            // 
            // bindingNavigatorCountItem
            // 
            this.bindingNavigatorCountItem.Name = "bindingNavigatorCountItem";
            this.bindingNavigatorCountItem.Size = new System.Drawing.Size(35, 22);
            this.bindingNavigatorCountItem.Text = "of {0}";
            this.bindingNavigatorCountItem.ToolTipText = "Total number of items";
            // 
            // bindingNavigatorSeparator1
            // 
            this.bindingNavigatorSeparator1.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorMoveNextItem
            // 
            this.bindingNavigatorMoveNextItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveNextItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveNextItem.Image")));
            this.bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            this.bindingNavigatorMoveNextItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveNextItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveNextItem.Text = "Move next";
            // 
            // bindingNavigatorMoveLastItem
            // 
            this.bindingNavigatorMoveLastItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveLastItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveLastItem.Image")));
            this.bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            this.bindingNavigatorMoveLastItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveLastItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveLastItem.Text = "Move last";
            // 
            // bindingNavigatorSeparator2
            // 
            this.bindingNavigatorSeparator2.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorAddNewItem
            // 
            this.bindingNavigatorAddNewItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorAddNewItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorAddNewItem.Image")));
            this.bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            this.bindingNavigatorAddNewItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorAddNewItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorAddNewItem.Text = "Add new";
            // 
            // bindingNavigatorDeleteItem
            // 
            this.bindingNavigatorDeleteItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorDeleteItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorDeleteItem.Image")));
            this.bindingNavigatorDeleteItem.Name = "bindingNavigatorDeleteItem";
            this.bindingNavigatorDeleteItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorDeleteItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorDeleteItem.Text = "Delete";
            // 
            // panelGrid
            // 
            this.panelGrid.Controls.Add(this.dataGridView1);
            this.panelGrid.Controls.Add(this.bindingNavigator1);
            this.panelGrid.Location = new System.Drawing.Point(3, 94);
            this.panelGrid.Name = "panelGrid";
            this.panelGrid.Size = new System.Drawing.Size(797, 131);
            this.panelGrid.TabIndex = 15;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(469, 67);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 16;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Caption";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 251;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Description";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 252;
            // 
            // colCurveTitle
            // 
            this.colCurveTitle.HeaderText = "Caption";
            this.colCurveTitle.Name = "colCurveTitle";
            // 
            // colCurveDescription
            // 
            this.colCurveDescription.HeaderText = "Description";
            this.colCurveDescription.Name = "colCurveDescription";
            this.colCurveDescription.ReadOnly = true;
            // 
            // comboRegression
            // 
            this.comboRegression.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRegression.FormattingEnabled = true;
            this.comboRegression.Location = new System.Drawing.Point(326, 39);
            this.comboRegression.Name = "comboRegression";
            this.comboRegression.Size = new System.Drawing.Size(121, 21);
            this.comboRegression.TabIndex = 17;
            // 
            // lblRegression
            // 
            this.lblRegression.AutoSize = true;
            this.lblRegression.Location = new System.Drawing.Point(224, 42);
            this.lblRegression.Name = "lblRegression";
            this.lblRegression.Size = new System.Drawing.Size(90, 13);
            this.lblRegression.TabIndex = 18;
            this.lblRegression.Text = "Regression Type:";
            // 
            // RunAlignmentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer1);
            this.Name = "RunAlignmentForm";
            this.TabText = "RunAlignmentForm";
            this.Text = "RunAlignmentForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingNavigator1)).EndInit();
            this.bindingNavigator1.ResumeLayout(false);
            this.bindingNavigator1.PerformLayout();
            this.panelGrid.ResumeLayout(false);
            this.panelGrid.PerformLayout();
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
        private System.Windows.Forms.CheckBox cbxDenseBestPath;
        private System.Windows.Forms.CheckBox cbxSimilarityMatrix;
        private System.Windows.Forms.CheckBox cbxSparseBestPath;
        private System.Windows.Forms.CheckBox cbxKdeAlignment;
        private System.Windows.Forms.CheckBox cbxHalfPrecision;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurveTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurveDescription;
        private System.Windows.Forms.DataGridViewComboBoxColumn colSymbol;
        private System.Windows.Forms.BindingNavigator bindingNavigator1;
        private System.Windows.Forms.ToolStripButton bindingNavigatorAddNewItem;
        private System.Windows.Forms.ToolStripLabel bindingNavigatorCountItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorDeleteItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveFirstItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMovePreviousItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator;
        private System.Windows.Forms.ToolStripTextBox bindingNavigatorPositionItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator1;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveNextItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveLastItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator2;
        private System.Windows.Forms.BindingSource bindingSource1;
        private System.Windows.Forms.Panel panelGrid;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.Label lblRegression;
        private System.Windows.Forms.ComboBox comboRegression;
    }
}