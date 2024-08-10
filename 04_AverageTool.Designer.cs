namespace TableEditor
{
    partial class AverageTool
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AverageTool));
            this.Dgv_NewEntry = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyWithAxisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_ClearAverage = new System.Windows.Forms.Button();
            this.btn_AddToAverage = new System.Windows.Forms.Button();
            this.btn_Reset = new System.Windows.Forms.Button();
            this.btn_CopyTable = new System.Windows.Forms.Button();
            this.btn_Close = new System.Windows.Forms.Button();
            this.Dgv_RunAvgCnt = new System.Windows.Forms.DataGridView();
            this.Dgv_NewEntryCnt = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.Dgv_RunAvg = new System.Windows.Forms.DataGridView();
            this.panel5 = new System.Windows.Forms.Panel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.cBox_LinkScrollBars = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_NewEntry)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_RunAvgCnt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_NewEntryCnt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_RunAvg)).BeginInit();
            this.panel5.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // Dgv_NewEntry
            // 
            this.Dgv_NewEntry.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Dgv_NewEntry.ContextMenuStrip = this.contextMenuStrip1;
            this.Dgv_NewEntry.Location = new System.Drawing.Point(3, 407);
            this.Dgv_NewEntry.Name = "Dgv_NewEntry";
            this.Dgv_NewEntry.Size = new System.Drawing.Size(438, 301);
            this.Dgv_NewEntry.TabIndex = 2;
            this.Dgv_NewEntry.Scroll += new System.Windows.Forms.ScrollEventHandler(this.Dgv_NewEntry_Scroll);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyWithAxisToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(156, 70);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // copyWithAxisToolStripMenuItem
            // 
            this.copyWithAxisToolStripMenuItem.Name = "copyWithAxisToolStripMenuItem";
            this.copyWithAxisToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.copyWithAxisToolStripMenuItem.Text = "Copy With Axis";
            this.copyWithAxisToolStripMenuItem.Click += new System.EventHandler(this.copyWithAxisToolStripMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.PasteToolStripMenuItem_Click);
            // 
            // btn_ClearAverage
            // 
            this.btn_ClearAverage.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_ClearAverage.Location = new System.Drawing.Point(85, 3);
            this.btn_ClearAverage.MinimumSize = new System.Drawing.Size(76, 38);
            this.btn_ClearAverage.Name = "btn_ClearAverage";
            this.btn_ClearAverage.Size = new System.Drawing.Size(76, 38);
            this.btn_ClearAverage.TabIndex = 5;
            this.btn_ClearAverage.Text = "Clear Table";
            this.btn_ClearAverage.UseVisualStyleBackColor = true;
            this.btn_ClearAverage.Click += new System.EventHandler(this.btn_ClearNewEntry_Click);
            // 
            // btn_AddToAverage
            // 
            this.btn_AddToAverage.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_AddToAverage.Location = new System.Drawing.Point(3, 3);
            this.btn_AddToAverage.MinimumSize = new System.Drawing.Size(76, 38);
            this.btn_AddToAverage.Name = "btn_AddToAverage";
            this.btn_AddToAverage.Size = new System.Drawing.Size(76, 38);
            this.btn_AddToAverage.TabIndex = 8;
            this.btn_AddToAverage.Text = "Add to Average";
            this.btn_AddToAverage.UseVisualStyleBackColor = true;
            this.btn_AddToAverage.Click += new System.EventHandler(this.btn_AddToAverage_Click);
            // 
            // btn_Reset
            // 
            this.btn_Reset.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Reset.Location = new System.Drawing.Point(167, 3);
            this.btn_Reset.MinimumSize = new System.Drawing.Size(76, 38);
            this.btn_Reset.Name = "btn_Reset";
            this.btn_Reset.Size = new System.Drawing.Size(76, 38);
            this.btn_Reset.TabIndex = 9;
            this.btn_Reset.Text = "Clear Table";
            this.btn_Reset.UseVisualStyleBackColor = true;
            this.btn_Reset.Click += new System.EventHandler(this.btn_ClearRunningAverage_Click);
            // 
            // btn_CopyTable
            // 
            this.btn_CopyTable.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_CopyTable.Location = new System.Drawing.Point(85, 3);
            this.btn_CopyTable.MinimumSize = new System.Drawing.Size(76, 38);
            this.btn_CopyTable.Name = "btn_CopyTable";
            this.btn_CopyTable.Size = new System.Drawing.Size(76, 38);
            this.btn_CopyTable.TabIndex = 7;
            this.btn_CopyTable.Text = "Copy Table";
            this.btn_CopyTable.UseVisualStyleBackColor = true;
            this.btn_CopyTable.Click += new System.EventHandler(this.btn_CopyTable_Click);
            // 
            // btn_Close
            // 
            this.btn_Close.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Close.Location = new System.Drawing.Point(3, 3);
            this.btn_Close.MinimumSize = new System.Drawing.Size(76, 38);
            this.btn_Close.Name = "btn_Close";
            this.btn_Close.Size = new System.Drawing.Size(76, 38);
            this.btn_Close.TabIndex = 4;
            this.btn_Close.Text = "Close";
            this.btn_Close.UseVisualStyleBackColor = true;
            this.btn_Close.Click += new System.EventHandler(this.btn_Close_Click);
            // 
            // Dgv_RunAvgCnt
            // 
            this.Dgv_RunAvgCnt.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Dgv_RunAvgCnt.ContextMenuStrip = this.contextMenuStrip1;
            this.Dgv_RunAvgCnt.Location = new System.Drawing.Point(3, 45);
            this.Dgv_RunAvgCnt.Name = "Dgv_RunAvgCnt";
            this.Dgv_RunAvgCnt.Size = new System.Drawing.Size(659, 300);
            this.Dgv_RunAvgCnt.TabIndex = 1;
            this.Dgv_RunAvgCnt.Scroll += new System.Windows.Forms.ScrollEventHandler(this.Dgv_RunAvgCnt_Scroll);
            // 
            // Dgv_NewEntryCnt
            // 
            this.Dgv_NewEntryCnt.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Dgv_NewEntryCnt.ContextMenuStrip = this.contextMenuStrip1;
            this.Dgv_NewEntryCnt.Location = new System.Drawing.Point(3, 407);
            this.Dgv_NewEntryCnt.Name = "Dgv_NewEntryCnt";
            this.Dgv_NewEntryCnt.Size = new System.Drawing.Size(659, 301);
            this.Dgv_NewEntryCnt.TabIndex = 3;
            this.Dgv_NewEntryCnt.Scroll += new System.Windows.Forms.ScrollEventHandler(this.Dgv_NewEntryCnt_Scroll);
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel2);
            this.splitContainer1.Panel1MinSize = 360;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tableLayoutPanel3);
            this.splitContainer1.Panel2MinSize = 360;
            this.splitContainer1.Size = new System.Drawing.Size(1350, 729);
            this.splitContainer1.SplitterDistance = 675;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 1;
            this.splitContainer1.DoubleClick += new System.EventHandler(this.splitContainer1_DoubleClick);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 671F));
            this.tableLayoutPanel2.Controls.Add(this.Dgv_NewEntry, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.Dgv_RunAvg, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.panel5, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(671, 725);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // Dgv_RunAvg
            // 
            this.Dgv_RunAvg.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Dgv_RunAvg.ContextMenuStrip = this.contextMenuStrip1;
            this.Dgv_RunAvg.Location = new System.Drawing.Point(3, 45);
            this.Dgv_RunAvg.Name = "Dgv_RunAvg";
            this.Dgv_RunAvg.Size = new System.Drawing.Size(411, 300);
            this.Dgv_RunAvg.TabIndex = 13;
            this.Dgv_RunAvg.Scroll += new System.Windows.Forms.ScrollEventHandler(this.Dgv_RunAvg_Scroll);
            // 
            // panel5
            // 
            this.panel5.AutoSize = true;
            this.panel5.Controls.Add(this.flowLayoutPanel2);
            this.panel5.Controls.Add(this.label1);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel5.Location = new System.Drawing.Point(0, 362);
            this.panel5.Margin = new System.Windows.Forms.Padding(0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(671, 42);
            this.panel5.TabIndex = 14;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.btn_AddToAverage);
            this.flowLayoutPanel2.Controls.Add(this.btn_ClearAverage);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(164, 42);
            this.flowLayoutPanel2.TabIndex = 15;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Right;
            this.label1.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(472, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 42);
            this.label1.TabIndex = 15;
            this.label1.Text = "Average values from data log";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btn_Close);
            this.flowLayoutPanel1.Controls.Add(this.btn_CopyTable);
            this.flowLayoutPanel1.Controls.Add(this.btn_Reset);
            this.flowLayoutPanel1.Controls.Add(this.cBox_LinkScrollBars);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(360, 42);
            this.flowLayoutPanel1.TabIndex = 14;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // cBox_LinkScrollBars
            // 
            this.cBox_LinkScrollBars.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.cBox_LinkScrollBars.AutoSize = true;
            this.cBox_LinkScrollBars.Checked = true;
            this.cBox_LinkScrollBars.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cBox_LinkScrollBars.Font = new System.Drawing.Font("Calibri", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cBox_LinkScrollBars.Location = new System.Drawing.Point(249, 3);
            this.cBox_LinkScrollBars.Name = "cBox_LinkScrollBars";
            this.cBox_LinkScrollBars.Size = new System.Drawing.Size(108, 38);
            this.cBox_LinkScrollBars.TabIndex = 10;
            this.cBox_LinkScrollBars.Text = "Link Scroll Bars";
            this.cBox_LinkScrollBars.UseVisualStyleBackColor = true;
            this.cBox_LinkScrollBars.CheckedChanged += new System.EventHandler(this.cBox_LinkScrollBars_CheckedChanged);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.Dgv_NewEntryCnt, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.Dgv_RunAvgCnt, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(665, 725);
            this.tableLayoutPanel3.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Right;
            this.label2.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(469, 362);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(193, 42);
            this.label2.TabIndex = 12;
            this.label2.Text = "Cell hit counts from data log";
            this.label2.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // AverageTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(3, 3);
            this.ClientSize = new System.Drawing.Size(1350, 729);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(790, 39);
            this.Name = "AverageTool";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Average Tool";
            this.Activated += new System.EventHandler(this.AverageTool_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AverageTool_FormClosing);
            this.Load += new System.EventHandler(this.AverageTool_Load);
            this.Shown += new System.EventHandler(this.AverageTool_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_NewEntry)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_RunAvgCnt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_NewEntryCnt)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Dgv_RunAvg)).EndInit();
            this.panel5.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridView Dgv_RunAvgCnt;
        private System.Windows.Forms.DataGridView Dgv_NewEntry;
        private System.Windows.Forms.DataGridView Dgv_NewEntryCnt;
        private System.Windows.Forms.Button btn_Close;
        private System.Windows.Forms.Button btn_ClearAverage;
        private System.Windows.Forms.Button btn_CopyTable;
        private System.Windows.Forms.Button btn_Reset;
        private System.Windows.Forms.Button btn_AddToAverage;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox cBox_LinkScrollBars;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.DataGridView Dgv_RunAvg;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.ToolStripMenuItem copyWithAxisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
    }
}