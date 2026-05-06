using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TableEditor.DataGrid;

// Suppress the ambiguity between System.Windows.Forms.Clipboard and this class name by
// referring to the Forms API through its full path where needed.
using WinClipboard = System.Windows.Forms.Clipboard;

namespace TableEditor.Clipboard;

// Serialises the selected DataGridView cells (or the whole table) to the system clipboard
// as tab-separated text, optionally prefixed with row and column header values.
// This format is understood by Excel, LibreOffice Calc, and HP Tuners / EFI Live table views.
public class Copy
{
    public string InstanceName { get; set; }
    public string ClassName { get; set; } = "Copy";

    // Whether to copy only the currently highlighted cells or every cell in the grid.
    public enum SelectMode
    {
        SelectAll,
        SelectedCells
    }

    // Whether to prepend a header row (column names) and a header column (row names) to the output.
    public enum Headers
    {
        Include,
        Exclude
    }

    public Copy()
    {
    }

    // Builds a tab-delimited string from the selected (or all) cells and places it on the
    // system clipboard. Uses the custom scroll-bar header DGVs when useMyScrollBars is true,
    // falling back to the built-in DataGridView header cells otherwise.
    public static void CopyClipboard(DgvCtrl dgvCtrl, SelectMode selectMode, Headers headerMode, bool useMyScrollBars)
    {
        var sb = new System.Text.StringBuilder();
        var currentCellAddress = new System.Drawing.Point();
        var selectedRows = new HashSet<int>();
        var selectedColumns = new HashSet<int>();

        // Temporarily select all cells when the caller wants a full-table copy, then restore
        // the original selection afterward so the UI does not visually flicker.
        if (selectMode == SelectMode.SelectAll)
        {
            currentCellAddress = dgvCtrl.dgv.CurrentCellAddress;
            dgvCtrl.dgv.SelectAll();
        }

        // Collect the distinct row and column indexes of every selected cell.
        foreach (DataGridViewCell cell in dgvCtrl.dgv.SelectedCells)
            selectedColumns.Add(cell.ColumnIndex);

        foreach (DataGridViewCell cell in dgvCtrl.dgv.SelectedCells)
            selectedRows.Add(cell.RowIndex);

        var sortedColumns = selectedColumns.OrderBy(idx => idx).ToList();
        var sortedRows    = selectedRows.OrderBy(idx => idx).ToList();

        int firstColumnIndex = sortedColumns.First();
        int firstRowIndex    = sortedRows.First();

        // Emit the column header row: a leading tab aligns it with the data columns when
        // row headers are also present.
        if (headerMode == Headers.Include)
        {
            sb.Append('\t');

            foreach (int columnIndex in sortedColumns)
            {
                if (useMyScrollBars)
                    sb.Append(dgvCtrl.dgvHeaders.dgvColHeader.Rows[0].Cells[columnIndex].Value);
                else
                    sb.Append(dgvCtrl.dgv.Columns[columnIndex].HeaderCell.FormattedValue);

                // Suppress the trailing tab on the last column so paste targets do not see
                // a phantom empty column at the right edge.
                if (columnIndex - firstColumnIndex < sortedColumns.Count - 1)
                    sb.Append('\t');
            }

            sb.AppendLine();
        }

        // Emit each selected row, optionally prefixed with its row header value.
        foreach (DataGridViewRow row in dgvCtrl.dgv.Rows)
        {
            bool rowSelected = row.Cells.Cast<DataGridViewCell>().Any(c => c.Selected);

            if (!rowSelected)
                continue;

            if (headerMode == Headers.Include)
            {
                if (useMyScrollBars)
                    sb.Append(dgvCtrl.dgvHeaders.dgvRowHeader.Rows[row.Index].Cells[0].Value);
                else
                    sb.Append(dgvCtrl.dgv.Rows[row.Index].HeaderCell.FormattedValue);

                sb.Append('\t');
            }

            foreach (int columnIndex in sortedColumns)
            {
                DataGridViewCell cell = row.Cells[columnIndex];

                if (cell.Selected)
                {
                    sb.Append(cell.Value);

                    if (columnIndex - firstColumnIndex < sortedColumns.Count - 1)
                        sb.Append('\t');
                }
            }

            // Suppress the trailing newline on the last row for the same reason as columns above.
            if (row.Index - firstRowIndex < selectedRows.Count - 1)
                sb.AppendLine();
        }

        // Restore the pre-copy selection so the UI returns to exactly the state the user left it.
        if (selectMode == SelectMode.SelectAll)
        {
            dgvCtrl.dgv.ClearSelection();
            dgvCtrl.dgv.CurrentCell = dgvCtrl.dgv[currentCellAddress.X, currentCellAddress.Y];
        }

        // The clipboard can throw a COMException on a locked desktop (e.g. UAC prompt, RDP
        // disconnect) — re-throw with a useful message rather than swallowing silently.
        try
        {
            WinClipboard.SetText(sb.ToString());
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} at{ExceptionHelper.FormatStackTrace(ex)}");
        }
    }
}
