using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TableEditor.Common;
using TableEditor.DataGrid;

namespace TableEditor.Clipboard;

// Reads text from the system clipboard (or a file), parses it according to the chosen eMode,
// and writes the result back into a DgvCtrl.  Raises Paste_NDR on success so host code can
// chain actions (e.g. refresh the 3-D graph) without polling.
public class Paste
{
    public string InstanceName { get; set; }
    public string ClassName { get; set; } = "Paste";
    public bool Debug { get; set; }
    public bool InProgress { get; private set; }

    // Fired when parsing succeeds.  The DgvData payload carries the complete set of axis and
    // table values that were written to the grid, plus the mode that produced them.
    public event EventHandler<DgvData> Paste_NDR;

    // Single shared clipboard state object — reset at the top of every ParseClipboardToDgv call
    // to prevent stale data from a previous paste bleeding into the new one.
    public static MyClipboard clipboard;

    public Paste()
    {
        clipboard = new MyClipboard();
    }

    // ---- Enums ----

    // Describes the structure of the data on the clipboard and how it maps onto the grid.
    public enum eMode
    {
        None,
        CopyWithAxis,
        Copy,
        PasteTableWithXYAxis,
        PasteTableWithXAxis,
        PasteTableWithYAxis,
        PasteTableWithNoAxis,
        PasteXAxis,
        PasteYAxis,
        PasteToCurrentCell,
        PasteSpecial_MultiplyByPercent,
        PasteSpecial_MultiplyByPercentHalf,
        PasteSpecial_DivideByPercent,
        PasteSpecial_DivideByPercentHalf,
        PasteSpecial_Add,
        PasteSpecial_Subtract,
        Default
    }

    // Where the raw text comes from.
    public enum eDataSource
    {
        ClipBoard,
        TextFile
    }

    // ---- Main Parse Method ----

