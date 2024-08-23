namespace TableEditor
{
    partial class TableEditor3D
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TableEditor3D));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToolStrip_CopyWithAxis = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_Copy = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStrip_PasteSpecial = new System.Windows.Forms.ToolStripMenuItem();
            this.multiplyByToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.multiplyByHalfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.divideByToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.divideByHalfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.subtractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_PasteWithXYAxis = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_Paste = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStrip_AddtnlPasteFcns = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_PasteYAxis = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_PasteXAxis = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_PasteXAxisPCMTEC = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStrip_PasteTable = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_PasteTableWithRowAxis = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip_PasteTableWithColAxis = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStrip_ClearTable = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btn_All_Interpolate = new System.Windows.Forms.Button();
            this.btn_Undo = new System.Windows.Forms.Button();
            this.btn_ClipMin = new System.Windows.Forms.Button();
            this.btn_ClipMax = new System.Windows.Forms.Button();
            this.btn_FillMissingDataGaps = new System.Windows.Forms.Button();
            this.btn_MissingNeighbourFill = new System.Windows.Forms.Button();
            this.btn_DecDp = new System.Windows.Forms.Button();
            this.btn_IncDp = new System.Windows.Forms.Button();
            this.btn_H_Interpolate = new System.Windows.Forms.Button();
            this.btn_V_Interpolate = new System.Windows.Forms.Button();
            this.btn_SetSelectedCellsValue = new System.Windows.Forms.Button();
            this.btn_ClearAllSelections = new System.Windows.Forms.Button();
            this.btn_Graph3D_ResetView = new System.Windows.Forms.Button();
            this.btn_Graph3D_Instructions = new System.Windows.Forms.Button();
            this.btn_Plot3d_TransposeXY = new System.Windows.Forms.Button();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.btn_Options = new System.Windows.Forms.Button();
            this.btn_AverageTool = new System.Windows.Forms.Button();
            this.btn_All_Smooth = new System.Windows.Forms.Button();
            this.btn_H_Smooth = new System.Windows.Forms.Button();
            this.btn_V_Smooth = new System.Windows.Forms.Button();
            this.btn_Redo = new System.Windows.Forms.Button();
            this.btn_RotateGraphDockedLocation = new System.Windows.Forms.Button();
            this.btn_TableEditMode = new System.Windows.Forms.Button();
            this.btn_Graph3d_PointMoveMode = new System.Windows.Forms.Button();
            this.btn_Graph3d_PointSelectMode = new System.Windows.Forms.Button();
            this.Main_Timer = new System.Windows.Forms.Timer(this.components);
            this.toolBar = new System.Windows.Forms.TableLayoutPanel();
            this.btn_LoadSample4 = new System.Windows.Forms.Button();
            this.btn_LoadSample3 = new System.Windows.Forms.Button();
            this.btn_LoadSample2 = new System.Windows.Forms.Button();
            this.btn_LoadSample1 = new System.Windows.Forms.Button();
            this.btn_Multiply = new System.Windows.Forms.Button();
            this.textBox_Adjust = new System.Windows.Forms.TextBox();
            this.btn_Divide = new System.Windows.Forms.Button();
            this.btn_Add = new System.Windows.Forms.Button();
            this.btn_Subtract = new System.Windows.Forms.Button();
            this.btn_Paste = new System.Windows.Forms.Button();
            this.btn_Copy = new System.Windows.Forms.Button();
            this.DgvTable = new System.Windows.Forms.DataGridView();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.hScrollBar = new System.Windows.Forms.HScrollBar();
            this.blankingPanel = new System.Windows.Forms.Panel();
            this.colHeader = new System.Windows.Forms.DataGridView();
            this.rowHeader = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.Graph3d_UserControl = new Plot3D.Editor3D();
            this.contextMenuStrip.SuspendLayout();
            this.toolBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DgvTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.colHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rowHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStrip_CopyWithAxis,
            this.ToolStrip_Copy,
            this.toolStripSeparator1,
            this.ToolStrip_PasteSpecial,
            this.ToolStrip_PasteWithXYAxis,
            this.ToolStrip_Paste,
            this.toolStripSeparator3,
            this.ToolStrip_AddtnlPasteFcns});
            this.contextMenuStrip.Name = "contextMenuStrip1";
            this.contextMenuStrip.Size = new System.Drawing.Size(185, 148);
            this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
            // 
            // ToolStrip_CopyWithAxis
            // 
            this.ToolStrip_CopyWithAxis.Name = "ToolStrip_CopyWithAxis";
            this.ToolStrip_CopyWithAxis.Size = new System.Drawing.Size(184, 22);
            this.ToolStrip_CopyWithAxis.Text = "Copy With Axis";
            this.ToolStrip_CopyWithAxis.Click += new System.EventHandler(this.CopyWithAxis_Click);
            // 
            // ToolStrip_Copy
            // 
            this.ToolStrip_Copy.Name = "ToolStrip_Copy";
            this.ToolStrip_Copy.Size = new System.Drawing.Size(184, 22);
            this.ToolStrip_Copy.Text = "Copy";
            this.ToolStrip_Copy.Click += new System.EventHandler(this.CopyWithNoAxis_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(181, 6);
            // 
            // ToolStrip_PasteSpecial
            // 
            this.ToolStrip_PasteSpecial.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.multiplyByToolStripMenuItem,
            this.multiplyByHalfToolStripMenuItem,
            this.divideByToolStripMenuItem,
            this.divideByHalfToolStripMenuItem,
            this.addToolStripMenuItem,
            this.subtractToolStripMenuItem});
            this.ToolStrip_PasteSpecial.Name = "ToolStrip_PasteSpecial";
            this.ToolStrip_PasteSpecial.Size = new System.Drawing.Size(184, 22);
            this.ToolStrip_PasteSpecial.Text = "Paste Special";
            // 
            // multiplyByToolStripMenuItem
            // 
            this.multiplyByToolStripMenuItem.Name = "multiplyByToolStripMenuItem";
            this.multiplyByToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.multiplyByToolStripMenuItem.Text = "Multiply by %";
            this.multiplyByToolStripMenuItem.Click += new System.EventHandler(this.Paste_MultiplyByPercent);
            // 
            // multiplyByHalfToolStripMenuItem
            // 
            this.multiplyByHalfToolStripMenuItem.Name = "multiplyByHalfToolStripMenuItem";
            this.multiplyByHalfToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.multiplyByHalfToolStripMenuItem.Text = "Multiply by % Half";
            this.multiplyByHalfToolStripMenuItem.Click += new System.EventHandler(this.Paste_MultiplyByPercentHalf);
            // 
            // divideByToolStripMenuItem
            // 
            this.divideByToolStripMenuItem.Name = "divideByToolStripMenuItem";
            this.divideByToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.divideByToolStripMenuItem.Text = "Divide by %";
            this.divideByToolStripMenuItem.Click += new System.EventHandler(this.Paste_DivideByPercent);
            // 
            // divideByHalfToolStripMenuItem
            // 
            this.divideByHalfToolStripMenuItem.Name = "divideByHalfToolStripMenuItem";
            this.divideByHalfToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.divideByHalfToolStripMenuItem.Text = "Divide by % Half";
            this.divideByHalfToolStripMenuItem.Click += new System.EventHandler(this.Paste_DivideByPercentHalf);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.Paste_Add);
            // 
            // subtractToolStripMenuItem
            // 
            this.subtractToolStripMenuItem.Name = "subtractToolStripMenuItem";
            this.subtractToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.subtractToolStripMenuItem.Text = "Subtract";
            this.subtractToolStripMenuItem.Click += new System.EventHandler(this.Paste_Subtract);
            // 
            // ToolStrip_PasteWithXYAxis
            // 
            this.ToolStrip_PasteWithXYAxis.Name = "ToolStrip_PasteWithXYAxis";
            this.ToolStrip_PasteWithXYAxis.Size = new System.Drawing.Size(184, 22);
            this.ToolStrip_PasteWithXYAxis.Text = "Paste With Axis";
            this.ToolStrip_PasteWithXYAxis.Click += new System.EventHandler(this.PasteTableWithXYAxis_Click);
            // 
            // ToolStrip_Paste
            // 
            this.ToolStrip_Paste.Name = "ToolStrip_Paste";
            this.ToolStrip_Paste.Size = new System.Drawing.Size(184, 22);
            this.ToolStrip_Paste.Text = "Paste";
            this.ToolStrip_Paste.Click += new System.EventHandler(this.Paste_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(181, 6);
            // 
            // ToolStrip_AddtnlPasteFcns
            // 
            this.ToolStrip_AddtnlPasteFcns.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStrip_PasteYAxis,
            this.ToolStrip_PasteXAxis,
            this.ToolStrip_PasteXAxisPCMTEC,
            this.toolStripSeparator5,
            this.ToolStrip_PasteTable,
            this.ToolStrip_PasteTableWithRowAxis,
            this.ToolStrip_PasteTableWithColAxis,
            this.toolStripSeparator2,
            this.ToolStrip_ClearTable});
            this.ToolStrip_AddtnlPasteFcns.Name = "ToolStrip_AddtnlPasteFcns";
            this.ToolStrip_AddtnlPasteFcns.Size = new System.Drawing.Size(184, 22);
            this.ToolStrip_AddtnlPasteFcns.Text = "Additional Functions";
            // 
            // ToolStrip_PasteYAxis
            // 
            this.ToolStrip_PasteYAxis.Name = "ToolStrip_PasteYAxis";
            this.ToolStrip_PasteYAxis.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_PasteYAxis.Text = "Paste Row Axis";
            this.ToolStrip_PasteYAxis.Click += new System.EventHandler(this.PasteYAxis_Click);
            // 
            // ToolStrip_PasteXAxis
            // 
            this.ToolStrip_PasteXAxis.Name = "ToolStrip_PasteXAxis";
            this.ToolStrip_PasteXAxis.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_PasteXAxis.Text = "Paste Column Axis";
            this.ToolStrip_PasteXAxis.Click += new System.EventHandler(this.PasteXAxis_Click);
            // 
            // ToolStrip_PasteXAxisPCMTEC
            // 
            this.ToolStrip_PasteXAxisPCMTEC.Name = "ToolStrip_PasteXAxisPCMTEC";
            this.ToolStrip_PasteXAxisPCMTEC.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_PasteXAxisPCMTEC.Text = "Paste Column Axis (PCMTEC)";
            this.ToolStrip_PasteXAxisPCMTEC.Click += new System.EventHandler(this.PasteXAxis_PCMTEC_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(227, 6);
            // 
            // ToolStrip_PasteTable
            // 
            this.ToolStrip_PasteTable.Name = "ToolStrip_PasteTable";
            this.ToolStrip_PasteTable.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_PasteTable.Text = "Paste Table with No Axis";
            this.ToolStrip_PasteTable.Click += new System.EventHandler(this.PasteTableWithNoAxis_Click);
            // 
            // ToolStrip_PasteTableWithRowAxis
            // 
            this.ToolStrip_PasteTableWithRowAxis.Name = "ToolStrip_PasteTableWithRowAxis";
            this.ToolStrip_PasteTableWithRowAxis.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_PasteTableWithRowAxis.Text = "Paste Table with Row Axis";
            this.ToolStrip_PasteTableWithRowAxis.Click += new System.EventHandler(this.PasteTableWithYAxis_Click);
            // 
            // ToolStrip_PasteTableWithColAxis
            // 
            this.ToolStrip_PasteTableWithColAxis.Name = "ToolStrip_PasteTableWithColAxis";
            this.ToolStrip_PasteTableWithColAxis.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_PasteTableWithColAxis.Text = "Paste Table with Column Axis";
            this.ToolStrip_PasteTableWithColAxis.Click += new System.EventHandler(this.PasteTableWithXAxis_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(227, 6);
            // 
            // ToolStrip_ClearTable
            // 
            this.ToolStrip_ClearTable.Name = "ToolStrip_ClearTable";
            this.ToolStrip_ClearTable.Size = new System.Drawing.Size(230, 22);
            this.ToolStrip_ClearTable.Text = "Clear Table";
            this.ToolStrip_ClearTable.Click += new System.EventHandler(this.ClearTable_Click);
            // 
            // toolTip
            // 
            this.toolTip.IsBalloon = true;
            // 
            // btn_All_Interpolate
            // 
            this.btn_All_Interpolate.BackColor = System.Drawing.SystemColors.Control;
            this.btn_All_Interpolate.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_All_Interpolate.BackgroundImage")));
            this.btn_All_Interpolate.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_All_Interpolate, 2);
            this.btn_All_Interpolate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_All_Interpolate.Location = new System.Drawing.Point(585, 1);
            this.btn_All_Interpolate.Margin = new System.Windows.Forms.Padding(1);
            this.btn_All_Interpolate.Name = "btn_All_Interpolate";
            this.btn_All_Interpolate.Size = new System.Drawing.Size(34, 36);
            this.btn_All_Interpolate.TabIndex = 27;
            this.toolTip.SetToolTip(this.btn_All_Interpolate, "Vertical & horizontal interpolate selected cells");
            this.btn_All_Interpolate.UseVisualStyleBackColor = false;
            this.btn_All_Interpolate.Click += new System.EventHandler(this.btn_All_Interpolate_Click);
            // 
            // btn_Undo
            // 
            this.toolBar.SetColumnSpan(this.btn_Undo, 2);
            this.btn_Undo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Undo.Enabled = false;
            this.btn_Undo.Image = ((System.Drawing.Image)(resources.GetObject("btn_Undo.Image")));
            this.btn_Undo.Location = new System.Drawing.Point(225, 1);
            this.btn_Undo.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Undo.Name = "btn_Undo";
            this.btn_Undo.Size = new System.Drawing.Size(34, 36);
            this.btn_Undo.TabIndex = 24;
            this.toolTip.SetToolTip(this.btn_Undo, "Undo");
            this.btn_Undo.UseVisualStyleBackColor = true;
            this.btn_Undo.Click += new System.EventHandler(this.btn_Undo_Click);
            // 
            // btn_ClipMin
            // 
            this.btn_ClipMin.BackColor = System.Drawing.SystemColors.Control;
            this.btn_ClipMin.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_ClipMin.BackgroundImage")));
            this.btn_ClipMin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_ClipMin, 2);
            this.btn_ClipMin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_ClipMin.Location = new System.Drawing.Point(297, 39);
            this.btn_ClipMin.Margin = new System.Windows.Forms.Padding(1);
            this.btn_ClipMin.Name = "btn_ClipMin";
            this.btn_ClipMin.Size = new System.Drawing.Size(34, 36);
            this.btn_ClipMin.TabIndex = 23;
            this.toolTip.SetToolTip(this.btn_ClipMin, "Selected cells that are less than the \r\nset value are raised to the set value\r\n");
            this.btn_ClipMin.UseVisualStyleBackColor = false;
            this.btn_ClipMin.Click += new System.EventHandler(this.btn_ClipMin_Click);
            // 
            // btn_ClipMax
            // 
            this.btn_ClipMax.BackColor = System.Drawing.SystemColors.Control;
            this.btn_ClipMax.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_ClipMax.BackgroundImage")));
            this.btn_ClipMax.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_ClipMax, 2);
            this.btn_ClipMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_ClipMax.Location = new System.Drawing.Point(261, 39);
            this.btn_ClipMax.Margin = new System.Windows.Forms.Padding(1);
            this.btn_ClipMax.Name = "btn_ClipMax";
            this.btn_ClipMax.Size = new System.Drawing.Size(34, 36);
            this.btn_ClipMax.TabIndex = 22;
            this.toolTip.SetToolTip(this.btn_ClipMax, "Selected cells that are greater than the \r\nset value are lowered to the set value" +
        "");
            this.btn_ClipMax.UseVisualStyleBackColor = false;
            this.btn_ClipMax.Click += new System.EventHandler(this.btn_ClipMax_Click);
            // 
            // btn_FillMissingDataGaps
            // 
            this.btn_FillMissingDataGaps.BackColor = System.Drawing.SystemColors.Control;
            this.btn_FillMissingDataGaps.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_FillMissingDataGaps.BackgroundImage")));
            this.btn_FillMissingDataGaps.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_FillMissingDataGaps, 2);
            this.btn_FillMissingDataGaps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_FillMissingDataGaps.Location = new System.Drawing.Point(387, 1);
            this.btn_FillMissingDataGaps.Margin = new System.Windows.Forms.Padding(1);
            this.btn_FillMissingDataGaps.Name = "btn_FillMissingDataGaps";
            this.btn_FillMissingDataGaps.Size = new System.Drawing.Size(34, 36);
            this.btn_FillMissingDataGaps.TabIndex = 13;
            this.toolTip.SetToolTip(this.btn_FillMissingDataGaps, "Data gap fill (auto interpolate)");
            this.btn_FillMissingDataGaps.UseVisualStyleBackColor = false;
            this.btn_FillMissingDataGaps.Click += new System.EventHandler(this.btn_FillMissingDataGaps_Click);
            // 
            // btn_MissingNeighbourFill
            // 
            this.btn_MissingNeighbourFill.BackColor = System.Drawing.SystemColors.Control;
            this.btn_MissingNeighbourFill.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_MissingNeighbourFill.BackgroundImage")));
            this.btn_MissingNeighbourFill.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_MissingNeighbourFill, 2);
            this.btn_MissingNeighbourFill.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_MissingNeighbourFill.Location = new System.Drawing.Point(423, 1);
            this.btn_MissingNeighbourFill.Margin = new System.Windows.Forms.Padding(1);
            this.btn_MissingNeighbourFill.Name = "btn_MissingNeighbourFill";
            this.btn_MissingNeighbourFill.Size = new System.Drawing.Size(34, 36);
            this.btn_MissingNeighbourFill.TabIndex = 14;
            this.toolTip.SetToolTip(this.btn_MissingNeighbourFill, "Missing neighbour fill");
            this.btn_MissingNeighbourFill.UseVisualStyleBackColor = false;
            this.btn_MissingNeighbourFill.Click += new System.EventHandler(this.btn_MissingNeighbourFill_Click);
            // 
            // btn_DecDp
            // 
            this.btn_DecDp.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_DecDp.BackgroundImage")));
            this.btn_DecDp.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_DecDp, 2);
            this.btn_DecDp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_DecDp.Location = new System.Drawing.Point(45, 1);
            this.btn_DecDp.Margin = new System.Windows.Forms.Padding(1);
            this.btn_DecDp.Name = "btn_DecDp";
            this.btn_DecDp.Size = new System.Drawing.Size(34, 36);
            this.btn_DecDp.TabIndex = 5;
            this.toolTip.SetToolTip(this.btn_DecDp, "Decrease decimal places");
            this.btn_DecDp.UseVisualStyleBackColor = true;
            this.btn_DecDp.Click += new System.EventHandler(this.btn_DecDp_Click);
            // 
            // btn_IncDp
            // 
            this.btn_IncDp.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_IncDp.BackgroundImage")));
            this.btn_IncDp.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_IncDp, 2);
            this.btn_IncDp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_IncDp.Location = new System.Drawing.Point(9, 1);
            this.btn_IncDp.Margin = new System.Windows.Forms.Padding(1);
            this.btn_IncDp.Name = "btn_IncDp";
            this.btn_IncDp.Size = new System.Drawing.Size(34, 36);
            this.btn_IncDp.TabIndex = 7;
            this.toolTip.SetToolTip(this.btn_IncDp, "Increase decimal places");
            this.btn_IncDp.UseVisualStyleBackColor = true;
            this.btn_IncDp.Click += new System.EventHandler(this.btn_IncDp_Click);
            // 
            // btn_H_Interpolate
            // 
            this.btn_H_Interpolate.BackColor = System.Drawing.SystemColors.Control;
            this.btn_H_Interpolate.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_H_Interpolate.BackgroundImage")));
            this.btn_H_Interpolate.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_H_Interpolate, 2);
            this.btn_H_Interpolate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_H_Interpolate.Location = new System.Drawing.Point(621, 1);
            this.btn_H_Interpolate.Margin = new System.Windows.Forms.Padding(1);
            this.btn_H_Interpolate.Name = "btn_H_Interpolate";
            this.btn_H_Interpolate.Size = new System.Drawing.Size(34, 36);
            this.btn_H_Interpolate.TabIndex = 15;
            this.toolTip.SetToolTip(this.btn_H_Interpolate, "Horizontal interpolate selected cells");
            this.btn_H_Interpolate.UseVisualStyleBackColor = false;
            this.btn_H_Interpolate.Click += new System.EventHandler(this.btn_H_Interpolate_Click);
            // 
            // btn_V_Interpolate
            // 
            this.btn_V_Interpolate.BackColor = System.Drawing.SystemColors.Control;
            this.btn_V_Interpolate.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_V_Interpolate.BackgroundImage")));
            this.btn_V_Interpolate.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_V_Interpolate, 2);
            this.btn_V_Interpolate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_V_Interpolate.Location = new System.Drawing.Point(657, 1);
            this.btn_V_Interpolate.Margin = new System.Windows.Forms.Padding(1);
            this.btn_V_Interpolate.Name = "btn_V_Interpolate";
            this.btn_V_Interpolate.Size = new System.Drawing.Size(34, 36);
            this.btn_V_Interpolate.TabIndex = 16;
            this.toolTip.SetToolTip(this.btn_V_Interpolate, "Vertical interpolate selected cells");
            this.btn_V_Interpolate.UseVisualStyleBackColor = false;
            this.btn_V_Interpolate.Click += new System.EventHandler(this.btn_V_Interpolate_Click);
            // 
            // btn_SetSelectedCellsValue
            // 
            this.btn_SetSelectedCellsValue.BackColor = System.Drawing.SystemColors.Control;
            this.btn_SetSelectedCellsValue.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_SetSelectedCellsValue.BackgroundImage")));
            this.btn_SetSelectedCellsValue.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_SetSelectedCellsValue, 2);
            this.btn_SetSelectedCellsValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_SetSelectedCellsValue.Location = new System.Drawing.Point(81, 39);
            this.btn_SetSelectedCellsValue.Margin = new System.Windows.Forms.Padding(1);
            this.btn_SetSelectedCellsValue.Name = "btn_SetSelectedCellsValue";
            this.btn_SetSelectedCellsValue.Size = new System.Drawing.Size(34, 36);
            this.btn_SetSelectedCellsValue.TabIndex = 20;
            this.toolTip.SetToolTip(this.btn_SetSelectedCellsValue, "Set value to selected cells");
            this.btn_SetSelectedCellsValue.UseVisualStyleBackColor = false;
            this.btn_SetSelectedCellsValue.Click += new System.EventHandler(this.btn_SetSelectedCellsValue_Click);
            // 
            // btn_ClearAllSelections
            // 
            this.btn_ClearAllSelections.BackColor = System.Drawing.SystemColors.Control;
            this.btn_ClearAllSelections.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_ClearAllSelections.BackgroundImage")));
            this.btn_ClearAllSelections.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_ClearAllSelections, 2);
            this.btn_ClearAllSelections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_ClearAllSelections.Location = new System.Drawing.Point(351, 39);
            this.btn_ClearAllSelections.Margin = new System.Windows.Forms.Padding(1);
            this.btn_ClearAllSelections.Name = "btn_ClearAllSelections";
            this.btn_ClearAllSelections.Size = new System.Drawing.Size(34, 36);
            this.btn_ClearAllSelections.TabIndex = 30;
            this.toolTip.SetToolTip(this.btn_ClearAllSelections, "Clear all selections");
            this.btn_ClearAllSelections.UseVisualStyleBackColor = true;
            this.btn_ClearAllSelections.Click += new System.EventHandler(this.btn_ClearAllSelections_Click);
            // 
            // btn_Graph3D_ResetView
            // 
            this.btn_Graph3D_ResetView.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Graph3D_ResetView.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_Graph3D_ResetView, 2);
            this.btn_Graph3D_ResetView.Image = ((System.Drawing.Image)(resources.GetObject("btn_Graph3D_ResetView.Image")));
            this.btn_Graph3D_ResetView.Location = new System.Drawing.Point(585, 39);
            this.btn_Graph3D_ResetView.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Graph3D_ResetView.Name = "btn_Graph3D_ResetView";
            this.btn_Graph3D_ResetView.Size = new System.Drawing.Size(34, 36);
            this.btn_Graph3D_ResetView.TabIndex = 31;
            this.toolTip.SetToolTip(this.btn_Graph3D_ResetView, "Reorientates the 3D chart view to the default coordinates");
            this.btn_Graph3D_ResetView.UseVisualStyleBackColor = true;
            this.btn_Graph3D_ResetView.Click += new System.EventHandler(this.btn_Graph3D_ResetView_Click);
            // 
            // btn_Graph3D_Instructions
            // 
            this.btn_Graph3D_Instructions.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Graph3D_Instructions.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_Graph3D_Instructions.BackgroundImage")));
            this.btn_Graph3D_Instructions.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_Graph3D_Instructions, 2);
            this.btn_Graph3D_Instructions.Location = new System.Drawing.Point(621, 39);
            this.btn_Graph3D_Instructions.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Graph3D_Instructions.Name = "btn_Graph3D_Instructions";
            this.btn_Graph3D_Instructions.Size = new System.Drawing.Size(34, 36);
            this.btn_Graph3D_Instructions.TabIndex = 33;
            this.toolTip.SetToolTip(this.btn_Graph3D_Instructions, "Instructions");
            this.btn_Graph3D_Instructions.UseVisualStyleBackColor = true;
            this.btn_Graph3D_Instructions.Click += new System.EventHandler(this.btn_Graph3D_Instructions_Click);
            // 
            // btn_Plot3d_TransposeXY
            // 
            this.btn_Plot3d_TransposeXY.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Plot3d_TransposeXY.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_Plot3d_TransposeXY, 2);
            this.btn_Plot3d_TransposeXY.ImageIndex = 4;
            this.btn_Plot3d_TransposeXY.ImageList = this.imageList1;
            this.btn_Plot3d_TransposeXY.Location = new System.Drawing.Point(549, 39);
            this.btn_Plot3d_TransposeXY.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Plot3d_TransposeXY.Name = "btn_Plot3d_TransposeXY";
            this.btn_Plot3d_TransposeXY.Size = new System.Drawing.Size(34, 36);
            this.btn_Plot3d_TransposeXY.TabIndex = 40;
            this.toolTip.SetToolTip(this.btn_Plot3d_TransposeXY, "Transpose Graph Axis");
            this.btn_Plot3d_TransposeXY.UseVisualStyleBackColor = false;
            this.btn_Plot3d_TransposeXY.Click += new System.EventHandler(this.btn_Plot3d_Transpose_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Mirror_Right.png");
            this.imageList1.Images.SetKeyName(1, "Mirror_Right_Selected.png");
            this.imageList1.Images.SetKeyName(2, "Mirror_Left.png");
            this.imageList1.Images.SetKeyName(3, "Mirror_Left_Selected.png");
            this.imageList1.Images.SetKeyName(4, "Transpose.png");
            this.imageList1.Images.SetKeyName(5, "Transpose Selected.png");
            this.imageList1.Images.SetKeyName(6, "Target.png");
            this.imageList1.Images.SetKeyName(7, "Target Selected.png");
            this.imageList1.Images.SetKeyName(8, "Point Move.png");
            this.imageList1.Images.SetKeyName(9, "Point Selected.png");
            // 
            // btn_Options
            // 
            this.btn_Options.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Options.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_Options.BackgroundImage")));
            this.btn_Options.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_Options, 2);
            this.btn_Options.Location = new System.Drawing.Point(657, 39);
            this.btn_Options.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Options.Name = "btn_Options";
            this.btn_Options.Size = new System.Drawing.Size(34, 36);
            this.btn_Options.TabIndex = 41;
            this.toolTip.SetToolTip(this.btn_Options, "Settings");
            this.btn_Options.UseVisualStyleBackColor = false;
            this.btn_Options.Click += new System.EventHandler(this.btn_Options_Click);
            // 
            // btn_AverageTool
            // 
            this.btn_AverageTool.BackColor = System.Drawing.SystemColors.Control;
            this.btn_AverageTool.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolBar.SetColumnSpan(this.btn_AverageTool, 2);
            this.btn_AverageTool.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_AverageTool.Image = ((System.Drawing.Image)(resources.GetObject("btn_AverageTool.Image")));
            this.btn_AverageTool.Location = new System.Drawing.Point(350, 0);
            this.btn_AverageTool.Margin = new System.Windows.Forms.Padding(0);
            this.btn_AverageTool.Name = "btn_AverageTool";
            this.btn_AverageTool.Size = new System.Drawing.Size(36, 38);
            this.btn_AverageTool.TabIndex = 42;
            this.toolTip.SetToolTip(this.btn_AverageTool, "Average Tool");
            this.btn_AverageTool.UseVisualStyleBackColor = false;
            this.btn_AverageTool.Click += new System.EventHandler(this.btn_AverageTool_Click);
            // 
            // btn_All_Smooth
            // 
            this.btn_All_Smooth.BackColor = System.Drawing.SystemColors.Control;
            this.btn_All_Smooth.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_All_Smooth.BackgroundImage")));
            this.btn_All_Smooth.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_All_Smooth, 2);
            this.btn_All_Smooth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_All_Smooth.Location = new System.Drawing.Point(477, 1);
            this.btn_All_Smooth.Margin = new System.Windows.Forms.Padding(1);
            this.btn_All_Smooth.Name = "btn_All_Smooth";
            this.btn_All_Smooth.Size = new System.Drawing.Size(34, 36);
            this.btn_All_Smooth.TabIndex = 43;
            this.toolTip.SetToolTip(this.btn_All_Smooth, "Vertical & horizontal smoothing of selected cells");
            this.btn_All_Smooth.UseVisualStyleBackColor = false;
            this.btn_All_Smooth.Click += new System.EventHandler(this.btn_All_Smooth_Click);
            // 
            // btn_H_Smooth
            // 
            this.btn_H_Smooth.BackColor = System.Drawing.SystemColors.Control;
            this.btn_H_Smooth.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_H_Smooth.BackgroundImage")));
            this.btn_H_Smooth.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_H_Smooth, 2);
            this.btn_H_Smooth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_H_Smooth.Location = new System.Drawing.Point(513, 1);
            this.btn_H_Smooth.Margin = new System.Windows.Forms.Padding(1);
            this.btn_H_Smooth.Name = "btn_H_Smooth";
            this.btn_H_Smooth.Size = new System.Drawing.Size(34, 36);
            this.btn_H_Smooth.TabIndex = 44;
            this.toolTip.SetToolTip(this.btn_H_Smooth, "Horizontal smoothing of selected cells");
            this.btn_H_Smooth.UseVisualStyleBackColor = false;
            this.btn_H_Smooth.Click += new System.EventHandler(this.btn_H_Smooth_Click);
            // 
            // btn_V_Smooth
            // 
            this.btn_V_Smooth.BackColor = System.Drawing.SystemColors.Control;
            this.btn_V_Smooth.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_V_Smooth.BackgroundImage")));
            this.btn_V_Smooth.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_V_Smooth, 2);
            this.btn_V_Smooth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_V_Smooth.Location = new System.Drawing.Point(549, 1);
            this.btn_V_Smooth.Margin = new System.Windows.Forms.Padding(1);
            this.btn_V_Smooth.Name = "btn_V_Smooth";
            this.btn_V_Smooth.Size = new System.Drawing.Size(34, 36);
            this.btn_V_Smooth.TabIndex = 45;
            this.toolTip.SetToolTip(this.btn_V_Smooth, "Vertical smoothing of selected cells");
            this.btn_V_Smooth.UseVisualStyleBackColor = false;
            this.btn_V_Smooth.Click += new System.EventHandler(this.btn_V_Smooth_Click);
            // 
            // btn_Redo
            // 
            this.toolBar.SetColumnSpan(this.btn_Redo, 2);
            this.btn_Redo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Redo.Enabled = false;
            this.btn_Redo.Image = ((System.Drawing.Image)(resources.GetObject("btn_Redo.Image")));
            this.btn_Redo.Location = new System.Drawing.Point(261, 1);
            this.btn_Redo.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Redo.Name = "btn_Redo";
            this.btn_Redo.Size = new System.Drawing.Size(34, 36);
            this.btn_Redo.TabIndex = 46;
            this.toolTip.SetToolTip(this.btn_Redo, "Redo");
            this.btn_Redo.UseVisualStyleBackColor = true;
            this.btn_Redo.Click += new System.EventHandler(this.btn_Redo_Click);
            // 
            // btn_RotateGraphDockedLocation
            // 
            this.btn_RotateGraphDockedLocation.BackColor = System.Drawing.SystemColors.Control;
            this.btn_RotateGraphDockedLocation.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_RotateGraphDockedLocation, 2);
            this.btn_RotateGraphDockedLocation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_RotateGraphDockedLocation.Image = ((System.Drawing.Image)(resources.GetObject("btn_RotateGraphDockedLocation.Image")));
            this.btn_RotateGraphDockedLocation.Location = new System.Drawing.Point(513, 39);
            this.btn_RotateGraphDockedLocation.Margin = new System.Windows.Forms.Padding(1);
            this.btn_RotateGraphDockedLocation.Name = "btn_RotateGraphDockedLocation";
            this.btn_RotateGraphDockedLocation.Size = new System.Drawing.Size(34, 36);
            this.btn_RotateGraphDockedLocation.TabIndex = 48;
            this.toolTip.SetToolTip(this.btn_RotateGraphDockedLocation, "Toggle graph dock location");
            this.btn_RotateGraphDockedLocation.UseVisualStyleBackColor = false;
            this.btn_RotateGraphDockedLocation.Click += new System.EventHandler(this.btn_RotateGraphDockedLocation_Click);
            // 
            // btn_TableEditMode
            // 
            this.btn_TableEditMode.BackColor = System.Drawing.SystemColors.Control;
            this.btn_TableEditMode.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_TableEditMode.BackgroundImage")));
            this.btn_TableEditMode.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.toolBar.SetColumnSpan(this.btn_TableEditMode, 2);
            this.btn_TableEditMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_TableEditMode.Location = new System.Drawing.Point(477, 39);
            this.btn_TableEditMode.Margin = new System.Windows.Forms.Padding(1);
            this.btn_TableEditMode.Name = "btn_TableEditMode";
            this.btn_TableEditMode.Size = new System.Drawing.Size(34, 36);
            this.btn_TableEditMode.TabIndex = 49;
            this.toolTip.SetToolTip(this.btn_TableEditMode, "Switch between table and % error mode");
            this.btn_TableEditMode.UseVisualStyleBackColor = false;
            this.btn_TableEditMode.Click += new System.EventHandler(this.btn_TableEditMode_Click);
            // 
            // btn_Graph3d_PointMoveMode
            // 
            this.toolBar.SetColumnSpan(this.btn_Graph3d_PointMoveMode, 2);
            this.btn_Graph3d_PointMoveMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Graph3d_PointMoveMode.Enabled = false;
            this.btn_Graph3d_PointMoveMode.ImageIndex = 8;
            this.btn_Graph3d_PointMoveMode.ImageList = this.imageList1;
            this.btn_Graph3d_PointMoveMode.Location = new System.Drawing.Point(387, 39);
            this.btn_Graph3d_PointMoveMode.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Graph3d_PointMoveMode.MaximumSize = new System.Drawing.Size(34, 36);
            this.btn_Graph3d_PointMoveMode.Name = "btn_Graph3d_PointMoveMode";
            this.btn_Graph3d_PointMoveMode.Padding = new System.Windows.Forms.Padding(3);
            this.btn_Graph3d_PointMoveMode.Size = new System.Drawing.Size(34, 36);
            this.btn_Graph3d_PointMoveMode.TabIndex = 79;
            this.toolTip.SetToolTip(this.btn_Graph3d_PointMoveMode, "Graph point move mode");
            this.btn_Graph3d_PointMoveMode.UseVisualStyleBackColor = true;
            this.btn_Graph3d_PointMoveMode.Click += new System.EventHandler(this.btn_Graph3d_PointMoveMode_Click);
            // 
            // btn_Graph3d_PointSelectMode
            // 
            this.toolBar.SetColumnSpan(this.btn_Graph3d_PointSelectMode, 2);
            this.btn_Graph3d_PointSelectMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Graph3d_PointSelectMode.ImageIndex = 6;
            this.btn_Graph3d_PointSelectMode.ImageList = this.imageList1;
            this.btn_Graph3d_PointSelectMode.Location = new System.Drawing.Point(423, 39);
            this.btn_Graph3d_PointSelectMode.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Graph3d_PointSelectMode.MaximumSize = new System.Drawing.Size(34, 36);
            this.btn_Graph3d_PointSelectMode.Name = "btn_Graph3d_PointSelectMode";
            this.btn_Graph3d_PointSelectMode.Padding = new System.Windows.Forms.Padding(3);
            this.btn_Graph3d_PointSelectMode.Size = new System.Drawing.Size(34, 36);
            this.btn_Graph3d_PointSelectMode.TabIndex = 80;
            this.toolTip.SetToolTip(this.btn_Graph3d_PointSelectMode, "Graph point select mode");
            this.btn_Graph3d_PointSelectMode.UseVisualStyleBackColor = true;
            this.btn_Graph3d_PointSelectMode.Click += new System.EventHandler(this.btn_Graph3d_PointSelectMode_Click);
            // 
            // Main_Timer
            // 
            this.Main_Timer.Interval = 200;
            this.Main_Timer.Tick += new System.EventHandler(this.Main_Timer_Tick);
            // 
            // toolBar
            // 
            this.toolBar.ColumnCount = 50;
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.toolBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.toolBar.Controls.Add(this.btn_LoadSample4, 44, 1);
            this.toolBar.Controls.Add(this.btn_Graph3d_PointSelectMode, 24, 1);
            this.toolBar.Controls.Add(this.btn_Graph3d_PointMoveMode, 22, 1);
            this.toolBar.Controls.Add(this.btn_TableEditMode, 27, 1);
            this.toolBar.Controls.Add(this.btn_RotateGraphDockedLocation, 29, 1);
            this.toolBar.Controls.Add(this.btn_Redo, 15, 0);
            this.toolBar.Controls.Add(this.btn_V_Smooth, 30, 0);
            this.toolBar.Controls.Add(this.btn_H_Smooth, 28, 0);
            this.toolBar.Controls.Add(this.btn_All_Smooth, 27, 0);
            this.toolBar.Controls.Add(this.btn_AverageTool, 20, 0);
            this.toolBar.Controls.Add(this.btn_Options, 37, 1);
            this.toolBar.Controls.Add(this.btn_Plot3d_TransposeXY, 31, 1);
            this.toolBar.Controls.Add(this.btn_LoadSample3, 40, 1);
            this.toolBar.Controls.Add(this.btn_LoadSample2, 40, 0);
            this.toolBar.Controls.Add(this.btn_LoadSample1, 44, 0);
            this.toolBar.Controls.Add(this.btn_Graph3D_Instructions, 35, 1);
            this.toolBar.Controls.Add(this.btn_Graph3D_ResetView, 33, 1);
            this.toolBar.Controls.Add(this.btn_ClearAllSelections, 20, 1);
            this.toolBar.Controls.Add(this.btn_All_Interpolate, 31, 0);
            this.toolBar.Controls.Add(this.btn_Undo, 13, 0);
            this.toolBar.Controls.Add(this.btn_ClipMin, 17, 1);
            this.toolBar.Controls.Add(this.btn_ClipMax, 15, 1);
            this.toolBar.Controls.Add(this.btn_Multiply, 7, 1);
            this.toolBar.Controls.Add(this.textBox_Adjust, 1, 1);
            this.toolBar.Controls.Add(this.btn_Divide, 9, 1);
            this.toolBar.Controls.Add(this.btn_Add, 11, 1);
            this.toolBar.Controls.Add(this.btn_Subtract, 13, 1);
            this.toolBar.Controls.Add(this.btn_FillMissingDataGaps, 22, 0);
            this.toolBar.Controls.Add(this.btn_MissingNeighbourFill, 24, 0);
            this.toolBar.Controls.Add(this.btn_Paste, 9, 0);
            this.toolBar.Controls.Add(this.btn_Copy, 7, 0);
            this.toolBar.Controls.Add(this.btn_DecDp, 3, 0);
            this.toolBar.Controls.Add(this.btn_IncDp, 1, 0);
            this.toolBar.Controls.Add(this.btn_H_Interpolate, 33, 0);
            this.toolBar.Controls.Add(this.btn_V_Interpolate, 35, 0);
            this.toolBar.Controls.Add(this.btn_SetSelectedCellsValue, 5, 1);
            this.toolBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolBar.Location = new System.Drawing.Point(0, 0);
            this.toolBar.MinimumSize = new System.Drawing.Size(750, 76);
            this.toolBar.Name = "toolBar";
            this.toolBar.RowCount = 2;
            this.toolBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.toolBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.toolBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.toolBar.Size = new System.Drawing.Size(886, 76);
            this.toolBar.TabIndex = 9;
            // 
            // btn_LoadSample4
            // 
            this.toolBar.SetColumnSpan(this.btn_LoadSample4, 4);
            this.btn_LoadSample4.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.btn_LoadSample4.Location = new System.Drawing.Point(783, 39);
            this.btn_LoadSample4.Margin = new System.Windows.Forms.Padding(1);
            this.btn_LoadSample4.Name = "btn_LoadSample4";
            this.btn_LoadSample4.Size = new System.Drawing.Size(70, 36);
            this.btn_LoadSample4.TabIndex = 81;
            this.btn_LoadSample4.Text = "Load Trq Follower";
            this.btn_LoadSample4.UseVisualStyleBackColor = true;
            this.btn_LoadSample4.Visible = false;
            this.btn_LoadSample4.Click += new System.EventHandler(this.btn_LoadSample4_Click);
            // 
            // btn_LoadSample3
            // 
            this.toolBar.SetColumnSpan(this.btn_LoadSample3, 2);
            this.btn_LoadSample3.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.btn_LoadSample3.Location = new System.Drawing.Point(711, 39);
            this.btn_LoadSample3.Margin = new System.Windows.Forms.Padding(1);
            this.btn_LoadSample3.MinimumSize = new System.Drawing.Size(70, 36);
            this.btn_LoadSample3.Name = "btn_LoadSample3";
            this.btn_LoadSample3.Size = new System.Drawing.Size(70, 36);
            this.btn_LoadSample3.TabIndex = 39;
            this.btn_LoadSample3.Text = "Load VE Table";
            this.btn_LoadSample3.UseVisualStyleBackColor = true;
            this.btn_LoadSample3.Visible = false;
            this.btn_LoadSample3.Click += new System.EventHandler(this.btn_LoadSample3_Click);
            // 
            // btn_LoadSample2
            // 
            this.toolBar.SetColumnSpan(this.btn_LoadSample2, 4);
            this.btn_LoadSample2.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.btn_LoadSample2.Location = new System.Drawing.Point(711, 1);
            this.btn_LoadSample2.Margin = new System.Windows.Forms.Padding(1);
            this.btn_LoadSample2.Name = "btn_LoadSample2";
            this.btn_LoadSample2.Size = new System.Drawing.Size(70, 36);
            this.btn_LoadSample2.TabIndex = 38;
            this.btn_LoadSample2.Text = "Load VTT";
            this.btn_LoadSample2.UseVisualStyleBackColor = true;
            this.btn_LoadSample2.Visible = false;
            this.btn_LoadSample2.Click += new System.EventHandler(this.btn_LoadSample2_Click);
            // 
            // btn_LoadSample1
            // 
            this.toolBar.SetColumnSpan(this.btn_LoadSample1, 4);
            this.btn_LoadSample1.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.btn_LoadSample1.Location = new System.Drawing.Point(783, 1);
            this.btn_LoadSample1.Margin = new System.Windows.Forms.Padding(1);
            this.btn_LoadSample1.Name = "btn_LoadSample1";
            this.btn_LoadSample1.Size = new System.Drawing.Size(70, 36);
            this.btn_LoadSample1.TabIndex = 37;
            this.btn_LoadSample1.Text = "Load Missing VTT";
            this.btn_LoadSample1.UseVisualStyleBackColor = true;
            this.btn_LoadSample1.Visible = false;
            this.btn_LoadSample1.Click += new System.EventHandler(this.btn_LoadSample1_Click);
            // 
            // btn_Multiply
            // 
            this.btn_Multiply.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Multiply.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_Multiply.BackgroundImage")));
            this.btn_Multiply.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_Multiply, 2);
            this.btn_Multiply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Multiply.Location = new System.Drawing.Point(117, 39);
            this.btn_Multiply.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Multiply.Name = "btn_Multiply";
            this.btn_Multiply.Size = new System.Drawing.Size(34, 36);
            this.btn_Multiply.TabIndex = 21;
            this.btn_Multiply.UseVisualStyleBackColor = false;
            this.btn_Multiply.Click += new System.EventHandler(this.btn_Multiply_Click);
            // 
            // textBox_Adjust
            // 
            this.toolBar.SetColumnSpan(this.textBox_Adjust, 4);
            this.textBox_Adjust.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_Adjust.Font = new System.Drawing.Font("Calibri", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Adjust.Location = new System.Drawing.Point(9, 45);
            this.textBox_Adjust.Margin = new System.Windows.Forms.Padding(1, 7, 1, 5);
            this.textBox_Adjust.MaximumSize = new System.Drawing.Size(70, 28);
            this.textBox_Adjust.MaxLength = 9;
            this.textBox_Adjust.MinimumSize = new System.Drawing.Size(70, 28);
            this.textBox_Adjust.Name = "textBox_Adjust";
            this.textBox_Adjust.Size = new System.Drawing.Size(70, 26);
            this.textBox_Adjust.TabIndex = 18;
            this.textBox_Adjust.WordWrap = false;
            // 
            // btn_Divide
            // 
            this.btn_Divide.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Divide.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_Divide.BackgroundImage")));
            this.btn_Divide.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_Divide, 2);
            this.btn_Divide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Divide.Location = new System.Drawing.Point(153, 39);
            this.btn_Divide.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Divide.Name = "btn_Divide";
            this.btn_Divide.Size = new System.Drawing.Size(34, 36);
            this.btn_Divide.TabIndex = 10;
            this.btn_Divide.UseVisualStyleBackColor = false;
            this.btn_Divide.Click += new System.EventHandler(this.btn_Divide_Click);
            // 
            // btn_Add
            // 
            this.btn_Add.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Add.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_Add.BackgroundImage")));
            this.btn_Add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_Add, 2);
            this.btn_Add.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Add.Location = new System.Drawing.Point(189, 39);
            this.btn_Add.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Add.Name = "btn_Add";
            this.btn_Add.Size = new System.Drawing.Size(34, 36);
            this.btn_Add.TabIndex = 11;
            this.btn_Add.UseVisualStyleBackColor = false;
            this.btn_Add.Click += new System.EventHandler(this.btn_Add_Click);
            // 
            // btn_Subtract
            // 
            this.btn_Subtract.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Subtract.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_Subtract.BackgroundImage")));
            this.btn_Subtract.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolBar.SetColumnSpan(this.btn_Subtract, 2);
            this.btn_Subtract.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Subtract.Location = new System.Drawing.Point(225, 39);
            this.btn_Subtract.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Subtract.Name = "btn_Subtract";
            this.btn_Subtract.Size = new System.Drawing.Size(34, 36);
            this.btn_Subtract.TabIndex = 12;
            this.btn_Subtract.UseVisualStyleBackColor = false;
            this.btn_Subtract.Click += new System.EventHandler(this.btn_Subtract_Click);
            // 
            // btn_Paste
            // 
            this.toolBar.SetColumnSpan(this.btn_Paste, 2);
            this.btn_Paste.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Paste.Image = ((System.Drawing.Image)(resources.GetObject("btn_Paste.Image")));
            this.btn_Paste.Location = new System.Drawing.Point(153, 1);
            this.btn_Paste.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Paste.Name = "btn_Paste";
            this.btn_Paste.Size = new System.Drawing.Size(34, 36);
            this.btn_Paste.TabIndex = 9;
            this.btn_Paste.UseVisualStyleBackColor = true;
            this.btn_Paste.Click += new System.EventHandler(this.btn_Paste_Click);
            // 
            // btn_Copy
            // 
            this.toolBar.SetColumnSpan(this.btn_Copy, 2);
            this.btn_Copy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Copy.Image = ((System.Drawing.Image)(resources.GetObject("btn_Copy.Image")));
            this.btn_Copy.Location = new System.Drawing.Point(117, 1);
            this.btn_Copy.Margin = new System.Windows.Forms.Padding(1);
            this.btn_Copy.Name = "btn_Copy";
            this.btn_Copy.Size = new System.Drawing.Size(34, 36);
            this.btn_Copy.TabIndex = 9;
            this.btn_Copy.UseVisualStyleBackColor = true;
            this.btn_Copy.Click += new System.EventHandler(this.btn_Copy_Click);
            // 
            // DgvTable
            // 
            this.DgvTable.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.DgvTable.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DgvTable.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystroke;
            this.DgvTable.Location = new System.Drawing.Point(0, 0);
            this.DgvTable.Margin = new System.Windows.Forms.Padding(0);
            this.DgvTable.Name = "DgvTable";
            this.DgvTable.RowTemplate.DefaultCellStyle.NullValue = null;
            this.DgvTable.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.DgvTable.Size = new System.Drawing.Size(170, 150);
            this.DgvTable.TabIndex = 82;
            // 
            // vScrollBar
            // 
            this.vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this.vScrollBar.Location = new System.Drawing.Point(476, 0);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(20, 272);
            this.vScrollBar.TabIndex = 78;
            // 
            // hScrollBar
            // 
            this.hScrollBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.hScrollBar.Location = new System.Drawing.Point(0, 252);
            this.hScrollBar.Name = "hScrollBar";
            this.hScrollBar.Size = new System.Drawing.Size(476, 20);
            this.hScrollBar.TabIndex = 80;
            // 
            // blankingPanel
            // 
            this.blankingPanel.BackColor = System.Drawing.Color.MistyRose;
            this.blankingPanel.Enabled = false;
            this.blankingPanel.Location = new System.Drawing.Point(0, 0);
            this.blankingPanel.Name = "blankingPanel";
            this.blankingPanel.Size = new System.Drawing.Size(38, 18);
            this.blankingPanel.TabIndex = 85;
            // 
            // colHeader
            // 
            this.colHeader.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.colHeader.Enabled = false;
            this.colHeader.Location = new System.Drawing.Point(0, 18);
            this.colHeader.Name = "colHeader";
            this.colHeader.Size = new System.Drawing.Size(18, 100);
            this.colHeader.TabIndex = 84;
            // 
            // rowHeader
            // 
            this.rowHeader.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.rowHeader.Enabled = false;
            this.rowHeader.Location = new System.Drawing.Point(38, 0);
            this.rowHeader.Name = "rowHeader";
            this.rowHeader.Size = new System.Drawing.Size(100, 18);
            this.rowHeader.TabIndex = 83;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.SystemColors.Control;
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 76);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.splitContainer1.Panel1.Controls.Add(this.hScrollBar);
            this.splitContainer1.Panel1.Controls.Add(this.vScrollBar);
            this.splitContainer1.Panel1.Controls.Add(this.blankingPanel);
            this.splitContainer1.Panel1.Controls.Add(this.rowHeader);
            this.splitContainer1.Panel1.Controls.Add(this.colHeader);
            this.splitContainer1.Panel1.Controls.Add(this.DgvTable);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.Graph3d_UserControl);
            this.splitContainer1.Size = new System.Drawing.Size(886, 274);
            this.splitContainer1.SplitterDistance = 498;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 77;
            // 
            // Graph3d_UserControl
            // 
            this.Graph3d_UserControl.BackColor = System.Drawing.SystemColors.Info;
            this.Graph3d_UserControl.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.Graph3d_UserControl.BorderColorFocus = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(153)))), ((int)(((byte)(255)))));
            this.Graph3d_UserControl.BorderColorNormal = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.Graph3d_UserControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Graph3d_UserControl.LegendPos = Plot3D.Editor3D.eLegendPos.BottomLeft;
            this.Graph3d_UserControl.Location = new System.Drawing.Point(0, 0);
            this.Graph3d_UserControl.Name = "Graph3d_UserControl";
            this.Graph3d_UserControl.Normalize = Plot3D.Editor3D.eNormalize.Separate;
            this.Graph3d_UserControl.PointMoveMode = false;
            this.Graph3d_UserControl.PointSelectMode = false;
            this.Graph3d_UserControl.Raster = Plot3D.Editor3D.eRaster.Labels;
            this.Graph3d_UserControl.Size = new System.Drawing.Size(380, 272);
            this.Graph3d_UserControl.TabIndex = 75;
            this.Graph3d_UserControl.TooltipMode = ((Plot3D.Editor3D.eTooltip)(((Plot3D.Editor3D.eTooltip.UserText | Plot3D.Editor3D.eTooltip.Coord) 
            | Plot3D.Editor3D.eTooltip.Hover)));
            this.Graph3d_UserControl.TopLegendColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(150)))));
            // 
            // TableEditor3D
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolBar);
            this.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "TableEditor3D";
            this.Size = new System.Drawing.Size(886, 350);
            this.contextMenuStrip.ResumeLayout(false);
            this.toolBar.ResumeLayout(false);
            this.toolBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DgvTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.colHeader)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rowHeader)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

