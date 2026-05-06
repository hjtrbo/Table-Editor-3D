namespace TableEditor.Clipboard;

// Holds all intermediate and final parsed state for one clipboard operation.
// A single shared static instance lives on Paste; Reset() is called at the start of every
// ParseClipboardToDgv invocation so stale data from a prior paste cannot bleed into the new one.
public class MyClipboard
{
    // ---- Raw input ----

    // The verbatim text read from the system clipboard or the source file, before any processing.
    public string RawClipboardText { get; set; }

    // The raw text split into a 2-D array of [row, column] cells.
    public string[,] RawClipboardTextArray { get; set; }

    // Dimensions of RawClipboardTextArray after any trailing-row / trailing-column trimming.
    public int RowLength { get; set; }
    public int ColLength { get; set; }

    // Intermediate row and column string arrays used while building RawClipboardTextArray.
    public string[] RawRows { get; set; }
    public string[] RawColumns { get; set; }

    // ---- Structural flags ----

    // Set by ParseClipboardToDgv based on the chosen eMode so the parsing stages know which
    // rows/columns are axis data versus table data.
    public bool RowHeaderPresent { get; set; }
    public bool ColHeaderPresent { get; set; }
    public bool TableDataPresent { get; set; }

    // True when the axis values could not be parsed as numbers, meaning they carry text labels
    // (e.g. gear names, RPM strings with units) rather than numeric breakpoints.
    public bool RowHeaderIsText { get; set; }
    public bool ColHeaderIsText { get; set; }

    // True when the parsed axis values differ from what was already in the grid, allowing the
    // host to decide whether to trigger a full table rebuild.
    public bool HeadersChanged { get; set; }

    // ---- Parsed axis data ----

    public string[] RowHeadersText { get; set; }
    public string[] ColHeadersText { get; set; }
    public double[] RowHeaders { get; set; }
    public double[] ColHeaders { get; set; }

    // ---- Parsed table data ----

    public double[,] TableData { get; set; }

    // ---- Status ----

    // Human-readable description of why parsing failed; empty string when no error occurred.
    public string ErrorText { get; set; }

    // Records which eMode was active so downstream consumers (e.g. NDR event handlers) can
    // branch on it without carrying the mode as a separate parameter.
    public Paste.eMode PasteMode { get; set; }

    // Resets all state to safe defaults before starting a new parse pass.
    public void Reset()
    {
        RawClipboardText      = "";
        RawClipboardTextArray = new string[0, 0];
        RowLength             = 0;
        ColLength             = 0;
        RawRows               = new string[0];
        RawColumns            = new string[0];
        RowHeaderPresent      = false;
        ColHeaderPresent      = false;
        TableDataPresent      = false;
        RowHeaderIsText       = false;
        ColHeaderIsText       = false;
        RowHeadersText        = new string[0];
        ColHeadersText        = new string[0];
        RowHeaders            = new double[0];
        ColHeaders            = new double[0];
        TableData             = new double[0, 0];
        ErrorText             = "";
        PasteMode             = Paste.eMode.None;
    }
}
