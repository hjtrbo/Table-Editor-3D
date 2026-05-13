﻿using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TableEditor.Clipboard;
using TableEditor.DataGrid;

namespace TableEditor.Forms;

// Running-average accumulator tool. Opens as a non-modal helper window alongside the main editor.
// Maintains two pairs of DataGridViews: running totals (RunAvg + RunAvgCnt) and the current
// paste batch (NewEntry + NewEntryCnt). On "Add to Average" the batch is folded into the totals
// using a weighted-average formula and the batch is cleared.
public partial class AverageTool : Form
{
    // ---- Public properties set by the host before calling LoadTable -------------------------

    public double[] RowHeaders { get; set; }
    public double[] ColHeaders { get; set; }
    public DgvNumFormat Format { get; set; }
    public bool ReDimReqd { get; set; }
    public bool FormLoaded { get; set; }

    // ---- Private fields --------------------------------------------------------------------

    DgvCtrl dgvRunAvg;
    DgvCtrl dgvRunAvgCnt;
    DgvCtrl dgvNewEntry;
    DgvCtrl dgvNewEntryCnt;

    // Default cell back colour — cached so it is not re-created on every paint pass.
    Color defaultBackColour = Color.White;

    // ---- Constructor -----------------------------------------------------------------------

    public AverageTool()
    {
        InitializeComponent();

        // Copy button is only enabled once there is data in the running average table.
        btn_CopyTable.Enabled = false;

        // Wrap each DataGridView in a DgvCtrl instance. Undo and custom scroll bars are
        // disabled here because this tool manages its own scroll-sync logic below.
        dgvRunAvg = new DgvCtrl
        {
            Dgv = Dgv_RunAvg,
            UndoEnabled = false,
            CopyPasteEnabled = true,
            UseMyScrollBars = false,
            ColourTheme = ColourScheme.None,
            InstanceName = "dgv_RunAvg"
        };
        dgvRunAvgCnt = new DgvCtrl
        {
            Dgv = Dgv_RunAvgCnt,
            UndoEnabled = false,
            CopyPasteEnabled = true,
            UseMyScrollBars = false,
            ColourTheme = ColourScheme.None,
            InstanceName = "dgv_RunAvgCnt"
        };
        dgvNewEntry = new DgvCtrl
        {
            Dgv = Dgv_NewEntry,
            UndoEnabled = false,
            CopyPasteEnabled = true,
            UseMyScrollBars = false,
            ColourTheme = ColourScheme.None,
            InstanceName = "dgv_NewEntry"
        };
        dgvNewEntryCnt = new DgvCtrl
        {
            Dgv = Dgv_NewEntryCnt,
            UndoEnabled = false,
            CopyPasteEnabled = true,
            UseMyScrollBars = false,
            ColourTheme = ColourScheme.None,
            InstanceName = "dgv_NewEntryCnt"
        };

        dgvRunAvg.Initialise();
        dgvRunAvgCnt.Initialise();
        dgvNewEntry.Initialise();
        dgvNewEntryCnt.Initialise();

        // All four views are read-only — the user pastes data via the context menu.
        dgvRunAvg.ReadOnly = true;
        dgvRunAvgCnt.ReadOnly = true;
        dgvNewEntry.ReadOnly = true;
        dgvNewEntryCnt.ReadOnly = true;

        dgvRunAvg.Font = new Font("Calibri", 8.25f, FontStyle.Regular);
        dgvRunAvgCnt.Font = new Font("Calibri", 8.25f, FontStyle.Regular);
        dgvNewEntry.Font = new Font("Calibri", 8.25f, FontStyle.Regular);
        dgvNewEntryCnt.Font = new Font("Calibri", 8.25f, FontStyle.Regular);

        dgvRunAvg.CellsBackColour = defaultBackColour;
        dgvRunAvgCnt.CellsBackColour = defaultBackColour;
        dgvNewEntry.CellsBackColour = defaultBackColour;
        dgvNewEntryCnt.CellsBackColour = defaultBackColour;

        // DgvHasData must be true before paste operations will work.
        dgvRunAvg.DgvHasData = true;
        dgvRunAvgCnt.DgvHasData = true;
        dgvNewEntry.DgvHasData = true;
        dgvNewEntryCnt.DgvHasData = true;

        dgvRunAvg.ScrollBars = ScrollBars.Both;
        dgvRunAvgCnt.ScrollBars = ScrollBars.Both;
        dgvNewEntry.ScrollBars = ScrollBars.Both;
        dgvNewEntryCnt.ScrollBars = ScrollBars.Both;

        // Focus follows the mouse so Ctrl+V works in whichever pane the cursor is over.
        dgvRunAvg.dgv.MouseEnter += DgvRunAvg_MouseEnter;
        dgvRunAvgCnt.dgv.MouseEnter += DgvRunAvgCnt_MouseEnter;
        dgvNewEntry.dgv.MouseEnter += DgvNewEntry_MouseEnter;
        dgvNewEntryCnt.dgv.MouseEnter += DgvNewEntryCnt_MouseEnter;
    }