#endregion
        public System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        public System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Timer Main_Timer;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_Copy;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_Paste;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_CopyWithAxis;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.TableLayoutPanel toolBar;
        private System.Windows.Forms.Button btn_ClearAllSelections;
        private System.Windows.Forms.Button btn_All_Interpolate;
        private System.Windows.Forms.Button btn_Undo;
        private System.Windows.Forms.Button btn_ClipMin;
        private System.Windows.Forms.Button btn_ClipMax;
        private System.Windows.Forms.Button btn_Multiply;
        private System.Windows.Forms.TextBox textBox_Adjust;
        private System.Windows.Forms.Button btn_Divide;
        private System.Windows.Forms.Button btn_Add;
        private System.Windows.Forms.Button btn_Subtract;
        private System.Windows.Forms.Button btn_FillMissingDataGaps;
        private System.Windows.Forms.Button btn_MissingNeighbourFill;
        private System.Windows.Forms.Button btn_Paste;
        private System.Windows.Forms.Button btn_Copy;
        private System.Windows.Forms.Button btn_DecDp;
        private System.Windows.Forms.Button btn_IncDp;
        private System.Windows.Forms.Button btn_H_Interpolate;
        private System.Windows.Forms.Button btn_V_Interpolate;
        private System.Windows.Forms.Button btn_SetSelectedCellsValue;
        private System.Windows.Forms.Button btn_LoadSample3;
        private System.Windows.Forms.Button btn_LoadSample2;
        private System.Windows.Forms.Button btn_LoadSample1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button btn_Options;
        private System.Windows.Forms.Button btn_Plot3d_TransposeXY;
        private System.Windows.Forms.Button btn_Graph3D_Instructions;
        private System.Windows.Forms.Button btn_Graph3D_ResetView;
        private System.Windows.Forms.Button btn_AverageTool;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_AddtnlPasteFcns;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteXAxisPCMTEC;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteYAxis;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteXAxis;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteSpecial;
        private System.Windows.Forms.ToolStripMenuItem multiplyByToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem multiplyByHalfToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem subtractToolStripMenuItem;
        private System.Windows.Forms.Button btn_Redo;
        private System.Windows.Forms.Button btn_V_Smooth;
        private System.Windows.Forms.Button btn_H_Smooth;
        private System.Windows.Forms.Button btn_All_Smooth;
        public Plot3D.Editor3D Graph3d_UserControl;
        private System.Windows.Forms.Button btn_RotateGraphDockedLocation;
        private System.Windows.Forms.DataGridView DgvTable;
        private System.Windows.Forms.VScrollBar vScrollBar;
        private System.Windows.Forms.HScrollBar hScrollBar;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteWithXYAxis;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteTable;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteTableWithRowAxis;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_PasteTableWithColAxis;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem ToolStrip_ClearTable;
        private System.Windows.Forms.Panel blankingPanel;
        private System.Windows.Forms.DataGridView colHeader;
        private System.Windows.Forms.DataGridView rowHeader;
        public System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btn_TableEditMode;
        private System.Windows.Forms.Button btn_Graph3d_PointSelectMode;
        private System.Windows.Forms.Button btn_Graph3d_PointMoveMode;
        private System.Windows.Forms.Button btn_LoadSample4;
        private System.Windows.Forms.ToolStripMenuItem divideByToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem divideByHalfToolStripMenuItem;
    }
}