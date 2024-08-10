using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TableEditor
{
    public partial class AverageTool : Form
    {
        public double[] RowHeaders { get; set; }
        public double[] ColHeaders { get; set; }
        public DgvCtrl.DgvNumFormat Format { get; set; }
        public bool ReDimReqd { get; set; }
        public bool FormLoaded { get; set; }

        DgvCtrl dgv_RunAvg;
        DgvCtrl dgv_RunAvgCnt;
        DgvCtrl dgv_NewEntry;
        DgvCtrl dgv_NewEntryCnt;

        Color defaultBackColour = Color.White;

        public AverageTool()
        {
            InitializeComponent();

            // Disable the copy button
            btn_CopyTable.Enabled = false;

            // Create the dgv instances. Pass in the dgv's from the form
            dgv_RunAvg = new DgvCtrl
            {
                Dgv = Dgv_RunAvg,
                UndoEnabled = false,
                CopyPasteEnabled = true,
                UseMyScrollBars = false,
                ColourTheme = ColourScheme.None,
                InstanceName = "dgv_RunAvg"
            };
            dgv_RunAvgCnt = new DgvCtrl
            {
                Dgv = Dgv_RunAvgCnt,
                UndoEnabled = false,
                CopyPasteEnabled = true,
                UseMyScrollBars = false,
                ColourTheme = ColourScheme.None,
                InstanceName = "dgv_RunAvgCnt"
            };
            dgv_NewEntry = new DgvCtrl
            {
                Dgv = Dgv_NewEntry,
                UndoEnabled = false,
                CopyPasteEnabled = true,
                UseMyScrollBars = false,
                ColourTheme = ColourScheme.None,
                InstanceName = "dgv_NewEntry"
            };
            dgv_NewEntryCnt = new DgvCtrl
            {
                Dgv = Dgv_NewEntryCnt,
                UndoEnabled = false,
                CopyPasteEnabled = true,
                UseMyScrollBars = false,
                ColourTheme = ColourScheme.None,
                InstanceName = "dgv_NewEntryCnt"
            };

            // Kicks off the dgv classes
            dgv_RunAvg.Initialise();
            dgv_RunAvgCnt.Initialise();
            dgv_NewEntry.Initialise();
            dgv_NewEntryCnt.Initialise();

            // Dgv readonly property
            dgv_RunAvg.ReadOnly = true;
            dgv_RunAvgCnt.ReadOnly = true;
            dgv_NewEntry.ReadOnly = true;
            dgv_NewEntryCnt.ReadOnly = true;

            // Dgv font property
            dgv_RunAvg.Font = new Font("Calibri", 8.25f, FontStyle.Regular);
            dgv_RunAvgCnt.Font = new Font("Calibri", 8.25f, FontStyle.Regular);
            dgv_NewEntry.Font = new Font("Calibri", 8.25f, FontStyle.Regular);
            dgv_NewEntryCnt.Font = new Font("Calibri", 8.25f, FontStyle.Regular);

            // Back colour
            dgv_RunAvg.CellsBackColour = defaultBackColour;
            dgv_RunAvgCnt.CellsBackColour = defaultBackColour;
            dgv_NewEntry.CellsBackColour = defaultBackColour;
            dgv_NewEntryCnt.CellsBackColour = defaultBackColour;

            // Important, set the hasData bool for paste function to work!
            dgv_RunAvg.DgvHasData = true;
            dgv_RunAvgCnt.DgvHasData = true;
            dgv_NewEntry.DgvHasData = true;
            dgv_NewEntryCnt.DgvHasData = true;

            // Scroll bars
            dgv_RunAvg.ScrollBars = ScrollBars.Both;
            dgv_RunAvgCnt.ScrollBars = ScrollBars.Both;
            dgv_NewEntry.ScrollBars = ScrollBars.Both;
            dgv_NewEntryCnt.ScrollBars = ScrollBars.Both;

            // Mouse enter events to switch focus
            dgv_RunAvg.dgv.MouseEnter += Dgv_RunAvg_MouseEnter;
            dgv_RunAvgCnt.dgv.MouseEnter += Dgv_RunAvgCnt_MouseEnter;
            dgv_NewEntry.dgv.MouseEnter += Dgv_NewEntry_MouseEnter;
            dgv_NewEntryCnt.dgv.MouseEnter += Dgv_NewEntryCnt_MouseEnter;
        }

        private void Dgv_RunAvg_MouseEnter(object sender, EventArgs e)
        {
            dgv_RunAvg.dgv.Focus();
        }

        private void Dgv_RunAvgCnt_MouseEnter(object sender, EventArgs e)
        {
            dgv_RunAvgCnt.dgv.Focus();
        }

        private void Dgv_NewEntry_MouseEnter(object sender, EventArgs e)
        {
            dgv_NewEntry.dgv.Focus();
        }

        private void Dgv_NewEntryCnt_MouseEnter(object sender, EventArgs e)
        {
            dgv_NewEntryCnt.dgv.Focus();
        }

        private void AverageTool_Load(object sender, EventArgs e)
        {
            // Opening position
            this.Location = new Point(-6, 0);

            // Build the dgv's
            TableBuilder(ReDimReqd);

            // Set form size based on size of dgv's
            AutoSetFormOpeningSize();

            // Set the property
            FormLoaded = true;
        }

        private void AverageTool_Shown(object sender, EventArgs e)
        {

        }

        public void LoadTable(double[] rowHeaders, double[] colHeaders, DgvCtrl.DgvNumFormat format, bool reDimReqd)
        {
            // Load the values
            RowHeaders = rowHeaders;
            ColHeaders = colHeaders;
            Format = format;
            ReDimReqd = reDimReqd;

            // Will drop through here if there is an axis update
            if (FormLoaded)
                TableBuilder(reDimReqd);
        }

        private void TableBuilder(bool reDimReqd = false)
        {
            //Load in the number format
            dgv_RunAvg.dgvNumFormat = Format;
            dgv_RunAvgCnt.dgvNumFormat = Format;
            dgv_NewEntry.dgvNumFormat = Format;
            dgv_NewEntryCnt.dgvNumFormat = Format;

            // Set up the dgv's with the row and column headers from the main editor
            if (reDimReqd)
            {
                dgv_RunAvg.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
                dgv_RunAvgCnt.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
                dgv_NewEntry.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
                dgv_NewEntryCnt.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
            }

            // Axis labels
            dgv_RunAvg.WriteRowHeaderLabels(RowHeaders);
            dgv_RunAvg.WriteColHeaderLabels(ColHeaders);

            dgv_RunAvgCnt.WriteRowHeaderLabels(RowHeaders);
            dgv_RunAvgCnt.WriteColHeaderLabels(ColHeaders);

            dgv_NewEntry.WriteRowHeaderLabels(RowHeaders);
            dgv_NewEntry.WriteColHeaderLabels(ColHeaders);

            dgv_NewEntryCnt.WriteRowHeaderLabels(RowHeaders);
            dgv_NewEntryCnt.WriteColHeaderLabels(ColHeaders);

            // Clear any contents if table was already populated
            ClearDataGridView(dgv_RunAvg);
            ClearDataGridView(dgv_RunAvgCnt);
            ClearDataGridView(dgv_NewEntry);
            ClearDataGridView(dgv_NewEntryCnt);

            // Refresh the dgv's
            dgv_RunAvg.Refresh(RefreshMode.AverageTool);
            dgv_RunAvgCnt.Refresh(RefreshMode.AverageTool);
            dgv_NewEntry.Refresh(RefreshMode.AverageTool);
            dgv_NewEntryCnt.Refresh(RefreshMode.AverageTool);

            ReDimReqd = false;
        }

        private void ClearDataGridView(DgvCtrl dgv)
        {
            if (dgv.dgv.Rows?.Count == 0 || dgv.dgv == null)
                return;

            foreach (DataGridViewRow row in dgv.dgv.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Value = 0;
                }
            }
        }

        private void splitContainer1_DoubleClick(object sender, EventArgs e)
        {
            // Re-centre the split bar
            Size size = new Size(this.Size.Width, this.Size.Height);
            int fudge = 20;

            splitContainer1.SplitterDistance = size.Width / 2 - fudge;
        }

        private void DefaultCellPainting(DgvCtrl dgv)
        {
            dgv.dgv.SuspendLayout();

            dgv.CellsBackColour = defaultBackColour;

            dgv.dgv.ResumeLayout(true);
        }

        private void PrettyCellPainting(DgvCtrl dgv)
        {
            dgv.dgv.SuspendLayout();

            foreach (DataGridViewRow row in dgv.dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value == DBNull.Value)
                    {
                        cell.Style.ForeColor = defaultBackColour;
                        cell.Style.BackColor = defaultBackColour;
                        cell.Style.SelectionForeColor = SystemColors.Highlight;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    else if ((double)cell.Value == 0)
                    {
                        cell.Style.ForeColor = defaultBackColour;
                        cell.Style.BackColor = defaultBackColour;
                        cell.Style.SelectionForeColor = SystemColors.Highlight;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    else
                    {
                        cell.Style.ForeColor = SystemColors.ControlText;
                        cell.Style.BackColor = SystemColors.Info;
                        cell.Style.SelectionForeColor = SystemColors.HighlightText;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                }

            dgv.dgv.ResumeLayout(true);
        }

        private void PrettyCellPainting(DgvCtrl dgv1, DgvCtrl dgv2)
        {
            dgv1.dgv.SuspendLayout();
            dgv2.dgv.SuspendLayout();

            // Running average
            foreach (DataGridViewRow row in dgv1.dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value == DBNull.Value)
                    {
                        cell.Style.ForeColor = defaultBackColour;
                        cell.Style.BackColor = defaultBackColour;
                        cell.Style.SelectionForeColor = SystemColors.Highlight;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    else if ((double)cell.Value == 0 && (double)dgv2.dgv.Rows[cell.RowIndex].Cells[cell.ColumnIndex].Value == 0)
                    {
                        cell.Style.ForeColor = defaultBackColour;
                        cell.Style.BackColor = defaultBackColour;
                        cell.Style.SelectionForeColor = SystemColors.Highlight;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    else
                    {
                        cell.Style.ForeColor = SystemColors.ControlText;
                        cell.Style.BackColor = SystemColors.Info;
                        cell.Style.SelectionForeColor = SystemColors.HighlightText;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                }

            // Running average count
            foreach (DataGridViewRow row in dgv2.dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value == DBNull.Value)
                    {
                        cell.Style.ForeColor = defaultBackColour;
                        cell.Style.BackColor = defaultBackColour;
                        cell.Style.SelectionForeColor = SystemColors.Highlight;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    else if ((double)cell.Value == 0)
                    {
                        cell.Style.ForeColor = defaultBackColour;
                        cell.Style.BackColor = defaultBackColour;
                        cell.Style.SelectionForeColor = SystemColors.Highlight;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    else
                    {
                        cell.Style.ForeColor = SystemColors.ControlText;
                        cell.Style.BackColor = SystemColors.Info;
                        cell.Style.SelectionForeColor = SystemColors.HighlightText;
                        cell.Style.SelectionBackColor = SystemColors.Highlight;
                    }
                }

            dgv1.dgv.ResumeLayout(true);
            dgv2.dgv.ResumeLayout(true);
        }

        private bool CheckDataBeforePushToRunAvg()
        {
            // Makes sure we have values in both new entry dgvs before pushing to the running average dgv's. For every
            // value in the new entry cell there must be a corresponding value in the new entry count cell, and vica
            // versa. Returns false if all ok

            bool result = false;

            // Loop through all cells in both new entry dgv's
            // Any exceptions will stop the data check and return true indicating no match
            try
            {
                for (int i = 0; i < dgv_NewEntry.RowCount; i++)
                {
                    for (int j = 0; j < dgv_NewEntry.ColumnCount; j++)
                    {
                        // New entry -> New entry count
                        if ((double)dgv_NewEntry.dgv.Rows[i].Cells[j].Value != 0)
                        {
                            if ((double)dgv_NewEntryCnt.dgv.Rows[i].Cells[j].Value > 0)
                            {
                                result = false; // ok
                            }
                            else
                            {
                                result = true; // not ok
                                return result;
                            }
                        }

                        // New entry count -> New entry
                        if ((double)dgv_NewEntryCnt.dgv.Rows[i].Cells[j].Value > 0)
                        {
                            if (IsNumber(dgv_NewEntry.dgv.Rows[i].Cells[j].Value.ToString()))
                            {
                                result = false; // ok
                            }
                            else
                            {
                                result = true; // not ok
                                return result;
                            }
                        }
                    }
                }
            }
            catch
            {
                return true;
            }
            return result;
        }

        private bool IsNumber(string input)
        {
            // Use regular expression to check if the input consists of digits
            return Regex.IsMatch(input, @"^[-+]?\d*\.?\d+$");
        }

        private void AutoSetFormOpeningSize()
        {
            int vPadding = 140;
            int hPadding = 80;

            // Get the working area of the display, excludes the taskbar
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;

            workingArea.Height += 8;
            workingArea.Width += 16;

            // Dgv width
            Size dgvSize = dgv_RunAvg.DgvSize;

            // Ideal form size width
            int idealFormWidth = dgvSize.Width * 2 + hPadding;

            // Ideal form size height
            int idealFormHeight = dgvSize.Height * 2 + vPadding;

            // Clamp to screen limits
            idealFormWidth = Math.Min(idealFormWidth, workingArea.Width);
            idealFormHeight = Math.Min(idealFormHeight, workingArea.Height);

            // Set opening size. The splitter will re-centre when the size changed event fires here
            this.Size = new Size(idealFormWidth, idealFormHeight);
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv_NewEntry.Focused)
            {
                // Paste
                // Set current cell to 0,0 
                dgv_NewEntry.SetDgvCurrentCell(0, 0);
                dgv_NewEntry.paste.ParseClipboardToDgv(dgv_NewEntry, Paste.Mode.PasteToCurrentCell);
                dgv_NewEntry.Refresh(RefreshMode.AverageTool);

                // Post process the dgv, hide all the 0's
                PrettyCellPainting(dgv_NewEntry);
            }

            if (dgv_NewEntryCnt.Focused)
            {
                // Paste
                // Set current cell to 0,0 
                dgv_NewEntryCnt.SetDgvCurrentCell(0, 0);
                dgv_NewEntryCnt.paste.ParseClipboardToDgv(dgv_NewEntryCnt, Paste.Mode.PasteToCurrentCell);
                dgv_NewEntryCnt.Refresh(RefreshMode.AverageTool);

                // Post process the dgv, hide all the 0's
                PrettyCellPainting(dgv_NewEntryCnt);
            }
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void btn_CopyTable_Click(object sender, EventArgs e)
        {
            Copy.CopyClipboard(dgv_RunAvg, Copy.SelectMode.SelectAll, Copy.Headers.Exclude, dgv_RunAvg.UseMyScrollBars);
        }

        private void btn_ClearRunningAverage_Click(object sender, EventArgs e)
        {
            dgv_RunAvg.ClearDataTable();
            dgv_RunAvgCnt.ClearDataTable();

            dgv_RunAvg.Refresh(RefreshMode.AverageTool);
            dgv_RunAvgCnt.Refresh(RefreshMode.AverageTool);

            // Post process the dgv, reset the cell colours, hide all the 0's
            DefaultCellPainting(dgv_RunAvg);
            DefaultCellPainting(dgv_RunAvgCnt);

            // Disable the copy button
            btn_CopyTable.Enabled = false;
        }

        private void btn_AddToAverage_Click(object sender, EventArgs e)
        {
            bool numberFormatFound = false;
            string numberFormat = "N0"; // default

            // Checks each new entry cell has a mate in the new entry count cell 
            if (CheckDataBeforePushToRunAvg())
            {
                MessageBox.Show("Please check your values, there is not a 1:1 match of average values and " +
                                "count table cells or there are negative values in the count table");
                return;
            }

            // Add new table data to running average and calc 
            for (int i = 0; i < dgv_RunAvg.RowCount; i++)
            {
                for (int j = 0; j < dgv_RunAvg.ColumnCount; j++)
                {
                    if ((double)dgv_NewEntry.ReadDt(i, j) != 0 && (double)dgv_NewEntryCnt.ReadDt(i, j) != 0)
                    {
                        // Get the displayed number format from the entry table which will be pushed to the running
                        // average table.
                        if (!numberFormatFound)
                        {
                            numberFormat = Utils.GetNumberFormat(dgv_NewEntry.dgv.Rows[i].Cells[j]);
                            numberFormatFound = true;
                        }

                        // Work out the average
                        // (Current average * Current count) + (New average * New count) / (Current count + New count)
                        double value = (dgv_RunAvg.ReadDt(i, j) * dgv_RunAvgCnt.ReadDt(i, j) +
                                        dgv_NewEntry.ReadDt(i, j) * dgv_NewEntryCnt.ReadDt(i, j)) /
                                        (dgv_RunAvgCnt.ReadDt(i, j) + dgv_NewEntryCnt.ReadDt(i, j));

                        // Add up the running counts
                        value = dgv_RunAvgCnt.ReadDt(i, j) + dgv_NewEntryCnt.ReadDt(i, j);
                        dgv_RunAvgCnt.WriteDt(i, j, value);
                    }
                }
            }

            // Get the displayed number format from the entry table which will be pushed to the running average table
            dgv_RunAvg.SetNumberFormat_v1(dgv_RunAvg.dgvNumFormat);
            dgv_RunAvg.Refresh(RefreshMode.AverageTool);
            dgv_RunAvgCnt.Refresh(RefreshMode.WidthColour);

            // Post proocess the dgv's to remove 0's
            PrettyCellPainting(dgv_RunAvg, dgv_RunAvgCnt);

            // Clear the table
            btn_ClearNewEntry_Click();

            // Enable the copy button
            btn_CopyTable.Enabled = true;
        }

        private void btn_ClearNewEntry_Click(object sender = null, EventArgs e = null)
        {
            dgv_NewEntry.ClearDataTable();
            dgv_NewEntryCnt.ClearDataTable();

            dgv_NewEntry.Refresh(RefreshMode.AverageTool);
            dgv_NewEntryCnt.Refresh(RefreshMode.AverageTool);

            // Post process the dgv, reset the cell colours
            DefaultCellPainting(dgv_NewEntry);
            DefaultCellPainting(dgv_NewEntryCnt);
        }

        private void AverageTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();

            e.Cancel = true;
        }

        private void AverageTool_Activated(object sender, EventArgs e)
        {
            // Repaint the cells every time the form is activated
            PrettyCellPainting(dgv_RunAvg, dgv_RunAvgCnt);
            PrettyCellPainting(dgv_NewEntry);
            PrettyCellPainting(dgv_NewEntryCnt);
        }

        private void Dgv_NewEntry_Scroll(object sender, ScrollEventArgs e)
        {
            dgv_NewEntryCnt.dgv.FirstDisplayedScrollingRowIndex = dgv_NewEntry.dgv.FirstDisplayedScrollingRowIndex;
            dgv_NewEntryCnt.dgv.FirstDisplayedScrollingColumnIndex = dgv_NewEntry.dgv.FirstDisplayedScrollingColumnIndex;
        }

        private void Dgv_NewEntryCnt_Scroll(object sender, ScrollEventArgs e)
        {
            dgv_NewEntry.dgv.FirstDisplayedScrollingRowIndex = dgv_NewEntryCnt.dgv.FirstDisplayedScrollingRowIndex;
            dgv_NewEntry.dgv.FirstDisplayedScrollingColumnIndex = dgv_NewEntryCnt.dgv.FirstDisplayedScrollingColumnIndex;
        }

        private void Dgv_RunAvg_Scroll(object sender, ScrollEventArgs e)
        {
            dgv_RunAvgCnt.dgv.FirstDisplayedScrollingRowIndex = dgv_RunAvg.dgv.FirstDisplayedScrollingRowIndex;
            dgv_RunAvgCnt.dgv.FirstDisplayedScrollingColumnIndex = dgv_RunAvg.dgv.FirstDisplayedScrollingColumnIndex;
        }

        private void Dgv_RunAvgCnt_Scroll(object sender, ScrollEventArgs e)
        {
            dgv_RunAvg.dgv.FirstDisplayedScrollingRowIndex = dgv_RunAvgCnt.dgv.FirstDisplayedScrollingRowIndex;
            dgv_RunAvg.dgv.FirstDisplayedScrollingColumnIndex = dgv_RunAvgCnt.dgv.FirstDisplayedScrollingColumnIndex;
        }

        private void copyWithAxisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv_RunAvg.dgv.Focused)
                Copy.CopyClipboard(dgv_RunAvg, Copy.SelectMode.SelectedCells, Copy.Headers.Include, dgv_RunAvg.UseMyScrollBars);

            if (dgv_RunAvgCnt.dgv.Focused)
                Copy.CopyClipboard(dgv_RunAvgCnt, Copy.SelectMode.SelectedCells, Copy.Headers.Include, dgv_RunAvgCnt.UseMyScrollBars);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv_RunAvg.dgv.Focused)
                Copy.CopyClipboard(dgv_RunAvg, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, dgv_RunAvg.UseMyScrollBars);

            if (dgv_RunAvgCnt.dgv.Focused)
                Copy.CopyClipboard(dgv_RunAvgCnt, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, dgv_RunAvgCnt.UseMyScrollBars);
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dgv_RunAvg.Focused || dgv_RunAvgCnt.Focused)
                pasteToolStripMenuItem.Enabled = false;
            else
                pasteToolStripMenuItem.Enabled = true;

            if (dgv_NewEntry.Focused || dgv_NewEntryCnt.Focused)
            {
                copyWithAxisToolStripMenuItem.Enabled = false;
                copyToolStripMenuItem.Enabled = false;
            }
            else
            {
                copyWithAxisToolStripMenuItem.Enabled = true;
                copyToolStripMenuItem.Enabled = true;
            }
        }

        private void cBox_LinkScrollBars_CheckedChanged(object sender, EventArgs e)
        {
            // Load / unload the scroll events
            if (cBox_LinkScrollBars.Checked)
            {
                Dgv_RunAvg.Scroll += Dgv_RunAvg_Scroll;
                Dgv_RunAvgCnt.Scroll += Dgv_RunAvgCnt_Scroll;
                Dgv_NewEntry.Scroll += Dgv_NewEntry_Scroll;
                Dgv_NewEntryCnt.Scroll += Dgv_NewEntryCnt_Scroll;
            }
            else
            {
                Dgv_RunAvg.Scroll -= Dgv_RunAvg_Scroll;
                Dgv_RunAvgCnt.Scroll -= Dgv_RunAvgCnt_Scroll;
                Dgv_NewEntry.Scroll -= Dgv_NewEntry_Scroll;
                Dgv_NewEntryCnt.Scroll -= Dgv_NewEntryCnt_Scroll;
            }
        }
    }
}