    // ---- Mouse-enter focus helpers ---------------------------------------------------------

    private void DgvRunAvg_MouseEnter(object sender, EventArgs e)
    {
        dgvRunAvg.dgv.Focus();
    }

    private void DgvRunAvgCnt_MouseEnter(object sender, EventArgs e)
    {
        dgvRunAvgCnt.dgv.Focus();
    }

    private void DgvNewEntry_MouseEnter(object sender, EventArgs e)
    {
        dgvNewEntry.dgv.Focus();
    }

    private void DgvNewEntryCnt_MouseEnter(object sender, EventArgs e)
    {
        dgvNewEntryCnt.dgv.Focus();
    }

    // ---- Form events -----------------------------------------------------------------------

    private void AverageTool_Load(object sender, EventArgs e)
    {
        // Position top-left so it does not overlap the main editor on a wide screen.
        this.Location = new Point(-6, 0);

        TableBuilder(ReDimReqd);
        AutoSetFormOpeningSize();

        FormLoaded = true;
    }

    private void AverageTool_Shown(object sender, EventArgs e)
    {
        // Reserved for future use — intentionally empty.
    }

    // ---- Public API ------------------------------------------------------------------------

    // Called by the host whenever axis headers change. If the form is already open, rebuilds
    // the tables immediately; otherwise stores parameters for the Load event.
    public void LoadTable(double[] rowHeaders, double[] colHeaders, DgvNumFormat format, bool reDimReqd)
    {
        RowHeaders = rowHeaders;
        ColHeaders = colHeaders;
        Format = format;
        ReDimReqd = reDimReqd;

        if (FormLoaded)
            TableBuilder(reDimReqd);
    }

    // ---- Private helpers -------------------------------------------------------------------

    private void TableBuilder(bool reDimReqd = false)
    {
        dgvRunAvg.dgvNumFormat = Format;
        dgvRunAvgCnt.dgvNumFormat = Format;
        dgvNewEntry.dgvNumFormat = Format;
        dgvNewEntryCnt.dgvNumFormat = Format;

        // Redimension only when axis count changes — avoids expensive layout recalculation
        // on every repaint.
        if (reDimReqd)
        {
            dgvRunAvg.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
            dgvRunAvgCnt.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
            dgvNewEntry.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
            dgvNewEntryCnt.ReDimensionDataTable_v2(RowHeaders.Length, ColHeaders.Length);
        }

        dgvRunAvg.WriteRowHeaderLabels(RowHeaders);
        dgvRunAvg.WriteColHeaderLabels(ColHeaders);

        dgvRunAvgCnt.WriteRowHeaderLabels(RowHeaders);
        dgvRunAvgCnt.WriteColHeaderLabels(ColHeaders);

        dgvNewEntry.WriteRowHeaderLabels(RowHeaders);
        dgvNewEntry.WriteColHeaderLabels(ColHeaders);

        dgvNewEntryCnt.WriteRowHeaderLabels(RowHeaders);
        dgvNewEntryCnt.WriteColHeaderLabels(ColHeaders);

        ClearDataGridView(dgvRunAvg);
        ClearDataGridView(dgvRunAvgCnt);
        ClearDataGridView(dgvNewEntry);
        ClearDataGridView(dgvNewEntryCnt);

        dgvRunAvg.Refresh(RefreshMode.AverageTool);
        dgvRunAvgCnt.Refresh(RefreshMode.AverageTool);
        dgvNewEntry.Refresh(RefreshMode.AverageTool);
        dgvNewEntryCnt.Refresh(RefreshMode.AverageTool);

        ReDimReqd = false;
    }

