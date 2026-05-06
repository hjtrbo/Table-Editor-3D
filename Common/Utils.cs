using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TableEditor;

public static class Utils
{
    // Returns true when the string represents a valid integer or decimal number,
    // including an optional leading sign. Does not accept scientific notation.
    public static bool IsNumber(string input)
    {
        return Regex.IsMatch(input, @"^[-+]?\d*\.?\d+$");
    }

    // Compares two DataTables cell-by-cell after verifying they have the same dimensions.
    // Uses Equals() so that DBNull, string, and numeric types are each handled correctly.
    public static bool AreDataTablesEqual(DataTable dt1, DataTable dt2)
    {
        if (dt1.Rows.Count != dt2.Rows.Count || dt1.Columns.Count != dt2.Columns.Count)
            return false;

        for (int i = 0; i < dt1.Rows.Count; i++)
        {
            for (int j = 0; j < dt1.Columns.Count; j++)
            {
                if (!Equals(dt1.Rows[i][j], dt2.Rows[i][j]))
                    return false;
            }
        }

        return true;
    }

    // Deep-copies a DataTable, preserving column schema and all row data.
    // Returns null when the source table is null.
    public static DataTable CopyDataTable(DataTable dtIn)
    {
        if (dtIn == null)
            return null;

        DataTable dtOut = dtIn.Clone();

        foreach (DataRow row in dtIn.Rows)
            dtOut.ImportRow(row);

        return dtOut;
    }

    // Returns a new array that is the mathematical transpose of the input:
    // element [i, j] in the source becomes [j, i] in the result.
    public static double[,] Transpose(double[,] array)
    {
        int rows = array.GetLength(0);
        int cols = array.GetLength(1);
        double[,] transposed = new double[cols, rows];

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                transposed[j, i] = array[i, j];

        return transposed;
    }

    // Flattens a DataTable (whose cells are all double) into a 2-D array with the same
    // row/column layout. Returns an empty [0,0] array for a null input.
    public static double[,] CopyDataTableToArray(DataTable dt)
    {
        if (dt == null)
            return new double[0, 0];

        double[,] copy = new double[dt.Rows.Count, dt.Columns.Count];

        for (int i = 0; i < dt.Rows.Count; i++)
            for (int j = 0; j < dt.Columns.Count; j++)
                copy[i, j] = (double)dt.Rows[i][j];

        return copy;
    }

    // Compares two double[,] arrays element-by-element after a dimension check.
    // The original source had an empty catch{} that caused unequal arrays to be reported as
    // equal whenever an index was out of range — that bug is removed here.
    public static bool CompareDataTableToArray(double[,] dt1, double[,] dt2)
    {
        if (dt1.GetLength(0) != dt2.GetLength(0) || dt1.GetLength(1) != dt2.GetLength(1))
            return false;

        for (int i = 0; i < dt1.GetLength(0); i++)
            for (int j = 0; j < dt1.GetLength(1); j++)
                if (dt1[i, j] != dt2[i, j])
                    return false;

        return true;
    }

    // Compares a DataTable against a double[,] snapshot after a dimension check.
    // Used to detect whether the live grid data has drifted from a saved reference copy.
    public static bool CompareDataTableToArray(DataTable dt1, double[,] dt2)
    {
        if (dt1.Rows.Count != dt2.GetLength(0) || dt1.Columns.Count != dt2.GetLength(1))
            return false;

        for (int i = 0; i < dt1.Rows.Count; i++)
            for (int j = 0; j < dt1.Columns.Count; j++)
                if ((double)dt1.Rows[i][j] != dt2[i, j])
                    return false;

        return true;
    }
}

// Pulls a short location string out of an exception stack trace so that catch-block log
// entries don't need to repeat the same fragile substring logic everywhere.
public static class ExceptionHelper
{
    public static string FormatStackTrace(Exception ex)
    {
        string st = ex.StackTrace ?? "";
        int idx = st.LastIndexOf(":line");
        return idx >= 0 ? st.Substring(idx) : st;
    }
}