    // Parses clipboard/file text into the grid according to copyPasteMode.
    //
    // Control flow: every early-exit path sets clipboard.ErrorText and returns.  The finally
    // block always fires Paste_NDR (on success) or logs the error (on failure) and clears
    // InProgress, matching the original goto-end behaviour without the label.
    public void ParseClipboardToDgv(
        DgvCtrl dgvCtrl,
        eMode copyPasteMode,
        eDataSource dataSource = eDataSource.ClipBoard,
        string fileName = null)
    {
        int i, j;
        DataTable dt    = GetBoundDataTableFromDgv(dgvCtrl.dgv);
        bool error      = false;

        InProgress = true;
        clipboard.Reset();
        clipboard.PasteMode = copyPasteMode;

        if (Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ParseClipboardToDgv.InProgress {InProgress}");

        try
        {
            // ---- Configure structural flags from the chosen mode ----
            switch (copyPasteMode)
            {
                case eMode.PasteTableWithXYAxis:
                    clipboard.RowHeaderPresent = true;
                    clipboard.ColHeaderPresent = true;
                    clipboard.TableDataPresent = true;
                    break;

                case eMode.PasteTableWithXAxis:
                    clipboard.RowHeaderPresent = false;
                    clipboard.ColHeaderPresent = true;
                    clipboard.TableDataPresent = true;
                    break;

                case eMode.PasteTableWithYAxis:
                    clipboard.RowHeaderPresent = true;
                    clipboard.ColHeaderPresent = false;
                    clipboard.TableDataPresent = true;
                    break;

                case eMode.PasteTableWithNoAxis:
                    clipboard.RowHeaderPresent = false;
                    clipboard.ColHeaderPresent = false;
                    clipboard.TableDataPresent = true;
                    break;

                case eMode.PasteToCurrentCell:
                    clipboard.RowHeaderPresent = false;
                    clipboard.ColHeaderPresent = false;
                    clipboard.TableDataPresent = true;
                    break;

                case eMode.PasteXAxis:
                    clipboard.RowHeaderPresent = false;
                    clipboard.ColHeaderPresent = true;
                    clipboard.TableDataPresent = false;
                    break;

                case eMode.PasteYAxis:
                    clipboard.RowHeaderPresent = true;
                    clipboard.ColHeaderPresent = false;
                    clipboard.TableDataPresent = false;
                    break;

                case eMode.PasteSpecial_MultiplyByPercent:
                case eMode.PasteSpecial_MultiplyByPercentHalf:
                case eMode.PasteSpecial_DivideByPercent:
                case eMode.PasteSpecial_DivideByPercentHalf:
                case eMode.PasteSpecial_Add:
                case eMode.PasteSpecial_Subtract:
                    clipboard.RowHeaderPresent = false;
                    clipboard.ColHeaderPresent = false;
                    clipboard.TableDataPresent = true;
                    break;
            }

            // ---- Read the raw input text ----

            // Prefer a file path over the system clipboard when explicitly told to, allowing
            // import from saved ECU export files without touching the clipboard.
            if (dataSource == eDataSource.TextFile && fileName != null)
            {
                clipboard.RawClipboardText = File.ReadAllText(fileName);
            }
            else if (dataSource == eDataSource.ClipBoard &&
                        System.Windows.Forms.Clipboard.ContainsData(DataFormats.Text))
            {
                clipboard.RawClipboardText = System.Windows.Forms.Clipboard.GetText();
            }
            else
            {
                clipboard.RawClipboardText = null;
            }

            if (clipboard.RawClipboardText == null)
            {
                clipboard.ErrorText = "No data on clipboard";
                error = true;
                return;
            }

            // HP Tuners exports sometimes include thousands-separator commas inside numeric
            // cells; stripping them unconditionally prevents double.TryParse failures later.
            clipboard.RawClipboardText = clipboard.RawClipboardText.Replace(",", "");

            // ---- Convert raw text into a 2-D string array ----

            clipboard.RawRows = clipboard.RawClipboardText.Split(
                new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Use the first row to size the column dimension; later rows may be shorter but
            // the array is sized from the widest row so we do not re-allocate mid-parse.
            clipboard.RawColumns = clipboard.RawRows[0].Split('\t');
            clipboard.RawClipboardTextArray =
                new string[clipboard.RawRows.Length, clipboard.RawColumns.Length];

            for (i = 0; i < clipboard.RawRows.Length; i++)
            {
                clipboard.RawColumns = clipboard.RawRows[i].Split('\t');

                for (j = 0; j < clipboard.RawColumns.Length; j++)
                    clipboard.RawClipboardTextArray[i, j] = clipboard.RawColumns[j];
            }

            clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0);
            clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1);

            // ---- Strip trailing empty rows / columns injected by HP Tuners or Excel ----

            // HP Tuners appends a blank trailing row and/or column when copying with headers.
            // ProcessLastRow / ProcessLastColumn remove them so the length calculations below
            // are not thrown off by one-off fence-post errors.
            switch (copyPasteMode)
            {
                case eMode.PasteTableWithXYAxis:
                    clipboard.RawClipboardTextArray = ProcessLastRow(clipboard.RawClipboardTextArray);
                    clipboard.RawClipboardTextArray = ProcessLastColumn(clipboard.RawClipboardTextArray);
                    break;

                case eMode.PasteTableWithXAxis:
                    clipboard.RawClipboardTextArray = ProcessLastColumn(clipboard.RawClipboardTextArray);
                    break;

                case eMode.PasteTableWithYAxis:
                    clipboard.RawClipboardTextArray = ProcessLastRow(clipboard.RawClipboardTextArray);
                    break;
            }

            // ---- Compute final row/column lengths ----

            // The mode determines which rows/columns are headers versus data.  Subtracting 1
            // for each axis direction that carries a header row/column gives us the true data
            // extent.  Modes that have no headers use the raw array dimensions directly.
            switch (copyPasteMode)
            {
                case eMode.PasteTableWithXYAxis:
                    clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0) - 1;
                    clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1) - 1;
                    break;

                case eMode.PasteTableWithXAxis:
                    // One header row at the top; all columns are data.
                    clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0) - 1;
                    clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1);
                    break;

                case eMode.PasteTableWithYAxis:
                    // One header column on the left; all rows are data.
                    clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0);
                    clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1) - 1;
                    break;

                case eMode.PasteTableWithNoAxis:
                case eMode.PasteToCurrentCell:
                case eMode.PasteSpecial_MultiplyByPercent:
                case eMode.PasteSpecial_MultiplyByPercentHalf:
                case eMode.PasteSpecial_DivideByPercent:
                case eMode.PasteSpecial_DivideByPercentHalf:
                case eMode.PasteSpecial_Add:
                case eMode.PasteSpecial_Subtract:
                    // No axis columns or rows; the raw dimensions are the data dimensions.
                    // (Note: explicit row/col count helpers were removed 23/08/24 due to
                    //  array-bounds errors; the raw array lengths are used instead.)
                    break;

                case eMode.PasteXAxis:
                case eMode.PasteYAxis:
                    // Length was already set during the 'Convert into rows and columns' phase.
                    break;
            }

            if (clipboard.RowLength == 0 || clipboard.ColLength == 0)
            {
                clipboard.ErrorText = "Set lengths failed. Row or column length is 0";
                error = true;
                return;
            }

            // ---- Validate minimum dimensions ----
            //
            // Dimension table:
            //   PasteTableWithXYAxis  : rows >= 1, cols >= 1 (data portion after header removal)
            //   PasteTableWithXAxis   : rows >= 1, cols >= 1
            //   PasteTableWithYAxis   : rows >= 1, cols >= 1
            //   PasteTableWithNoAxis  : rows >= 1, cols >= 1
            //   PasteToCurrentCell    : rows >= 1, cols >= 1
            //   PasteSpecial_*        : rows >= 1, cols >= 1
            //   PasteXAxis            : cols must equal existing grid column count
            //   PasteYAxis            : rows must equal existing grid row count

            switch (copyPasteMode)
            {
                case eMode.PasteTableWithXYAxis:
                case eMode.PasteTableWithXAxis:
                case eMode.PasteTableWithYAxis:
                case eMode.PasteTableWithNoAxis:
                case eMode.PasteToCurrentCell:
                case eMode.PasteSpecial_MultiplyByPercent:
                case eMode.PasteSpecial_MultiplyByPercentHalf:
                case eMode.PasteSpecial_DivideByPercent:
                case eMode.PasteSpecial_DivideByPercentHalf:
                case eMode.PasteSpecial_Add:
                case eMode.PasteSpecial_Subtract:
                    if (clipboard.RowLength < 1 || clipboard.ColLength < 1)
                    {
                        clipboard.ErrorText = "Row or column length is <1";
                        error = true;
                        return;
                    }
                    break;

                case eMode.PasteXAxis:
                    // The pasted axis must match the existing grid width exactly, otherwise
                    // there is no safe way to align headers with data columns.
                    if (clipboard.ColLength != dt.Columns.Count)
                    {
                        clipboard.ErrorText =
                            "Paste single axis failed, the axis length is not equal to the data table length";
                        error = true;
                        return;
                    }
                    break;

                case eMode.PasteYAxis:
                    if (clipboard.RowLength != dt.Rows.Count)
                    {
                        clipboard.ErrorText =
                            "Paste single axis failed, the axis length is not equal to the data table length";
                        error = true;
                        return;
                    }
                    break;
            }

            // ---- Parse row headers ----

            if (clipboard.RowHeaderPresent)
            {
                clipboard.RowHeaders     = new double[clipboard.RowLength];
                clipboard.RowHeadersText = new string[clipboard.RowLength];

                int rowLength = clipboard.RowLength;

                // When a column header row is also present the row-header column starts at
                // array row 1 (row 0 is the column header); otherwise it starts at row 0.
                if (clipboard.ColHeaderPresent)
                {
                    for (i = 1; i < rowLength + 1; i++)
                        clipboard.RowHeadersText[i - 1] = clipboard.RawClipboardTextArray[i, 0];
                }
                else
                {
                    for (i = 0; i < rowLength; i++)
                        clipboard.RowHeadersText[i] = clipboard.RawClipboardTextArray[i, 0];
                }

                for (i = 0; i < rowLength; i++)
                {
                    if (double.TryParse(clipboard.RowHeadersText[i], out double result))
                    {
                        clipboard.RowHeaders[i] = result;
                    }
                    else
                    {
                        // Non-numeric labels (e.g. gear names) get a sequential index so the
                        // rest of the pipeline can treat them as ordinary numeric breakpoints.
                        clipboard.RowHeaders[i]  = i;
                        clipboard.RowHeaderIsText = true;
                    }
                }
            }

            // ---- Parse column headers ----

            if (clipboard.ColHeaderPresent)
            {
                clipboard.ColHeaders     = new double[clipboard.ColLength];
                clipboard.ColHeadersText = new string[clipboard.ColLength];

                int colLength = clipboard.ColLength;

                // Mirror of the row-header logic: offset by 1 when a row-header column is present.
                if (clipboard.RowHeaderPresent)
                {
                    for (i = 1; i < colLength + 1; i++)
                        clipboard.ColHeadersText[i - 1] = clipboard.RawClipboardTextArray[0, i];
                }
                else
                {
                    for (i = 0; i < colLength; i++)
                        clipboard.ColHeadersText[i] = clipboard.RawClipboardTextArray[0, i];
                }

                for (i = 0; i < colLength; i++)
                {
                    if (double.TryParse(clipboard.ColHeadersText[i], out double result))
                    {
                        clipboard.ColHeaders[i] = result;
                    }
                    else
                    {
                        clipboard.ColHeaders[i]   = i;
                        clipboard.ColHeaderIsText  = true;
                    }
                }
            }

            // ---- HP Tuners Y-axis orientation fix ----

            // HP Tuners exports the Y axis as a single row (1 row × N cols). PCMTec exports it
            // as a single column (N rows × 1 col).  We normalise the HP layout to the PCMTec
            // column layout so the rest of the pipeline only needs to handle one shape.
            if (copyPasteMode == eMode.PasteYAxis)
            {
                bool sourceIsHp = false;

                // Validate: exactly one of the two supported orientations must match dt.Rows.Count.
                bool hpOrientation    = clipboard.RawClipboardTextArray.GetLength(0) == 1 &&
                                        clipboard.RawClipboardTextArray.GetLength(1) == dt.Rows.Count;
                bool pcmtecOrientation = clipboard.RawClipboardTextArray.GetLength(1) == 1 &&
                                        clipboard.RawClipboardTextArray.GetLength(0) == dt.Rows.Count;

                if (!hpOrientation && !pcmtecOrientation)
                {
                    clipboard.ErrorText =
                        "Paste Y axis failed, the attempted paste axis length is not equal to the data table length";
                    error = true;
                    return;
                }

                sourceIsHp = hpOrientation;

                if (sourceIsHp)
                {
                    // Transpose the single-row array into a single-column array.
                    var copy = new string[clipboard.RawClipboardTextArray.GetLength(0),
                                            clipboard.RawClipboardTextArray.GetLength(1)];
                    Array.Copy(clipboard.RawClipboardTextArray, copy, dt.Rows.Count);

                    clipboard.RawClipboardTextArray = new string[dt.Rows.Count, 1];

                    for (i = 0; i < dt.Rows.Count; i++)
                        clipboard.RawClipboardTextArray[i, 0] = copy[0, i];
                }

                // After the optional transpose, fix up the lengths to reflect a column vector.
                clipboard.RowLength = 1;
                clipboard.ColLength = sourceIsHp
                    ? clipboard.RawClipboardTextArray.GetLength(0)  // HP: rows now hold the values
                    : clipboard.RawClipboardTextArray.GetLength(1); // PCMTec: single column
            }

            // ---- Allocate output arrays ----

            switch (copyPasteMode)
            {
                case eMode.PasteTableWithXYAxis:
                case eMode.PasteTableWithXAxis:
                case eMode.PasteTableWithYAxis:
                    clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                    break;

                case eMode.PasteTableWithNoAxis:
                    clipboard.RowHeadersText = new string[clipboard.RowLength];
                    clipboard.ColHeadersText = new string[clipboard.ColLength];
                    clipboard.RowHeaders     = new double[clipboard.RowLength];
                    clipboard.ColHeaders     = new double[clipboard.ColLength];
                    clipboard.TableData      = new double[clipboard.RowLength, clipboard.ColLength];
                    break;

                case eMode.PasteXAxis:
                case eMode.PasteYAxis:
                case eMode.PasteToCurrentCell:
                case eMode.PasteSpecial_MultiplyByPercent:
                case eMode.PasteSpecial_MultiplyByPercentHalf:
                case eMode.PasteSpecial_DivideByPercent:
                case eMode.PasteSpecial_DivideByPercentHalf:
                case eMode.PasteSpecial_Add:
                case eMode.PasteSpecial_Subtract:
                    clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                    break;
            }

            // ---- Parse table data ----

            switch (copyPasteMode)
            {
                case eMode.PasteTableWithXYAxis:
                    // Data starts at [1,1] because [0,*] is the column header row and [*,0] is
                    // the row header column.
                    for (i = 0; i < clipboard.RowLength; i++)
                    {
                        for (j = 0; j < clipboard.ColLength; j++)
                        {
                            clipboard.TableData[i, j] = TryParseDouble(
                                clipboard.RawClipboardTextArray[i + 1, j + 1]);
                        }
                    }
                    break;

                case eMode.PasteTableWithXAxis:
                    // Data starts at row 1 (row 0 is the column header).  Create token row
                    // headers because the source did not include them.
                    clipboard.RowHeaders     = new double[clipboard.RowLength];
                    clipboard.RowHeadersText = new string[clipboard.RowLength];

                    for (i = 0; i < clipboard.RowLength; i++)
                    {
                        for (j = 0; j < clipboard.ColLength; j++)
                        {
                            clipboard.TableData[i, j] = TryParseDouble(
                                clipboard.RawClipboardTextArray[i + 1, j]);
                        }

                        // Assign 1-based token labels so the row headers are not all zero.
                        clipboard.RowHeaders[i]     = i + 1;
                        clipboard.RowHeadersText[i] = (i + 1).ToString();
                    }
                    break;

                case eMode.PasteTableWithYAxis:
                    // Data starts at column 1 (column 0 is the row header).  Create token
                    // column headers.
                    clipboard.ColHeaders     = new double[clipboard.ColLength];
                    clipboard.ColHeadersText = new string[clipboard.ColLength];

                    for (i = 0; i < clipboard.RowLength; i++)
                    {
                        for (j = 1; j < clipboard.ColLength + 1; j++)
                        {
                            clipboard.TableData[i, j - 1] = TryParseDouble(
                                clipboard.RawClipboardTextArray[i, j]);
                        }
                    }

                    for (i = 0; i < clipboard.ColLength; i++)
                    {
                        clipboard.ColHeaders[i]     = i + 1;
                        clipboard.ColHeadersText[i] = (i + 1).ToString();
                    }
                    break;

                case eMode.PasteTableWithNoAxis:
                    // No axis at all: generate sequential token labels for both axes and map
                    // the entire array as data.
                    for (i = 0; i < clipboard.RowLength; i++)
                    {
                        clipboard.RowHeadersText[i] = i.ToString();
                        clipboard.RowHeaders[i]     = i;
                    }

                    for (i = 0; i < clipboard.ColLength; i++)
                    {
                        clipboard.ColHeadersText[i] = i.ToString();
                        clipboard.ColHeaders[i]     = i;
                    }

                    for (i = 0; i < clipboard.RowLength; i++)
                    {
                        for (j = 0; j < clipboard.ColLength; j++)
                        {
                            clipboard.TableData[i, j] = TryParseDouble(
                                clipboard.RawClipboardTextArray[i, j]);
                        }
                    }
                    break;
            }

            // ---- Write results back to the grid ----

            switch (copyPasteMode)
            {
                // Full table replacement — headers and data are all rewritten.
                case eMode.PasteTableWithXYAxis:
                case eMode.PasteTableWithXAxis:
                case eMode.PasteTableWithYAxis:
                case eMode.PasteTableWithNoAxis:
                    dgvCtrl.WriteToDataGridView(clipboard.RowHeaders, clipboard.ColHeaders, clipboard.TableData);
                    dgvCtrl.ClearSelection();
                    clipboard.HeadersChanged = true;

                    if (Debug)
                        Console.WriteLine(
                            $"{InstanceName} - {ClassName} - {Enum.GetName(typeof(eMode), copyPasteMode)}");
                    break;

                // Axis-only replacement — re-dimensions the grid if the pasted axis is a
                // different length, then writes only the header labels.
                case eMode.PasteXAxis:
                    dgvCtrl.ReDimensionDataTable_v2(dt.Rows.Count, clipboard.ColHeaders.Length);
                    dgvCtrl.WriteColHeaderLabels(clipboard.ColHeaders);
                    dgvCtrl.ClearSelection();
                    clipboard.HeadersChanged = true;

                    if (Debug)
                        Console.WriteLine(
                            $"{InstanceName} - {ClassName} - {Enum.GetName(typeof(eMode), copyPasteMode)}");
                    break;

                case eMode.PasteYAxis:
                    dgvCtrl.ReDimensionDataTable_v2(clipboard.RowHeaders.Length, dt.Columns.Count);
                    dgvCtrl.WriteRowHeaderLabels(clipboard.RowHeaders);
                    dgvCtrl.ClearSelection();
                    clipboard.HeadersChanged = true;

                    if (Debug)
                        Console.WriteLine(
                            $"{InstanceName} - {ClassName} - {Enum.GetName(typeof(eMode), copyPasteMode)}");
                    break;

                // Cell-level paste: reads the existing grid values, overlays the clipboard
                // block starting at the currently selected cell, and writes back.
                case eMode.PasteToCurrentCell:
                case eMode.PasteSpecial_MultiplyByPercent:
                case eMode.PasteSpecial_MultiplyByPercentHalf:
                case eMode.PasteSpecial_DivideByPercent:
                case eMode.PasteSpecial_DivideByPercentHalf:
                case eMode.PasteSpecial_Add:
                case eMode.PasteSpecial_Subtract:
                {
                    int rowLength    = dt.Rows.Count;
                    int columnLength = dt.Columns.Count;
                    var dgvDataTable = new double[rowLength, columnLength];

                    // Snapshot the current grid values so we can do arithmetic in-memory
                    // without touching the DataGridView on every cell.
                    for (i = 0; i < rowLength; i++)
                    {
                        for (j = 0; j < columnLength; j++)
                        {
                            if (dt.Rows[i][j] != DBNull.Value)
                                dgvDataTable[i, j] = double.Parse(dt.Rows[i][j].ToString());
                        }
                    }

                    // Determine the top-left anchor cell; default to [0,0] when nothing is
                    // selected so pasting always has a defined start position.
                    Point topLeft = dgvCtrl.TopLeftCellAddress;

                    if (topLeft.X == -1 && topLeft.Y == -1)
                    {
                        topLeft.X = 0;
                        topLeft.Y = 0;
                    }

                    // Parse the clipboard block to double first so we can apply the arithmetic
                    // operators below without repeated string conversions.
                    for (i = 0; i < clipboard.RowLength; i++)
                    {
                        for (j = 0; j < clipboard.ColLength; j++)
                        {
                            clipboard.TableData[i, j] = TryParseDouble(
                                clipboard.RawClipboardTextArray[i, j]);
                        }
                    }

                    // Walk the grid from the anchor outward, applying the paste operation.
                    // Cells outside the grid bounds are silently skipped; cells that exceed
                    // the clipboard block dimensions stop the inner / outer loop respectively.
                    int m = 0, n = 0;

                    for (i = topLeft.Y; i < dgvDataTable.GetLength(0); i++)
                    {
                        for (j = topLeft.X; j < dgvDataTable.GetLength(1); j++)
                        {
                            if (!dgvCtrl.DgvHasData)
                            {
                                clipboard.ErrorText =
                                    $"parse to selected cell at row {i} column {j} failed";
                                error = true;
                                return;
                            }

                            switch (copyPasteMode)
                            {
                                case eMode.PasteToCurrentCell:
                                    dgvDataTable[i, j] = clipboard.TableData[m, n];
                                    break;

                                case eMode.PasteSpecial_MultiplyByPercent:
                                    dgvDataTable[i, j] = MultiplyByPercent(1.0, dgvDataTable[i, j], clipboard.TableData[m, n]);
                                    break;

                                case eMode.PasteSpecial_MultiplyByPercentHalf:
                                    dgvDataTable[i, j] = MultiplyByPercent(0.5, dgvDataTable[i, j], clipboard.TableData[m, n]);
                                    break;

                                case eMode.PasteSpecial_DivideByPercent:
                                    dgvDataTable[i, j] = DivideByPercent(1.0, dgvDataTable[i, j], clipboard.TableData[m, n]);
                                    break;

                                case eMode.PasteSpecial_DivideByPercentHalf:
                                    dgvDataTable[i, j] = DivideByPercent(0.5, dgvDataTable[i, j], clipboard.TableData[m, n]);
                                    break;

                                case eMode.PasteSpecial_Add:
                                    dgvDataTable[i, j] = Add(dgvDataTable[i, j], clipboard.TableData[m, n]);
                                    break;

                                case eMode.PasteSpecial_Subtract:
                                    dgvDataTable[i, j] = Subtract(dgvDataTable[i, j], clipboard.TableData[m, n]);
                                    break;
                            }

                            n++;

                            if (n >= clipboard.TableData.GetLength(1))
                                break;
                        }

                        n = 0;
                        m++;

                        if (m >= clipboard.TableData.GetLength(0))
                            break;
                    }

                    // Mirror the modified grid values back into clipboard.TableData so that
                    // the Paste_NDR event payload reflects what was actually written.
                    clipboard.TableData = new double[dgvDataTable.GetLength(0), dgvDataTable.GetLength(1)];

                    for (i = 0; i < dgvDataTable.GetLength(0); i++)
                    {
                        for (j = 0; j < dgvDataTable.GetLength(1); j++)
                            clipboard.TableData[i, j] = dgvDataTable[i, j];
                    }

                    dgvCtrl.WriteToDataTable(clipboard.TableData);
                    clipboard.HeadersChanged = false;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // Show a dialog so the user knows something went wrong; ExceptionHelper gives us
            // the ":line NNN" suffix without the fragile manual Substring / LastIndexOf pattern.
            MessageBox.Show(
                $"{ex.Message}\r\nAt{ExceptionHelper.FormatStackTrace(ex)}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            error = true;
        }
        finally
        {
            // This block always runs, whether the try completed normally, hit an early return,
            // or threw an exception.  It replaces the original goto end: label.

            if (!error)
            {
                var e = new DgvData
                {
                    RowHeadersText  = clipboard.RowHeadersText,
                    ColHeadersText  = clipboard.ColHeadersText,
                    RowHeaders      = clipboard.RowHeaders,
                    ColHeaders      = clipboard.ColHeaders,
                    TableData       = clipboard.TableData,
                    RowHeaderFormat = dgvCtrl.RowHeaderFormat,
                    ColHeaderFormat = dgvCtrl.ColHeaderFormat,
                    CopyPasteMode   = copyPasteMode
                };
                e.FormatHeaderText();

                Paste_NDR?.Invoke(this, e);
            }
            else
            {
#if DEBUG
                Console.WriteLine(clipboard.ErrorText);
#endif
                MessageBox.Show(clipboard.ErrorText, "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            InProgress = false;

            if (Debug)
                Console.WriteLine(
                    $"{InstanceName} - {ClassName} - ParseClipboardToDgv.InProgress {InProgress}");
        }
    }

    // ---- Private Helpers ----

    // Checks whether the last row in the array is essentially empty (all-null / all-empty
    // except for at most one cell), and if so returns a new array with that row removed.
    // This handles HP Tuners' habit of appending a blank trailing row when exporting.
    private string[,] ProcessLastRow(string[,] text)
    {
        if (text.GetLength(0) == 1)
            return text;

        int cnt = 0;

        for (int i = 0; i < clipboard.ColLength; i++)
        {
            if (string.IsNullOrEmpty(text[text.GetLength(0) - 1, i]))
                cnt++;
        }

        if (cnt >= text.GetLength(1) - 1)
            text = RemoveLastRow(clipboard.RawClipboardTextArray);

        return text;
    }

    // Mirror of ProcessLastRow but for the last column, handling the same HP Tuners artefact
    // in the horizontal direction.
    private string[,] ProcessLastColumn(string[,] text)
    {
        if (text.GetLength(1) == 1)
            return text;

        int cnt = 0;

        for (int i = 0; i < text.GetLength(0); i++)
        {
            if (string.IsNullOrEmpty(clipboard.RawClipboardTextArray[i, text.GetLength(1) - 1]))
                cnt++;
        }

        if (cnt >= text.GetLength(0) - 1)
            text = RemoveLastColumn(clipboard.RawClipboardTextArray);

        return text;
    }

    // Counts non-empty lines so callers can verify row counts without re-splitting.
    // NOTE: this method is retained for reference but is no longer called because it caused
    // array-bounds errors when invoked on multi-line clipboard data (fixed 23/08/24).
    [Obsolete("Caused array-bounds errors with VCM Scanner data — use raw array dimensions instead (fixed 23/08/24).")]
    private int GetRowCountFromRawInputText(string input)
    {
        string[] lines = input.Split('\n');
        return lines.Count();
    }

    // Counts tab-delimited columns in the first row.
    // NOTE: same as GetRowCountFromRawInputText — retired 23/08/24, kept for archaeology.
    [Obsolete("Caused array-bounds errors with VCM Scanner data — use raw array dimensions instead (fixed 23/08/24).")]
    private int GetColCountFromRawInputText(string input)
    {
        string[] rows    = input.Split('\n');
        string[] columns = rows[0].Split('\t');
        return columns.Count();
    }

    private string[,] RemoveLastRow(string[,] array2d)
    {
        int rows = array2d.GetLength(0);
        int cols = array2d.GetLength(1);
        var result = new string[rows - 1, cols];

        for (int i = 0; i < rows - 1; i++)
            for (int j = 0; j < cols; j++)
                result[i, j] = array2d[i, j];

        return result;
    }

    private string[,] RemoveLastColumn(string[,] array2d)
    {
        int rows = array2d.GetLength(0);
        int cols = array2d.GetLength(1);
        var result = new string[rows, cols - 1];

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols - 1; j++)
                result[i, j] = array2d[i, j];

        return result;
    }

    // Retrieves the DataTable that backs a DataGridView, handling both a direct DataTable
    // DataSource and a BindingSource wrapper over a DataTable.
    private DataTable GetBoundDataTableFromDgv(DataGridView dataGridView)
    {
        if (dataGridView.DataSource is DataTable directTable)
            return directTable;

        if (dataGridView.DataSource is BindingSource bindingSource &&
            bindingSource.DataSource is DataTable boundTable)
            return boundTable;

        return null;
    }

    // Returns parsed double or 0.0 when the text cannot be converted.  Centralising this
    // avoids repeating the TryParse / fallback pattern across every table-data loop.
    private static double TryParseDouble(string text)
    {
        return double.TryParse(text, out double result) ? result : 0.0;
    }

    // Scales dataValue by (1 + modifierValue * percentScalar / 100).
    // percentScalar = 1.0 applies the full modifier; 0.5 applies half.
    private double MultiplyByPercent(double percentScalar, double dataValue, double modifierValue)
    {
        return dataValue * (1.0 + modifierValue * percentScalar / 100.0);
    }

    // Inverse of MultiplyByPercent, preserving the same percentScalar semantics.
    private double DivideByPercent(double percentScalar, double dataValue, double modifierValue)
    {
        return dataValue / (1.0 + modifierValue * percentScalar / 100.0);
    }

    private double Add(double dataValue, double modifierValue)
    {
        return dataValue + modifierValue;
    }

    private double Subtract(double dataValue, double modifierValue)
    {
        return dataValue - modifierValue;
    }
}
