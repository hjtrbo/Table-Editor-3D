namespace TableEditor.DataGrid;

// Tracks the current number formats for row headers, column headers, and cells.
// Call Update() after setting format properties to determine which part of the
// grid needs reformatting (the Target property).
public class DgvNumFormat
{
    public string InstanceName { get; set; }
    public bool CelLckOut { get; set; }
    public string RowHdrFormat { get; set; }
    public string ColHdrFormat { get; set; }
    public string CellFormat { get { return cellFormat; } set { if (!CelLckOut) cellFormat = value; } }
    public FormatTarget Target { get; set; }

    private string RowHdrFormat_Prev { get; set; }
    private string ColHdrFormat_Prev { get; set; }
    private string CellFormat_Prev   { get; set; }

    private string cellFormat;

    public DgvNumFormat()
    {
        CelLckOut    = false;
        RowHdrFormat = "N0";
        ColHdrFormat = "N0";
        CellFormat   = "N0";
        Target       = FormatTarget.All;
    }

    /// <summary>
    /// Compares the set number format(s) to the previously set number formats to determine
    /// the new number format target. Row, column or cell format properties must be set
    /// before calling Update().
    /// </summary>
    public void Update()
    {
        if (RowHdrFormat_Prev != RowHdrFormat || ColHdrFormat_Prev != ColHdrFormat || CellFormat_Prev != CellFormat)
        {
            bool rowChanged  = RowHdrFormat_Prev != RowHdrFormat;
            bool colChanged  = ColHdrFormat_Prev != ColHdrFormat;
            bool cellChanged = CellFormat_Prev   != CellFormat;

            if (rowChanged && !colChanged && !cellChanged)
                Target = FormatTarget.RowHeaders;
            else if (!rowChanged && colChanged && !cellChanged)
                Target = FormatTarget.ColHeaders;
            else if (!rowChanged && !colChanged && cellChanged)
                Target = FormatTarget.Cells;
            else if (rowChanged && colChanged && !cellChanged)
                Target = FormatTarget.AllHeaders;
            else if (rowChanged && colChanged && cellChanged)
                Target = FormatTarget.All;
        }
        else
        {
            Target = FormatTarget.None;
        }

        RowHdrFormat_Prev = RowHdrFormat;
        ColHdrFormat_Prev = ColHdrFormat;
        CellFormat_Prev   = CellFormat;
    }
}
