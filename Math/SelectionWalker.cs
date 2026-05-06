using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using TableEditor.DataGrid;

namespace TableEditor.Math;

// Groups the selected cells in a DataGridView by column (Vertical) or row (Horizontal),
// then invokes a callback once per group with the DataTable, primary index (column or row),
// and the inclusive min/max secondary index within that group. This lets Interpolate and
// Smooth share identical scaffolding while keeping their math separate.
//
// Walk does nothing when mode == All; callers are responsible for issuing two separate
// Walk calls (Vertical then Horizontal, or vice-versa) so each can apply its own ordering.
public static class SelectionWalker
{
    // Callback signature: (dataTable, primaryIndex, minSecondaryIndex, maxSecondaryIndex).
    // primaryIndex is the column index for Vertical mode, or the row index for Horizontal mode.
    public delegate void GroupAction(
        System.Data.DataTable dataTable,
        int primaryIndex,
        int minSecondaryIndex,
        int maxSecondaryIndex);

    // Walks selectedCells grouped by the axis determined by mode and calls action for each group.
    // Returns false when the selection is empty, mode is All, or no group contains two or more
    // cells in the same column/row (the minimum needed for any meaningful operation).
    public static bool Walk(
        DataGridView dgv,
        DataGridViewSelectedCellCollection selectedCells,
        WalkMode mode,
        GroupAction action)
    {
        if (selectedCells.Count == 0 || mode == WalkMode.All)
            return false;

        var dt = (System.Data.DataTable)dgv.DataSource;

        // Build (primaryIndex, secondaryIndex) pairs according to direction.
        // Vertical  → primary = column, secondary = row
        // Horizontal → primary = row,    secondary = column
        var rawIndexes = new List<(int primary, int secondary)>();

        foreach (DataGridViewCell cell in selectedCells)
        {
            if (mode == WalkMode.Vertical)
                rawIndexes.Add((cell.ColumnIndex, cell.RowIndex));
            else
                rawIndexes.Add((cell.RowIndex, cell.ColumnIndex));
        }

        rawIndexes.Sort();

        // Identify primary indexes that appear more than once — only those have a range
        // to act on (a single selected cell in a column/row yields nothing useful).
        var seen = new HashSet<int>();
        var validPrimaries = new HashSet<int>();

        foreach (var (primary, _) in rawIndexes)
        {
            if (!seen.Add(primary))
                validPrimaries.Add(primary);
        }

        if (validPrimaries.Count == 0)
            return false;

        // For each valid primary index, compute the inclusive secondary range and invoke action.
        foreach (int primary in validPrimaries)
        {
            int minSecondary = int.MaxValue;
            int maxSecondary = int.MinValue;

            foreach (var (p, s) in rawIndexes)
            {
                if (p != primary)
                    continue;

                if (s < minSecondary) minSecondary = s;
                if (s > maxSecondary) maxSecondary = s;
            }

            action(dt, primary, minSecondary, maxSecondary);
        }

        return true;
    }
}
