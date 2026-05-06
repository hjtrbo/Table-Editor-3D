using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TableEditor;
using TableEditor.DataGrid;
using Key      = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;

namespace TableEditor.Layout;

// Manages the two "frozen header" DataGridViews (row header and column header) that float on top of
// the main DGV to simulate the locked-row / locked-column behaviour found in spreadsheet apps.
//
// The row-header DGV scrolls vertically in sync with the main DGV.
// The column-header DGV scrolls horizontally in sync with the main DGV.
// A small blanking panel sits in the top-left corner covering the intersection of the two headers.
public class DgvHeadersCtrl
{
    // -------------------------Properties -----------------------------------------------------------------------------------------

    public string ClassName    { get; set; } = "DgvHdrs";
    public string InstanceName { get; private set; }
    public bool   DebugHeaders { get; set; }

    // Receiving end of the scroll pipeline: ScrollBarCtrl assigns ScrollEventArgs then sets one of the
    // bool setters below. The setter immediately calls the appropriate scroll handler, which keeps the
    // header DGVs in sync without needing a separate event subscription.
    public ScrollEventArgs ScrollEventArgs { get; set; }

    public bool hScrollInProgress { set { hScroll_Scrolled(dgvColHeader, ScrollEventArgs); } }
    public bool vScrollInProgress { set { vScroll_Scrolled(dgvRowHeader, ScrollEventArgs); } }

    // Convenience properties so callers don't have to reach into dgvCtrl.dgv directly.
    public int RowHeight { get { return dgvCtrl.dgv.Rows[0].Height + 1; } }
    public int ColWidth  { get { return dgvCtrl.dgv.RowHeadersWidth - 1; } }

    // -------------------------Variables ------------------------------------------------------------------------------------------

    DgvCtrl dgvCtrl;

    // Public so that ScrollBarCtrl can read the current header DGV locations if needed.
    public DataGridView dgvRowHeader;
    public DataGridView dgvColHeader;

    Panel         blankingPanel;
    HScrollBar    hScroll;
    VScrollBar    vScroll;
    SplitContainer splitContainer;

    // Previously loaded header arrays; reserved for change-detection if needed in future.
    double[] rowHeaderValuesPrev = new double[0];
    double[] colHeaderValuesPrev = new double[0];

    // Default header cell colours. Beige background provides visual contrast from the editable DGV.
    Color foreColour = Color.Black;
    Color backColour = Color.Beige;

    // Tracks which rows / columns the user has clicked so that a second click de-selects them.
    List<int> selectedRows = new List<int>();
    List<int> selectedCols = new List<int>();

    // Drives the DgvStyleOverrides overload that handles rows, columns, or both.
    enum Target { Both, Rows, Columns }

    // -------------------------Constructor ----------------------------------------------------------------------------------------

    public DgvHeadersCtrl(DgvCtrl dgvCtrl, string instanceName)
    {
        this.dgvCtrl       = dgvCtrl;
        // Pull control references from LayoutControls so this class always uses the same bundle
        // as ScrollBarCtrl — avoids drift if controls are later replaced.
        this.dgvRowHeader  = dgvCtrl.DgvHeaderCntrls.RowHeader;
        this.dgvColHeader  = dgvCtrl.DgvHeaderCntrls.ColHeader;
        this.blankingPanel = dgvCtrl.DgvHeaderCntrls.BlankingPanel;
        this.hScroll       = dgvCtrl.DgvHeaderCntrls.HScrollBar;
        this.vScroll       = dgvCtrl.DgvHeaderCntrls.VScrollBar;
        this.splitContainer = dgvCtrl.DgvHeaderCntrls.SplitContainer;

        InstanceName = instanceName;

        // Start hidden; LoadHeaders() will show them once data is available and dimensions are known.
        HideHeaders();

        // Subscribe to data and size events so headers update automatically when the table changes.
        dgvCtrl.myEvents.DgvSizeChanged_Intermittent += MyEvents_SizeChanged_Intermittent;
        dgvCtrl.myEvents.DgvDataChangedToHeaders     += MyEvents_DgvDataChanged_ToHeaders_Event;
        blankingPanel.Click                          += BlankingPanel_Click;
        dgvCtrl.dgv.Move                             += Dgv_Move;
        dgvRowHeader.CellClick                       += RowHeader_CellClick;
        dgvColHeader.CellClick                       += ColHeader_CellClick;
        splitContainer.Resize                        += SplitContainer_Resize;
    }

    // -------------------------Functions ------------------------------------------------------------------------------------------