    private void ClearDataGridView(DgvCtrl dgv)
    {
        // Guard against an empty grid to avoid a NullReferenceException.
        if (dgv.dgv.Rows?.Count == 0 || dgv.dgv == null)
            return;

        foreach (DataGridViewRow row in dgv.dgv.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = 0;
    }

    private void splitContainer1_DoubleClick(object sender, EventArgs e)
    {
        // Double-clicking the divider re-centres it so both panels are equal width.
        int fudge = 20; // accounts for border thickness and splitter width
        splitContainer1.SplitterDistance = this.Size.Width / 2 - fudge;
    }

    private void DefaultCellPainting(DgvCtrl dgv)
    {
        // Resets all cells to plain white so the grid is legible when it is empty.
        dgv.dgv.SuspendLayout();
        dgv.CellsBackColour = defaultBackColour;
        dgv.dgv.ResumeLayout(true);
    }

    private void PrettyCellPainting(DgvCtrl dgv)
    {
        // Hides zero-value cells by setting their fore and back colour to white, making the
        // non-zero data cells stand out visually.
        dgv.dgv.SuspendLayout();

        foreach (DataGridViewRow row in dgv.dgv.Rows)
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.Value == DBNull.Value || (double)cell.Value == 0)
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
        // Overload for paired running-average views: a cell in dgv1 is hidden only when both
        // it and its counterpart in dgv2 (the count grid) are zero, preventing the average
        // from being hidden when the count is non-zero or vice versa.
        dgv1.dgv.SuspendLayout();
        dgv2.dgv.SuspendLayout();

        foreach (DataGridViewRow row in dgv1.dgv.Rows)
            foreach (DataGridViewCell cell in row.Cells)
            {
                bool bothZero = cell.Value != DBNull.Value
                    && (double)cell.Value == 0
                    && (double)dgv2.dgv.Rows[cell.RowIndex].Cells[cell.ColumnIndex].Value == 0;

                if (cell.Value == DBNull.Value || bothZero)
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

        foreach (DataGridViewRow row in dgv2.dgv.Rows)
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.Value == DBNull.Value || (double)cell.Value == 0)
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

    // Returns true (i.e. data is NOT ok) if any NewEntry cell has a value but its corresponding
    // NewEntryCnt cell is missing, or vice versa. Prevents a divide-by-zero in the average calc.
    private bool CheckDataBeforePushToRunAvg()
    {
        bool result = false;

        try
        {
            for (int i = 0; i < dgvNewEntry.RowCount; i++)
            {
                for (int j = 0; j < dgvNewEntry.ColumnCount; j++)
                {
                    // A non-zero average value must be paired with a positive count.
                    if ((double)dgvNewEntry.dgv.Rows[i].Cells[j].Value != 0)
                    {
                        if ((double)dgvNewEntryCnt.dgv.Rows[i].Cells[j].Value > 0)
                            result = false;
                        else
                        {
                            result = true;
                            return result;
                        }
                    }

                    // A positive count must be paired with a parseable average value.
                    if ((double)dgvNewEntryCnt.dgv.Rows[i].Cells[j].Value > 0)
                    {
                        if (IsNumber(dgvNewEntry.dgv.Rows[i].Cells[j].Value.ToString()))
                            result = false;
                        else
                        {
                            result = true;
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
        // Accepts integers and decimals with an optional leading sign.
        return Regex.IsMatch(input, @"^[-+]?\d*\.?\d+$");
    }

    private void AutoSetFormOpeningSize()
    {
        int vPadding = 140;
        int hPadding = 80;

        // Use the working area (excludes taskbar) so the form does not open behind it.
        Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
        workingArea.Height += 8;
        workingArea.Width += 16;

        Size dgvSize = dgvRunAvg.DgvSize;

        int idealWidth = System.Math.Min(dgvSize.Width * 2 + hPadding, workingArea.Width);
        int idealHeight = System.Math.Min(dgvSize.Height * 2 + vPadding, workingArea.Height);

        this.Size = new Size(idealWidth, idealHeight);
    }

    // ---- Button and menu handlers ----------------------------------------------------------

    private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (dgvNewEntry.Focused)
        {
            dgvNewEntry.SetDgvCurrentCell(0, 0);
            dgvNewEntry.paste.ParseClipboardToDgv(dgvNewEntry, Paste.eMode.PasteToCurrentCell);
            dgvNewEntry.Refresh(RefreshMode.AverageTool);
            PrettyCellPainting(dgvNewEntry);
        }

        if (dgvNewEntryCnt.Focused)
        {
            dgvNewEntryCnt.SetDgvCurrentCell(0, 0);
            dgvNewEntryCnt.paste.ParseClipboardToDgv(dgvNewEntryCnt, Paste.eMode.PasteToCurrentCell);
            dgvNewEntryCnt.Refresh(RefreshMode.AverageTool);
            PrettyCellPainting(dgvNewEntryCnt);
        }
    }

    private void btn_Close_Click(object sender, EventArgs e)
    {
        this.Hide();
    }

    private void btn_CopyTable_Click(object sender, EventArgs e)
    {
        Copy.CopyClipboard(dgvRunAvg, Copy.SelectMode.SelectAll, Copy.Headers.Exclude, dgvRunAvg.UseMyScrollBars);
    }

    private void btn_ClearRunningAverage_Click(object sender, EventArgs e)
    {
        dgvRunAvg.ClearDataTable();
        dgvRunAvgCnt.ClearDataTable();

        dgvRunAvg.Refresh(RefreshMode.AverageTool);
        dgvRunAvgCnt.Refresh(RefreshMode.AverageTool);

        DefaultCellPainting(dgvRunAvg);
        DefaultCellPainting(dgvRunAvgCnt);

        btn_CopyTable.Enabled = false;
    }

    private void btn_AddToAverage_Click(object sender, EventArgs e)
    {
        bool numberFormatFound = false;
        string numberFormat = "N0"; // default — overwritten below if a non-zero cell is found
        double value;

        if (CheckDataBeforePushToRunAvg())
        {
            MessageBox.Show("Please check your values, there is not a 1:1 match of average values and " +
                            "count table cells or there are negative values in the count table");
            return;
        }

        for (int i = 0; i < dgvRunAvg.RowCount; i++)
        {
            for (int j = 0; j < dgvRunAvg.ColumnCount; j++)
            {
                if ((double)dgvNewEntry.ReadDt(i, j) != 0 && (double)dgvNewEntryCnt.ReadDt(i, j) != 0)
                {
                    // Capture the displayed format from the first non-zero entry cell so the
                    // running-average table matches the precision of the source data.
                    if (!numberFormatFound)
                    {
                        numberFormat = NumberFormatter.GetNumberFormat(dgvNewEntry.dgv.Rows[i].Cells[j]);
                        numberFormatFound = true;
                    }

                    // Weighted average: (oldAvg * oldCount + newAvg * newCount) / (oldCount + newCount)
                    value = (dgvRunAvg.ReadDt(i, j)    * dgvRunAvgCnt.ReadDt(i, j) +
                             dgvNewEntry.ReadDt(i, j)  * dgvNewEntryCnt.ReadDt(i, j)) /
                            (dgvRunAvgCnt.ReadDt(i, j) + dgvNewEntryCnt.ReadDt(i, j));
                    dgvRunAvg.WriteDt(i, j, value);

                    value = dgvRunAvgCnt.ReadDt(i, j) + dgvNewEntryCnt.ReadDt(i, j);
                    dgvRunAvgCnt.WriteDt(i, j, value);
                }
            }
        }

        dgvRunAvg.SetNumberFormat_v1(dgvRunAvg.dgvNumFormat);
        dgvRunAvg.Refresh(RefreshMode.AverageTool);
        dgvRunAvgCnt.Refresh(RefreshMode.WidthColour);

        PrettyCellPainting(dgvRunAvg, dgvRunAvgCnt);

        // Clear the batch tables ready for the next data log paste.
        btn_ClearNewEntry_Click();

        btn_CopyTable.Enabled = true;
    }

    private void btn_ClearNewEntry_Click(object sender = null, EventArgs e = null)
    {
        dgvNewEntry.ClearDataTable();
        dgvNewEntryCnt.ClearDataTable();

        dgvNewEntry.Refresh(RefreshMode.AverageTool);
        dgvNewEntryCnt.Refresh(RefreshMode.AverageTool);

        DefaultCellPainting(dgvNewEntry);
        DefaultCellPainting(dgvNewEntryCnt);
    }

    private void AverageTool_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Hide instead of close so the accumulated running-average data is preserved for the
        // session. The form is only truly destroyed when the host application exits.
        this.Hide();
        e.Cancel = true;
    }

    private void AverageTool_Activated(object sender, EventArgs e)
    {
        // Repaint on activation to reflect any external colour-scheme changes.
        PrettyCellPainting(dgvRunAvg, dgvRunAvgCnt);
        PrettyCellPainting(dgvNewEntry);
        PrettyCellPainting(dgvNewEntryCnt);
    }

    // ---- Scroll synchronisation ------------------------------------------------------------
    // Keep paired grids in sync so the user always sees the same row/column position in both.

    private void Dgv_NewEntry_Scroll(object sender, ScrollEventArgs e)
    {
        dgvNewEntryCnt.dgv.FirstDisplayedScrollingRowIndex = dgvNewEntry.dgv.FirstDisplayedScrollingRowIndex;
        dgvNewEntryCnt.dgv.FirstDisplayedScrollingColumnIndex = dgvNewEntry.dgv.FirstDisplayedScrollingColumnIndex;
    }

    private void Dgv_NewEntryCnt_Scroll(object sender, ScrollEventArgs e)
    {
        dgvNewEntry.dgv.FirstDisplayedScrollingRowIndex = dgvNewEntryCnt.dgv.FirstDisplayedScrollingRowIndex;
        dgvNewEntry.dgv.FirstDisplayedScrollingColumnIndex = dgvNewEntryCnt.dgv.FirstDisplayedScrollingColumnIndex;
    }

    private void Dgv_RunAvg_Scroll(object sender, ScrollEventArgs e)
    {
        dgvRunAvgCnt.dgv.FirstDisplayedScrollingRowIndex = dgvRunAvg.dgv.FirstDisplayedScrollingRowIndex;
        dgvRunAvgCnt.dgv.FirstDisplayedScrollingColumnIndex = dgvRunAvg.dgv.FirstDisplayedScrollingColumnIndex;
    }

    private void Dgv_RunAvgCnt_Scroll(object sender, ScrollEventArgs e)
    {
        dgvRunAvg.dgv.FirstDisplayedScrollingRowIndex = dgvRunAvgCnt.dgv.FirstDisplayedScrollingRowIndex;
        dgvRunAvg.dgv.FirstDisplayedScrollingColumnIndex = dgvRunAvgCnt.dgv.FirstDisplayedScrollingColumnIndex;
    }

    // ---- Context menu handlers -------------------------------------------------------------

    private void copyWithAxisToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (dgvRunAvg.dgv.Focused)
            Copy.CopyClipboard(dgvRunAvg, Copy.SelectMode.SelectedCells, Copy.Headers.Include, dgvRunAvg.UseMyScrollBars);

        if (dgvRunAvgCnt.dgv.Focused)
            Copy.CopyClipboard(dgvRunAvgCnt, Copy.SelectMode.SelectedCells, Copy.Headers.Include, dgvRunAvgCnt.UseMyScrollBars);
    }

    private void copyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (dgvRunAvg.dgv.Focused)
            Copy.CopyClipboard(dgvRunAvg, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, dgvRunAvg.UseMyScrollBars);

        if (dgvRunAvgCnt.dgv.Focused)
            Copy.CopyClipboard(dgvRunAvgCnt, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, dgvRunAvgCnt.UseMyScrollBars);
    }

    private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Paste is only available in the new-entry panes; copy is only available in the
        // running-average panes.
        if (dgvRunAvg.Focused || dgvRunAvgCnt.Focused)
            pasteToolStripMenuItem.Enabled = false;
        else
            pasteToolStripMenuItem.Enabled = true;

        if (dgvNewEntry.Focused || dgvNewEntryCnt.Focused)
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
        // Load or unload the scroll-sync handlers so the user can opt out when independently
        // scrolling two different regions of a large table.
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