    // Placeholder — the Move event is wired to allow future synchronisation logic without needing a
    // new event subscription. Currently empty because position sync happens through scroll events.
    [Obsolete("Empty stub; not yet implemented")]
    private void Dgv_Move(object sender, EventArgs e)
    {
    }

    // Shifts the appropriate header DGV by delta pixels to keep it aligned with the main DGV after a
    // scroll or resize. vert=true moves the row header (Y axis); false moves the column header (X axis).
    public void PushNewHeaderLocation(bool vert, int delta)
    {
        if (vert)
        {
            Point loc = dgvRowHeader.Location;
            loc.Y += delta;
            dgvRowHeader.Location = loc;
        }
        else
        {
            Point loc = dgvColHeader.Location;
            loc.X += delta;
            dgvColHeader.Location = loc;
        }
    }

    // Snaps the row header and the main DGV back to the vertical home position.
    // Called by ScrollBarCtrl when it detects that the child DGV has been pushed to Y >= 0.
    public void ResetRowHeaderPosition()
    {
        Point loc = dgvCtrl.dgv.Location;
        loc.Y = 0;
        dgvCtrl.dgv.Location = loc;
        // Row header sits just below the column-header row.
        dgvRowHeader.Location = new Point(0, RowHeight);
        vScroll.Value = 0;
    }

    // Snaps the column header and the main DGV back to the horizontal home position.
    public void ResetColHeaderPosition()
    {
        Point loc = dgvCtrl.dgv.Location;
        loc.X = 0;
        dgvCtrl.dgv.Location = loc;
        // Column header sits just to the right of the row-header column.
        dgvColHeader.Location = new Point(ColWidth, 0);
        hScroll.Value = 0;
    }

    // -------------------------Event handlers -------------------------------------------------------------------------------------

    private void MyEvents_DgvDataChanged_ToHeaders_Event(object sender, DgvData e)
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_ScrollBarDataChanged_Event()");

        // New data pushed to headers — reload labels, reposition, and re-style.
        LoadHeaders(e);
    }

    private void MyEvents_SizeChanged_Intermittent(object sender, DgvEvents.SizeEventArgs e)
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_SizeChanged_Intermittent()");

        // A 1×1 table means the DGV is effectively empty — hide headers rather than showing a single cell.
        if (dgvCtrl.dgv.Rows.Count == 1 && dgvCtrl.dgv.Columns.Count == 1)
        {
            HideHeaders();
            return;
        }

        LoadHeaders(e);
    }

    private void RowHeader_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - RowHeader_CellClick()");

        // Without Ctrl held, clicking a row header clears everything else first.
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            dgvCtrl.dgv.ClearSelection();

        // Second click on an already-selected row deselects it (toggle behaviour).
        if (selectedRows.Contains(e.RowIndex))
        {
            for (int i = 0; i < dgvCtrl.dgv.Columns.Count; i++)
                dgvCtrl.dgv.Rows[e.RowIndex].Cells[i].Selected = false;

            selectedRows.Remove(e.RowIndex);
            return;
        }

        for (int i = 0; i < dgvCtrl.dgv.Columns.Count; i++)
            dgvCtrl.dgv.Rows[e.RowIndex].Cells[i].Selected = true;

        selectedRows.Add(e.RowIndex);
    }

    private void ColHeader_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - ColHeader_CellClick()");

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            dgvCtrl.dgv.ClearSelection();

        // Toggle: deselect the column if already selected.
        if (selectedCols.Contains(e.ColumnIndex))
        {
            selectedCols.Remove(e.ColumnIndex);
            return;
        }

        for (int i = 0; i < dgvCtrl.dgv.Rows.Count; i++)
            dgvCtrl.dgv.Rows[i].Cells[e.ColumnIndex].Selected = true;

        selectedCols.Add(e.ColumnIndex);
    }

    private void BlankingPanel_Click(object sender, EventArgs e)
    {
        // Toggle between select-all and clear-all so the blanking panel acts like the corner cell in Excel.
        if (dgvCtrl.dgv.AreAllCellsSelected(true))
            dgvCtrl.dgv.ClearSelection();
        else
            dgvCtrl.dgv.SelectAll();
    }

    private void SplitContainer_Resize(object sender, EventArgs e)
    {
        // Reposition floating headers to stay aligned with the main DGV after a container resize.
        // The main DGV's own Location.Y/X offset is included so the header tracks correctly when
        // the table is partially scrolled at the moment of resize.
        dgvRowHeader.Location = new Point(0,        dgvCtrl.dgv.Rows[0].Height + 1 + dgvCtrl.dgv.Location.Y);
        dgvColHeader.Location = new Point(dgvCtrl.dgv.RowHeadersWidth - 1 + dgvCtrl.dgv.Location.X, 0);
    }

    // -------------------------Header layout --------------------------------------------------------------------------------------

    private void HideHeaders()
    {
        dgvRowHeader.Hide();
        dgvColHeader.Hide();
        blankingPanel.Hide();

        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - HideHeaders()");
    }

    private void UnhideHeaders()
    {
        dgvRowHeader.Show();
        dgvColHeader.Show();
        blankingPanel.Show();

        // BringToFront ensures the floating headers paint over the main DGV edges and scroll bars.
        dgvRowHeader.BringToFront();
        dgvColHeader.BringToFront();
        hScroll.BringToFront();
        vScroll.BringToFront();
        blankingPanel.BringToFront();

        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - UnhideHeaders()");
    }

    // Overload for full data reload (new row/column labels supplied).
    private void LoadHeaders(DgvData e)
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - LoadNewHeaders(PasteEventArgs)");

        // All four arrays must be present; partial data would leave headers in an inconsistent state.
        if (e.RowHeaders == null || e.ColHeaders == null || e.RowHeadersText == null || e.ColHeadersText == null)
        {
            if (DebugHeaders)
                Console.WriteLine($"{InstanceName} - {ClassName} - Returned from LoadHeaders(). Null checks failed");
            return;
        }

        ResetRowHeaderPosition();
        ResetColHeaderPosition();

        WriteScrollBarRowHeaders(e.RowHeadersText);
        WriteScrollBarColHeaders(e.ColHeadersText);

        DgvStyleOverrides(dgvRowHeader, Target.Rows);
        DgvStyleOverrides(dgvColHeader, Target.Columns);
        PanelStyleOverrides(blankingPanel);

        UnhideHeaders();
    }

    // Overload for size-only change (labels unchanged but dimensions may have shifted).
    private void LoadHeaders(DgvEvents.SizeEventArgs e)
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - LoadNewHeaders(MyEvent SizeEventArgs)");

        ResetRowHeaderPosition();
        ResetColHeaderPosition();

        DgvStyleOverrides(dgvRowHeader, Target.Rows);
        DgvStyleOverrides(dgvColHeader, Target.Columns);
        PanelStyleOverrides(blankingPanel);

        UnhideHeaders();
    }

    // -------------------------Scroll sync ----------------------------------------------------------------------------------------

    // Translates a horizontal scroll event into a pixel-level pan of the column-header DGV.
    // Called via the hScrollInProgress setter so the call path is: ScrollBarCtrl → setter → here.
    private void hScroll_Scrolled(DataGridView hdrDgv, ScrollEventArgs e)
    {
        Point location = hdrDgv.Location;
        location.X -= e.NewValue - e.OldValue;
        hdrDgv.Location = location;
    }

    private void vScroll_Scrolled(DataGridView hdrDgv, ScrollEventArgs e)
    {
        Point location = hdrDgv.Location;
        location.Y -= e.NewValue - e.OldValue;
        hdrDgv.Location = location;
    }

    // -------------------------Style helpers --------------------------------------------------------------------------------------

    // Applies visual overrides so the header DGVs look like extensions of the main DGV (matching font,
    // grid colour, border style, row height, and column widths) while suppressing interactive features
    // that would be confusing in a read-only header (own scroll bars, user resize, user add-row, etc.).
    private void DgvStyleOverrides(DataGridView hdrDgv, Target target)
    {
        // Exit early if the DGV has no data — styling an empty DGV throws index-out-of-range.
        if (hdrDgv.Rows.Count == 0 || hdrDgv.Columns.Count == 0)
            return;

        // Match row heights to the main DGV so the header cells align perfectly with data cells.
        foreach (DataGridViewRow row in hdrDgv.Rows)
            row.Height = dgvCtrl.dgv.Rows[0].Height;

        // Cell appearance — inherit font from main DGV, centre-align text, apply fixed colours so the
        // header can never be confused with a selected cell in the main table.
        hdrDgv.DefaultCellStyle.Font      = dgvCtrl.dgv.DefaultCellStyle.Font;
        hdrDgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        foreach (DataGridViewRow row in hdrDgv.Rows)
        {
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style.ForeColor          = foreColour;
                cell.Style.BackColor          = backColour;
                cell.Style.SelectionForeColor = foreColour;  // Prevent blue flicker on accidental click.
                cell.Style.SelectionBackColor = backColour;
            }
        }

        // Control-level appearance — match the main DGV border and grid colour for visual consistency.
        hdrDgv.BackgroundColor          = backColour;
        hdrDgv.GridColor                = dgvCtrl.dgv.GridColor;
        hdrDgv.BorderStyle              = dgvCtrl.dgv.BorderStyle;
        hdrDgv.ScrollBars               = ScrollBars.None;  // We manage scrolling manually.
        hdrDgv.AutoSizeColumnsMode      = DataGridViewAutoSizeColumnsMode.None;
        hdrDgv.ColumnHeadersVisible     = false;
        hdrDgv.RowHeadersVisible        = false;

        // Lock down all user-editing capability.
        hdrDgv.AllowUserToAddRows       = false;
        hdrDgv.AllowUserToDeleteRows    = false;
        hdrDgv.AllowUserToOrderColumns  = false;
        hdrDgv.AllowUserToResizeColumns = false;
        hdrDgv.AllowUserToResizeRows    = false;
        hdrDgv.ReadOnly                 = true;

        // Axis-specific sizing and positioning.
        if (target == Target.Rows || target == Target.Both)
        {
            // Row header runs vertically alongside the main DGV, offset downward by one row height to
            // leave room for the column header at the top.
            hdrDgv.Columns[0].Width = dgvCtrl.dgv.RowHeadersWidth;
            hdrDgv.Width            = dgvCtrl.dgv.RowHeadersWidth + 1;
            hdrDgv.Height           = dgvCtrl.dgv.Height - dgvCtrl.dgv.Rows[0].Height - 1;
            hdrDgv.Location         = new Point(0, dgvCtrl.dgv.Rows[0].Height + 1);
            hdrDgv.DefaultCellStyle.Format = dgvCtrl.dgv.RowHeadersDefaultCellStyle.Format;
        }

        if (target == Target.Columns || target == Target.Both)
        {
            // Column header runs horizontally across the top of the main DGV, offset rightward by the
            // row-header width to leave room for the blanking panel in the corner.
            hdrDgv.Width    = dgvCtrl.dgv.Width - dgvCtrl.dgv.RowHeadersWidth;
            hdrDgv.Height   = dgvCtrl.dgv.Rows[0].Height + 3;
            hdrDgv.Location = new Point(dgvCtrl.dgv.RowHeadersWidth - 1, 0);
            hdrDgv.DefaultCellStyle.Format = dgvCtrl.dgv.ColumnHeadersDefaultCellStyle.Format;

            // Match each column's width to the main DGV so values line up under their headers.
            foreach (DataGridViewColumn col in hdrDgv.Columns)
                col.Width = dgvCtrl.dgv.Columns[0].Width;
        }

        hdrDgv.ClearSelection();
    }

    // Sizes and colours the blanking panel that covers the top-left intersection corner.
    private void PanelStyleOverrides(Panel blankingPlate)
    {
        blankingPlate.Location    = new Point(0, 0);
        blankingPlate.Size        = new Size(dgvCtrl.dgv.RowHeadersWidth + 1, dgvCtrl.dgv.ColumnHeadersHeight + 1);
        blankingPlate.BackColor   = backColour;
        blankingPlate.BorderStyle = BorderStyle.FixedSingle;
    }

    // -------------------------Header sync ----------------------------------------------------------------------------------------

    // Re-applies the main DGV's current layout to the floating column-header DGV and blanking panel.
    // Column widths, the DGV's X position, the DGV's total width, and the blanking panel width are
    // all derived from dgv.RowHeadersWidth — which is only correct after SetCellWidths() has measured
    // the actual header text. Calling this after SetCellWidths() ensures headers with wide text labels
    // (e.g. "1st Gear") don't leave the column header DGV positioned at a stale narrower offset.
    public void SyncColHeaderWidths()
    {
        if (dgvColHeader.Rows.Count == 0 || dgvColHeader.Columns.Count == 0)
            return;

        int colWidth    = dgvCtrl.dgv.Columns.Count > 0 ? dgvCtrl.dgv.Columns[0].Width : 0;
        int rowHdrWidth = dgvCtrl.dgv.RowHeadersWidth;
        if (colWidth <= 0 || rowHdrWidth <= 0)
            return;

        foreach (DataGridViewColumn col in dgvColHeader.Columns)
            col.Width = colWidth;

        dgvColHeader.Location = new Point(rowHdrWidth - 1, 0);
        dgvColHeader.Width    = dgvCtrl.dgv.Width - rowHdrWidth;

        blankingPanel.Size = new Size(rowHdrWidth + 1, dgvCtrl.dgv.ColumnHeadersHeight + 1);
    }

    // Re-applies the main DGV's current row-header width and row height to the floating row-header DGV.
    // Width syncs the single column and the DGV's total width. Height syncs every row's pixel height.
    // Called after SetCellWidths() because WriteScrollBarRowHeaders (called explicitly by toolbar buttons
    // after WriteToDataGridView) recreates all rows at the WinForms default height, not at the 20px that
    // DgvStyleOverrides establishes — causing progressive vertical drift without this correction.
    public void SyncRowHeaderWidth()
    {
        if (dgvRowHeader.Rows.Count == 0 || dgvRowHeader.Columns.Count == 0)
            return;

        int rowHdrWidth = dgvCtrl.dgv.RowHeadersWidth;
        if (rowHdrWidth <= 0)
            return;

        dgvRowHeader.Columns[0].Width = rowHdrWidth;
        dgvRowHeader.Width            = rowHdrWidth + 1;

        int rowHeight = dgvCtrl.dgv.Rows.Count > 0 ? dgvCtrl.dgv.Rows[0].Height : 0;
        if (rowHeight > 0)
            foreach (DataGridViewRow row in dgvRowHeader.Rows)
                row.Height = rowHeight;
    }

    // -------------------------Header write ---------------------------------------------------------------------------------------

    // Writes an array of values into the row-header DGV (one value per row, single column).
    // Generic so callers can pass string[], double[], int[], etc. without conversion overhead.
    public void WriteScrollBarRowHeaders<T>(T[] values)
    {
        if (values.Length == 0)
            return;

        // None mode is required before adding columns to avoid the WinForms FillWeight exception that
        // fires when column widths are set before AutoSizeColumnsMode is None.
        dgvRowHeader.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        // Fallback: if the caller passed null or an empty array, synthesise dummy integer labels from
        // the current row count so the header is never completely blank.
        if (values == null || values.Length == 0)
        {
            if (dgvCtrl.dgv.Rows.Count == 0)
                throw new Exception("WriteScrollBarRowHeaders called with no data and empty DGV.");

            values = new T[dgvCtrl.dgv.Rows.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = (T)Convert.ChangeType(i, typeof(T));
        }

        dgvRowHeader.Columns.Clear();
        dgvRowHeader.Rows.Clear();

        var column = new DataGridViewTextBoxColumn { HeaderText = "Values" };
        dgvRowHeader.Columns.Add(column);

        for (int i = 0; i < values.Length; i++)
        {
            dgvRowHeader.Rows.Add(values[i]);
            dgvRowHeader.Rows[i].Cells[0].Value = values[i];
        }
    }

    // Writes an array of values into the column-header DGV (single row, one value per column).
    public void WriteScrollBarColHeaders<T>(T[] values)
    {
        if (values.Length == 0)
            return;

        dgvColHeader.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        if (values == null)
        {
            if (dgvCtrl.dgv.Columns.Count == 0)
                throw new Exception("WriteScrollBarColHeaders called with no data and empty DGV.");

            values = new T[dgvCtrl.dgv.Columns.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = (T)Convert.ChangeType(i, typeof(T));
        }

        dgvColHeader.Columns.Clear();
        dgvColHeader.Rows.Clear();

        for (int i = 0; i < values.Length; i++)
            dgvColHeader.Columns.Add($"Column{i + 1}", $"Column{i + 1}");

        dgvColHeader.Rows.Add();

        for (int i = 0; i < values.Length; i++)
            dgvColHeader.Rows[0].Cells[i].Value = values[i];
    }

    // -------------------------Header read ----------------------------------------------------------------------------------------

    // Returns the current row-header labels as strings, or null when the DGV has no data.
    public string[] ReadRowHeaders()
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadRowHeaders()");

        if (!dgvCtrl.DgvHasData) return null;

        string[] rowLabels = new string[dgvRowHeader.RowCount];
        for (int i = 0; i < dgvRowHeader.RowCount; i++)
            rowLabels[i] = (string)dgvRowHeader.Rows[i].Cells[0].Value;

        return rowLabels;
    }

    public string[] ReadColHeaders()
    {
        if (DebugHeaders)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadColHeaders()");

        if (!dgvCtrl.DgvHasData) return null;

        string[] colLabels = new string[dgvColHeader.ColumnCount];
        for (int i = 0; i < dgvColHeader.ColumnCount; i++)
            colLabels[i] = (string)dgvColHeader.Rows[0].Cells[i].Value;

        return colLabels;
    }
}
