using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using TableEditor.Clipboard;
using TableEditor.Layout;
using TableEditor.UndoRedo;
using Key = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;

namespace TableEditor.DataGrid;

public class DgvCtrl
{
    // Properties

    public string ClassName    { get; set; } = "DgvCtrl";
    public string InstanceName { get; set; }

    // Dgv
    public DataGridView Dgv
    {
        set { dgv = value; }
    }
    public Size DgvSize
    {
        get { return dgv.Size; }
    }
    public bool DgvHasData  { get; set; }
    public int  RowCount    { get { return dgv.Rows.Count; } }
    public int  ColumnCount { get { return dgv.Columns.Count; } }
    public bool ReadOnly    { set { dgv.ReadOnly = value; } }
    public bool Focused     { get { return dgv.Focused; } set { dgv.Focus(); } }
    public bool TransposeXY { get; set; }
    public ScrollBars ScrollBars { get; set; }
    public LayoutControls ScrollBarCntrls { get; set; }
    public LayoutControls DgvHeaderCntrls { get; set; }

    // Appearance
    public Color CellsForeColour
    {
        set
        {
            defaultForeColour = value;
            dgv.SuspendLayout();
            foreach (DataGridViewRow row in dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    cell.Style.ForeColor = value;
            dgv.ResumeLayout(true);
        }
    }
    public Color CellsBackColour
    {
        set
        {
            defaultBackColour = value;
            dgv.SuspendLayout();
            foreach (DataGridViewRow row in dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    cell.Style.BackColor = value;
            dgv.ResumeLayout(true);
        }
    }
    public Color CellsSelectionForeColour
    {
        set
        {
            defaultSelectionForeColour = value;
            dgv.SuspendLayout();
            foreach (DataGridViewRow row in dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    cell.Style.SelectionForeColor = value;
            dgv.ResumeLayout(true);
        }
    }
    public Color CellsSelectionBackColour
    {
        set
        {
            defaultSelectionBackColour = value;
            dgv.SuspendLayout();
            foreach (DataGridViewRow row in dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    cell.Style.SelectionBackColor = value;
            dgv.ResumeLayout(true);
        }
    }
    public int  RowHeight       { get { return dgv.Rows[0].Height; } }
    public int  ColumnWidth     { get { return dgv.Columns[0].Width; } }
    public int  RowHeaderWidth  { get { return dgv.Rows[0].HeaderCell.Size.Width; } }
    public int  ColHeaderWidth  { get { return dgv.Columns[0].HeaderCell.Size.Width; } }
    public string RowHeaderFormat  { get { return NumberFormatter.GetNumberFormat(dgv.Rows[0].HeaderCell.Value.ToString()); } }
    public string ColHeaderFormat  { get { return NumberFormatter.GetNumberFormat(dgv.Columns[0].HeaderText); } }
    public string DataTableFormat  { get { return NumberFormatter.GetNumberFormat(dgv.Rows[0].Cells[0].FormattedValue.ToString()); } }
    public int hTextPadding { get; set; } = H_TEXT_PADDING;
    public int vTextPadding { get; set; } = V_TEXT_PADDING;
    public Font Font
    {
        get { return dgv.DefaultCellStyle.Font; }
        set { font = value; dgv.DefaultCellStyle.Font = value; }
    }

    // Data
    public bool Z_RemoteValuesChanging { get; set; }
    public Point TopLeftCellAddress    { get { return GetTopLeftCellAddress(dgv.SelectedCells); } }
    public Point SelectedCellAddress   { get; private set; }
    public DataGridViewCell CurrentCell { get { return dgv.CurrentCell; } }
    public DataGridViewSelectedCellCollection SelectedCellCollection { get { return dgv.SelectedCells; } }
    public bool NewTablePasted
    {
        get { return myEvents.NewTablePasted; }
        set { myEvents.NewTablePasted = value; }
    }

    // Options & settings
    public bool UndoEnabled      { get; set; }
    public bool CopyPasteEnabled { get; set; }
    public bool UseMyScrollBars  { get; set; }
    public ColourScheme ColourTheme { get; set; }
    public bool InitialiseDgvCtrl { set { Initialise(); } }

    // Debug
    public bool EventDebug           { get; set; }
    public bool DataChangedDebug     { get; set; }
    public bool SelectionChangedDebug { get; set; }
    public bool SizeChangedDebug     { get; set; }
    public bool DebugMouse           { get; set; }
    public bool DgvData_Debug        { get; set; }

    // Fields

    public Copy copy;
    public Paste paste;
    public IncDec incDecTask;
    public DgvNumFormat dgvNumFormat;
    public DataGridView dgv;
    public DgvEvents myEvents;
    public ScrollBarCtrl scrollBarCtrl;
    public DgvHeadersCtrl dgvHeaders;

    public DataTable dt;

    bool keepCellsSelectedAfterEdit = false;
    DataGridViewCell currentCell_Copy;
    DataGridViewSelectedCellCollection selectedCellsCollection_Copy;

    public Undo undo;
    public Redo redo;
    public bool InitImage_ONS;

    public event EventHandler<bool> FormButton_UndoEnabled;
    public event EventHandler<bool> FormButton_RedoEnabled;

    bool userCellEditPending = false;

    List<int> selectedRows = new List<int>();
    List<int> selectedCols = new List<int>();

    Font font = new Font("Calibri", 8.75f, FontStyle.Regular);
    const int ROW_HEIGHT = 18;
    const int COLUMN_HEADER_HEIGHT = 20;
    const int MINIMUM_ROW_HEADER_WIDTH = 42;
    const int MINIMUM_COLUMN_WIDTH = 38;
    const int H_TEXT_PADDING = 6;
    const int V_TEXT_PADDING = 2;
    const int H_BORDER_PADDING = 2;
    const int V_BORDER_PADDING = 2;
    int columnWidth    = MINIMUM_COLUMN_WIDTH;
    int rowHeaderWidth = MINIMUM_ROW_HEADER_WIDTH;
    Color defaultForeColour          = SystemColors.ControlText;
    Color defaultBackColour          = SystemColors.Window;
    Color defaultSelectionForeColour = SystemColors.HighlightText;
    Color defaultSelectionBackColour = SystemColors.Highlight;

    // Constructor

    public DgvCtrl()
    {
        // Empty constructor to enable object-initialiser syntax.
    }

    public DgvCtrl(DataGridView dgv, LayoutControls scrollBarControls, bool undoEnabled, bool copyPasteEnabled, bool useMyScrollBars, ColourScheme colourTheme, string instanceName)
    {
        Dgv              = dgv;
        ScrollBarCntrls  = scrollBarControls;
        UndoEnabled      = undoEnabled;
        CopyPasteEnabled = copyPasteEnabled;
        UseMyScrollBars  = useMyScrollBars;
        ColourTheme      = colourTheme;
        InstanceName     = instanceName;

        Initialise();
    }

    public void Initialise()
    {
        this.Dgv = dgv;
        this.dt  = new DataTable();

        this.dgv.DataSource = dt;

        myEvents     = new DgvEvents(this);
        dgvNumFormat = new DgvNumFormat();
        incDecTask   = new IncDec(this);

        if (UndoEnabled) undo = new Undo();
        if (UndoEnabled) redo = new Redo();
        if (CopyPasteEnabled) copy  = new Copy();
        if (CopyPasteEnabled) paste = new Paste();

        if (UseMyScrollBars) scrollBarCtrl = new ScrollBarCtrl(this, InstanceName);
        if (UseMyScrollBars) scrollBarCtrl.Initiate();

        if (UseMyScrollBars) dgvHeaders = new DgvHeadersCtrl(this, InstanceName);

        AssignInstanceNames();
        EnableDoubleBuffering();
        ResetDataTable();
        StyleOverrides(dgv);
        SetDgvSize();

        // Always-on events
        incDecTask.IncDec_Incremental_NDR += IncDec_Incremental_NDR;
        incDecTask.IncDec_Completed_NDR   += IncDec_Completed_NDR;

        if (CopyPasteEnabled)
            paste.Paste_NDR += Paste_Completed_NDR;

        if (UndoEnabled)
            undo.NDR += Undo_Completed_NDR;

        if (!UseMyScrollBars)
        {
            this.dgv.RowHeaderMouseClick    += RowHeader_CellClick;
            this.dgv.ColumnHeaderMouseClick += ColHeader_CellClick;
        }

        myEvents.DgvDataChanged_Debounced   += MyEvents_Dgv_NDR_Debounced;
        myEvents.DgvDataChanged_Immediate    += MyEvents_Dgv_NDR_Immediate;
        myEvents.DgvSizeChanged_Intermittent += MyEvents_Dgv_NewSize_Intermittent;

        // Subscribe cell-value and selection events exactly once here.
        // Events_CellDataAndSelectionChanged_Resume uses -=/+= pairs to guarantee
        // a single subscription whenever it is called.
        this.dgv.CellValueChanged += DgvCellValueChanged;
        this.dt.RowChanged        += DtRowChanged;

        this.dgv.CellMouseDown += DgvMouseDown;
        this.dgv.SelectionChanged += DgvSelectionChanged;
        this.dgv.SizeChanged      += DgvSizeChanged;
        this.dgv.KeyDown          += DgvKeyDown;

        this.dgv.KeyUp                  += DgvKeyUp;
        this.dgv.EditingControlShowing  += DgvEditingControlShowing;
        this.dgv.CellEndEdit            += DgvCellEndEdit;
        this.dgv.CellValidating         += DgvCellValidating;
        this.dgv.CellClick              += DgvTableCellClick;
        this.dgv.CellDoubleClick        += DgvTableCellDoubleClick;
        this.dgv.MouseUp                += DgvMouseUp;
    }

    // Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            dgvHeaders?.Dispose();
            UnsubscribeEvents();
            font?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void UnsubscribeEvents()
    {
        if (dgv != null)
        {
            dgv.CellValueChanged        -= DgvCellValueChanged;
            dgv.SelectionChanged        -= DgvSelectionChanged;
            dgv.SizeChanged             -= DgvSizeChanged;
            dgv.KeyDown                 -= DgvKeyDown;
            dgv.KeyUp                   -= DgvKeyUp;
            dgv.EditingControlShowing   -= DgvEditingControlShowing;
            dgv.CellEndEdit             -= DgvCellEndEdit;
            dgv.CellValidating          -= DgvCellValidating;
            dgv.CellClick               -= DgvTableCellClick;
            dgv.CellDoubleClick         -= DgvTableCellDoubleClick;
            dgv.MouseUp                 -= DgvMouseUp;
            dgv.CellMouseDown           -= DgvMouseDown;
            dgv.RowHeaderMouseClick     -= RowHeader_CellClick;
            dgv.ColumnHeaderMouseClick  -= ColHeader_CellClick;
        }

        if (dt != null)
            dt.RowChanged -= DtRowChanged;

        if (incDecTask != null)
        {
            incDecTask.IncDec_Incremental_NDR -= IncDec_Incremental_NDR;
            incDecTask.IncDec_Completed_NDR   -= IncDec_Completed_NDR;
        }

        if (paste != null)
            paste.Paste_NDR -= Paste_Completed_NDR;

        if (undo != null)
            undo.NDR -= Undo_Completed_NDR;

        if (myEvents != null)
        {
            myEvents.DgvDataChanged_Debounced   -= MyEvents_Dgv_NDR_Debounced;
            myEvents.DgvDataChanged_Immediate    -= MyEvents_Dgv_NDR_Immediate;
            myEvents.DgvSizeChanged_Intermittent -= MyEvents_Dgv_NewSize_Intermittent;
        }
    }

    // Undo

    public void Undo_Set(DgvData e)
    {
        if (!UndoEnabled)
            return;

        if (!DgvHasData || incDecTask.Mode.Enabled || Z_RemoteValuesChanging)
        {
            if (undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Undo returned");
            return;
        }

        if (undo.CanSet)
        {
            if (undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Undo_Set()");
            undo.Set(e);
        }
        else if (undo.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Undo cannot be set");

        FormButton_UndoEnabled?.Invoke(this, undo.CanDo);
    }

    public void Undo_Get()
    {
        if (!UndoEnabled)
            return;

        if (undo.CanDo)
        {
            if (undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Undo Get()");

            Redo_Set(myEvents.BuildEventArgs_DgvDataChanged_Event());

            DgvData undoData = undo.Get();

            dgvNumFormat.RowHdrFormat = undoData.RowHeaderFormat;
            dgvNumFormat.ColHdrFormat = undoData.ColHeaderFormat;
            dgvNumFormat.CellFormat   = undoData.TableDataFormat;

            WriteToDataGridView(undoData.RowHeaders, undoData.ColHeaders, undoData.TableData, RefreshMode.Partial);

            myEvents.Req_DgvDataChanged_ToHeaders_Event(undoData);

            undo.InProgress = false;
        }

        FormButton_UndoEnabled?.Invoke(this, undo.CanDo);
    }

    public void Redo_Set(DgvData e)
    {
        if (!UndoEnabled)
            return;

        if (!DgvHasData || incDecTask.Mode.Enabled || Z_RemoteValuesChanging)
        {
            if (redo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Redo returned");
            return;
        }

        if (redo.CanSet)
        {
            if (redo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Redo_Set()");
            redo.Set(e);
        }
        else if (redo.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Redo cannot be set");

        FormButton_RedoEnabled?.Invoke(this, redo.CanDo);
    }

    public void Redo_Get()
    {
        if (!UndoEnabled)
            return;

        if (redo.CanDo)
        {
            if (redo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Redo Get()");

            DgvData redoData = redo.Get();

            undo.Set(myEvents.BuildEventArgs_DgvDataChanged_Event());

            dgvNumFormat.RowHdrFormat = redoData.RowHeaderFormat;
            dgvNumFormat.ColHdrFormat = redoData.ColHeaderFormat;
            dgvNumFormat.CellFormat   = redoData.TableDataFormat;

            WriteToDataGridView(redoData.RowHeaders, redoData.ColHeaders, redoData.TableData, RefreshMode.Partial);

            myEvents.Req_DgvDataChanged_ToHeaders_Event(redoData);
        }

        FormButton_RedoEnabled?.Invoke(this, redo.CanDo);
    }

    // DGV Formatting

    public void Refresh(RefreshMode refreshMode)
    {
        this.dgv.SuspendLayout();

        // Unload event — SetNumberFormat triggers CellValueChanged
        dgv.CellValueChanged -= DgvCellValueChanged;

        switch (refreshMode)
        {
            case RefreshMode.All:
                StyleOverrides(dgv);
                dgvNumFormat.RowHdrFormat = NumberFormatter.FormatDouble(ReadRowHeaders());
                dgvNumFormat.ColHdrFormat = NumberFormatter.FormatDouble(ReadColHeaders());
                dgvNumFormat.CellFormat   = NumberFormatter.FormatDouble(dt);
                SetNumberFormat_v1(dgvNumFormat);
                SetCellWidths();
                CellColorizer.SetCellColour(dgv, dt, ColourTheme);
                break;

            case RefreshMode.Partial:
                SetNumberFormat_v1(dgvNumFormat);
                SetCellWidths();
                CellColorizer.SetCellColour(dgv, dt, ColourTheme);
                break;

            case RefreshMode.WidthColour:
                SetCellWidths();
                CellColorizer.SetCellColour(dgv, dt, ColourTheme);
                break;

            case RefreshMode.ColourOnly:
                CellColorizer.SetCellColour(dgv, dt, ColourTheme);
                break;

            case RefreshMode.DpAdjust:
                SetNumberFormat_v1(dgvNumFormat);
                SetCellWidths();
                break;

            case RefreshMode.StyleWidthSize:
                StyleOverrides(dgv);
                SetCellWidths();
                CellColorizer.SetCellColour(dgv, dt, ColourTheme);
                break;

            case RefreshMode.AverageTool:
                StyleOverrides(dgv);
                SetNumberFormat_v1(dgvNumFormat);
                SetCellWidths_NoHeaders();
                break;
        }

        this.dgv.ResumeLayout(true);

        // Reload event exactly once
        dgv.CellValueChanged -= DgvCellValueChanged;
        dgv.CellValueChanged += DgvCellValueChanged;
    }

    public void StyleOverrides(DataGridView targetDgv)
    {
        targetDgv.ColumnHeadersDefaultCellStyle.Font      = font;
        targetDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        targetDgv.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        targetDgv.ColumnHeadersHeight                     = COLUMN_HEADER_HEIGHT + V_TEXT_PADDING;

        foreach (DataGridViewColumn column in targetDgv.Columns)
        {
            column.Width        = columnWidth;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.SortMode     = DataGridViewColumnSortMode.NotSortable;
        }

        targetDgv.RowHeadersDefaultCellStyle.Font      = font;
        targetDgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        targetDgv.RowHeadersWidthSizeMode               = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        targetDgv.AutoSizeRowsMode                      = DataGridViewAutoSizeRowsMode.None;
        targetDgv.RowTemplate.Height                    = ROW_HEIGHT + V_TEXT_PADDING;
        targetDgv.RowHeadersWidth                       = rowHeaderWidth;

        foreach (DataGridViewRow row in targetDgv.Rows)
            row.Height = ROW_HEIGHT + V_TEXT_PADDING;

        targetDgv.DefaultCellStyle.Font      = font;
        targetDgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        targetDgv.SelectionMode              = DataGridViewSelectionMode.CellSelect;

        foreach (DataGridViewRow row in targetDgv.Rows)
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style.ForeColor          = defaultForeColour;
                cell.Style.BackColor          = defaultBackColour;
                cell.Style.SelectionForeColor = defaultSelectionForeColour;
                cell.Style.SelectionBackColor = defaultSelectionBackColour;
            }

        targetDgv.BackgroundColor    = SystemColors.Control;
        targetDgv.GridColor          = Color.LightGray;
        targetDgv.BorderStyle        = BorderStyle.Fixed3D;
        targetDgv.ScrollBars         = ScrollBars;
        targetDgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        targetDgv.AllowUserToAddRows         = false;
        targetDgv.AllowUserToDeleteRows      = false;
        targetDgv.AllowUserToOrderColumns    = false;
        targetDgv.AllowUserToResizeColumns   = false;
        targetDgv.AllowUserToResizeRows      = false;
        targetDgv.ReadOnly                   = false;
        targetDgv.ShowCellToolTips           = false;
        targetDgv.ClipboardCopyMode          = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

        // Removes the row-header triangle that appears when row height > 16
        targetDgv.RowHeadersDefaultCellStyle.Padding = new Padding(1, 0, 0, 4);

        targetDgv.Refresh();
    }

    public void SetNumberFormat_v1(DgvNumFormat format)
    {
        format.Update();

        // Consistency override: always reapply all formats
        format.Target = FormatTarget.All;

        if (format.Target == FormatTarget.None)
            return;

        if (format.Target == FormatTarget.All || format.Target == FormatTarget.AllHeaders || format.Target == FormatTarget.RowHeaders)
        {
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (double.TryParse(dgv.Rows[i].HeaderCell.Value?.ToString(), out double doubleValue))
                    dgv.Rows[i].HeaderCell.Value = doubleValue.ToString(format.RowHdrFormat);
            }
            dgv.RowHeadersDefaultCellStyle.Format = format.RowHdrFormat;
        }

        if (format.Target == FormatTarget.All || format.Target == FormatTarget.AllHeaders || format.Target == FormatTarget.ColHeaders)
        {
            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                if (double.TryParse(dgv.Columns[i].HeaderText?.ToString(), out double doubleValue))
                    dgv.Columns[i].HeaderText = doubleValue.ToString(format.ColHdrFormat);
            }
            dgv.ColumnHeadersDefaultCellStyle.Format = format.ColHdrFormat;
        }

        if (format.Target == FormatTarget.All || format.Target == FormatTarget.Cells)
        {
            dgv.DefaultCellStyle.Format = format.CellFormat;
        }
    }

    [Obsolete("Do not use — thread-marshalling makes this extremely slow. Use SetNumberFormat_v1.")]
    public void SetNumberFormat_v2(DgvNumFormat format)
    {
        Task.Run(() =>
        {
            bool targetAll           = format.Target == FormatTarget.All;
            bool targetHeaders       = targetAll || format.Target == FormatTarget.AllHeaders;
            bool targetRowHeaders    = targetAll || format.Target == FormatTarget.RowHeaders;
            bool targetColumnHeaders = targetAll || format.Target == FormatTarget.ColHeaders;
            bool targetCells        = targetAll || format.Target == FormatTarget.Cells;

            var rowHeaderUpdates    = new List<(DataGridViewRow, string)>();
            var columnHeaderUpdates = new List<(DataGridViewColumn, string)>();
            var cellUpdates         = new List<(DataGridViewCell, string)>();

            if (targetHeaders || targetRowHeaders)
            {
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (double.TryParse(row.HeaderCell.Value?.ToString(), out double doubleValue))
                        rowHeaderUpdates.Add((row, doubleValue.ToString(format.RowHdrFormat)));
                }
            }

            if (targetHeaders || targetColumnHeaders)
            {
                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    if (double.TryParse(column.HeaderText?.ToString(), out double doubleValue))
                        columnHeaderUpdates.Add((column, doubleValue.ToString(format.ColHdrFormat)));
                }
            }

            if (targetCells)
            {
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (double.TryParse(cell.Value?.ToString(), out double doubleValue))
                            cellUpdates.Add((cell, format.CellFormat));
                    }
                }
            }

            dgv.Invoke(new Action(() =>
            {
                foreach (var (row, value) in rowHeaderUpdates)
                    row.HeaderCell.Value = value;

                foreach (var (column, value) in columnHeaderUpdates)
                    column.HeaderText = value;

                foreach (var (cell, formatStr) in cellUpdates)
                    cell.Style.Format = formatStr;
            }));
        });
    }

    [Obsolete("Not called from active code; SetCellWidths() (no-args) supersedes this.")]
    private void SetCellWidths(DgvData e)
    {
        if (SizeChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - GetHeaderCellWidths()");

        int rowHdrWidth = 0;
        int colHdrWidth = 0;

        foreach (string s in e.RowHeadersText)
        {
            Size textSize = TextRenderer.MeasureText(s, Font);
            if ((textSize.Width + H_TEXT_PADDING) > rowHdrWidth)
                rowHdrWidth = textSize.Width + H_TEXT_PADDING;
        }

        foreach (string s in e.ColHeadersText)
        {
            Size textSize = TextRenderer.MeasureText(s, Font);
            if ((textSize.Width + H_TEXT_PADDING) > colHdrWidth)
                colHdrWidth = textSize.Width + H_TEXT_PADDING;
        }

        rowHdrWidth = System.Math.Max(rowHdrWidth, MINIMUM_ROW_HEADER_WIDTH);
        colHdrWidth = System.Math.Max(System.Math.Max(ColumnWidth, colHdrWidth), MINIMUM_COLUMN_WIDTH);

        dgv.RowHeadersWidth = rowHdrWidth;
        foreach (DataGridViewColumn column in dgv.Columns) column.Width = colHdrWidth;

        SetDgvSize();

        if (UseMyScrollBars) dgvHeaders?.SyncFromMainDgv();
    }

    private void SetCellWidths_NoHeaders()
    {
        if (SizeChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - AutoSetDgvCellWidths()");

        int rowHdrWidth  = 0;
        int colHdrWidth  = 0;
        int dgvCellWidth = 0;
        string s;

        foreach (DataGridViewRow row in dgv.Rows)
        {
            s = row.HeaderCell.Value?.ToString() ?? string.Empty;
            double.TryParse(s, out double cellValue);
            s = cellValue.ToString(dgvNumFormat.RowHdrFormat);
            Size textSize = TextRenderer.MeasureText(s, Font);
            if ((textSize.Width + H_TEXT_PADDING) > rowHdrWidth)
                rowHdrWidth = textSize.Width + H_TEXT_PADDING;
        }

        foreach (DataGridViewColumn column in dgv.Columns)
        {
            s = column.HeaderText?.ToString() ?? string.Empty;
            double.TryParse(s, out double cellValue);
            s = cellValue.ToString(dgvNumFormat.ColHdrFormat);
            Size textSize = TextRenderer.MeasureText(s, Font);
            if ((textSize.Width + H_TEXT_PADDING) > colHdrWidth)
                colHdrWidth = textSize.Width + H_TEXT_PADDING;
        }

        for (int i = 0; i < dgv.Rows.Count; i++)
        {
            for (int j = 0; j < dgv.Columns.Count; j++)
            {
                DataGridViewCell cell = dgv.Rows[i].Cells[j];
                s = cell.Value?.ToString() ?? string.Empty;
                if (!s.Equals(string.Empty))
                    s = double.Parse(s).ToString(dgvNumFormat.CellFormat);
                Size textSize = TextRenderer.MeasureText(s, cell.InheritedStyle.Font);
                if ((textSize.Width + H_TEXT_PADDING) > dgvCellWidth)
                    dgvCellWidth = textSize.Width + H_TEXT_PADDING;
            }
        }

        rowHdrWidth = System.Math.Max(rowHdrWidth, MINIMUM_ROW_HEADER_WIDTH);
        colHdrWidth = System.Math.Max(System.Math.Max(dgvCellWidth, colHdrWidth), MINIMUM_COLUMN_WIDTH);

        dgv.RowHeadersWidth = this.rowHeaderWidth;
        foreach (DataGridViewColumn column in dgv.Columns) column.Width = colHdrWidth;

        SetDgvSize();

        if (UseMyScrollBars) dgvHeaders?.SyncFromMainDgv();
    }

    public void SetCellWidths()
    {
        if (SizeChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - AutoSetDgvCellWidths()");

        int rowHdrWidth  = 0;
        int colHdrWidth  = 0;
        int dgvCellWidth = 0;
        string[] rows, cols;

        if (UseMyScrollBars)
        {
            rows = dgvHeaders.ReadRowHeaders();
            cols = dgvHeaders.ReadColHeaders();
        }
        else
        {
            rows = Array.ConvertAll(ReadRowHeaders(), x => x.ToString(dgvNumFormat.RowHdrFormat));
            cols = Array.ConvertAll(ReadColHeaders(), x => x.ToString(dgvNumFormat.ColHdrFormat));
        }

        if (rows != null)
            foreach (string s in rows)
            {
                Size textSize = TextRenderer.MeasureText(s, Font);
                if ((textSize.Width + H_TEXT_PADDING) > rowHdrWidth)
                    rowHdrWidth = textSize.Width + H_TEXT_PADDING;
            }

        if (cols != null)
            foreach (string s in cols)
            {
                Size textSize = TextRenderer.MeasureText(s, Font);
                if ((textSize.Width + H_TEXT_PADDING) > colHdrWidth)
                    colHdrWidth = textSize.Width + H_TEXT_PADDING;
            }

        for (int i = 0; i < dgv.Rows.Count; i++)
        {
            for (int j = 0; j < dgv.Columns.Count; j++)
            {
                DataGridViewCell cell = dgv.Rows[i].Cells[j];
                string s = cell.Value?.ToString() ?? string.Empty;
                if (!s.Equals(string.Empty))
                    s = double.Parse(s).ToString(dgvNumFormat.CellFormat);
                Size textSize = TextRenderer.MeasureText(s, cell.InheritedStyle.Font);
                if ((textSize.Width + H_TEXT_PADDING) > dgvCellWidth)
                    dgvCellWidth = textSize.Width + H_TEXT_PADDING;
            }
        }

        rowHdrWidth = System.Math.Max(rowHdrWidth, MINIMUM_ROW_HEADER_WIDTH);
        colHdrWidth = System.Math.Max(System.Math.Max(dgvCellWidth, colHdrWidth), MINIMUM_COLUMN_WIDTH);

        dgv.RowHeadersWidth = rowHdrWidth;
        foreach (DataGridViewColumn column in dgv.Columns) column.Width = colHdrWidth;

        SetDgvSize();

        if (UseMyScrollBars) dgvHeaders?.SyncFromMainDgv();
    }

    // Convenience method for loading a complete table when floating headers are in use.
    // Pre-populates the header DGVs BEFORE calling WriteToDataGridView so that SetCellWidths()
    // inside Refresh(All) can correctly measure the header text on its first pass.
    // rowHeadersText / colHeadersText default to the numeric string representation of the axis arrays.
    public void LoadTable(double[] rowLabels, double[] colLabels, double[,] values,
                          string[] rowHeadersText = null, string[] colHeadersText = null,
                          RefreshMode refreshMode = RefreshMode.All)
    {
        if (UseMyScrollBars)
        {
            dgvHeaders.WriteScrollBarRowHeaders(rowHeadersText ?? DgvData.ConvertNumericHeadersToText(rowLabels));
            dgvHeaders.WriteScrollBarColHeaders(colHeadersText ?? DgvData.ConvertNumericHeadersToText(colLabels));
        }
        WriteToDataGridView(rowLabels, colLabels, values, refreshMode);
    }

    private void SetDgvSize()
    {
        if (SizeChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - AutoSetDgvSize()");

        if (dgv.Rows.Count == 0)
            return;

        Size size = new Size(Point.Empty);
        size.Width  = dgv.RowHeadersWidth + dgv.Columns[0].Width * dgv.Columns.Count;
        size.Height = dgv.ColumnHeadersHeight + dgv.Rows[0].Height * dgv.Rows.Count;

        size.Width  += H_BORDER_PADDING;
        size.Height += V_BORDER_PADDING;

        dgv.Size = size;
    }

    private void HideZeros()
    {
        for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < dt.Columns.Count; columnIndex++)
            {
                try
                {
                    double value = Convert.ToDouble(dt.Rows[rowIndex][columnIndex]);
                    if (value != 0.0)
                        continue;

                    dgv.Rows[rowIndex].Cells[columnIndex].Style.ForeColor          = defaultBackColour;
                    dgv.Rows[rowIndex].Cells[columnIndex].Style.BackColor          = defaultBackColour;
                    dgv.Rows[rowIndex].Cells[columnIndex].Style.SelectionForeColor = SystemColors.Highlight;
                    dgv.Rows[rowIndex].Cells[columnIndex].Style.SelectionBackColor = SystemColors.Highlight;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}");
                }
            }
        }
    }

    // Colour — delegates to CellColorizer

    public void SetCellColour(ColourScheme colourScheme)
    {
        if (!DgvHasData)
            return;

        ColourTheme = colourScheme;
        CellColorizer.SetCellColour(dgv, dt, colourScheme);
    }

    // Data Table Write Functions

    public void WriteHeaders(double[] rowLabels, double[] columnLabels)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - WriteHeaders()");

        if (rowLabels.Length != dt.Rows.Count || columnLabels.Length != dt.Columns.Count)
            ReDimensionDataTable_v2(rowLabels.Length, columnLabels.Length);

        WriteRowHeaderLabels(rowLabels);
        WriteColHeaderLabels(columnLabels);
    }

    public void WriteRowHeaderLabels(double[] rowLabels, string format = "999")
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - WriteRowHeaderLabels()");

        if (rowLabels.Length != dt.Rows.Count)
            return;

        if (format == "999")
            for (int i = 0; i < rowLabels.Length; i++)
                dgv.Rows[i].HeaderCell.Value = rowLabels[i].ToString();
        else
            for (int i = 0; i < rowLabels.Length; i++)
                dgv.Rows[i].HeaderCell.Value = rowLabels[i].ToString(format);
    }

    public void WriteColHeaderLabels(double[] columnLabels, string format = "999")
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - WriteColHeaderLabels()");

        if (format == "999")
            for (int i = 0; i < columnLabels.Length && i < dgv.Columns.Count; i++)
                dgv.Columns[i].HeaderText = columnLabels[i].ToString();
        else
            for (int i = 0; i < columnLabels.Length && i < dgv.Columns.Count; i++)
                dgv.Columns[i].HeaderText = columnLabels[i].ToString(format);
    }

    public void WriteToDataGridView(double[] rowLabels, double[] columnLabels, double[,] values, RefreshMode refreshMode = RefreshMode.All)
    {
        DgvHasData = false;

        this.dgv.SuspendLayout();

        Events_CellDataAndSelectionChanged_Pause();

        ReDimensionDataTable_v2(rowLabels.Length, columnLabels.Length);
        WriteToDataTable(values);
        WriteRowHeaderLabels(rowLabels);
        WriteColHeaderLabels(columnLabels);

        DgvHasData = true;

        Refresh(refreshMode);

        this.dgv.ResumeLayout(true);

        Events_CellDataAndSelectionChanged_Resume(true);
    }

    public void WriteToDataGridView(double[] rowLabels, double[] columnLabels, DataTable values, RefreshMode refreshMode = RefreshMode.All)
    {
        DgvHasData = false;

        this.dgv.SuspendLayout();

        Events_CellDataAndSelectionChanged_Pause();

        ReDimensionDataTable_v2(rowLabels.Length, columnLabels.Length);
        WriteToDataTable(values);
        WriteRowHeaderLabels(rowLabels);
        WriteColHeaderLabels(columnLabels);

        Refresh(refreshMode);

        this.dgv.ResumeLayout(true);

        DgvHasData = true;

        Events_CellDataAndSelectionChanged_Resume(true);
    }

    public void WriteToDataTable(double[,] values)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - WriteToDataTable1()");

        int rowLength    = values.GetLength(0);
        int columnLength = values.GetLength(1);

        if (rowLength < 1 || columnLength < 1)
            return;

        if (rowLength != dt.Rows.Count || columnLength != dt.Columns.Count)
            return;

        for (int i = 0; i < rowLength; i++)
            for (int j = 0; j < columnLength; j++)
                dt.Rows[i][j] = values[i, j];
    }

    public void WriteToDataTable(DataTable values)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - WriteToDataTable2()");

        dt = values.Clone();

        foreach (DataRow row in values.Rows)
            dt.ImportRow(row);
    }

    public void WriteDt(int row, int col, double value)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - WriteDt() X{col} Y{row}");

        if (row >= 0 && row < dt.Rows.Count && col >= 0 && col < dt.Columns.Count)
            dt.Rows[row][col] = value;
        else
            throw new IndexOutOfRangeException("Row or column index is out of range.");
    }

    // Data Table Read Functions

    public double[] ReadRowHeaders()
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadRowHeaders()");

        if (!DgvHasData) return null;

        double[] rowLabels = new double[dgv.Rows.Count];

        for (int i = 0; i < dgv.Rows.Count; i++)
        {
            if (dgv.Rows[i].HeaderCell.Value != null)
            {
                if (double.TryParse(dgv.Rows[i].HeaderCell.Value.ToString(), out double result))
                    rowLabels[i] = result;
            }
            else
            {
                return null;
            }
        }

        return rowLabels;
    }

    public double ReadRowHeaderAtIndex(int index)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadRowHeaderAtIndex()");

        if (!DgvHasData || index == -1) return -1;

        if (dgv.Rows[index].HeaderCell.Value != null)
        {
            if (double.TryParse(dgv.Rows[index].HeaderCell.Value.ToString(), out double result))
                return result;
        }
        else
        {
            return double.NaN;
        }

        return double.NaN;
    }

    public double[] ReadColHeaders()
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadColHeaders()");

        if (!DgvHasData) return null;

        double[] columnLabels = new double[dgv.Columns.Count];

        for (int i = 0; i < dgv.Columns.Count; i++)
        {
            if (dgv.Columns[i].HeaderText != null)
            {
                if (double.TryParse(dgv.Columns[i].HeaderText, out double result))
                    columnLabels[i] = result;
            }
            else
            {
                return null;
            }
        }

        return columnLabels;
    }

    public double ReadColumnHeaderAtIndex(int index)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadColumnHeaderAtIndex()");

        if (!DgvHasData || index == -1) return -1;

        if (dgv.Columns[index].HeaderText != null)
        {
            if (double.TryParse(dgv.Columns[index].HeaderText, out double value))
                return value;
        }
        else
        {
            return double.NaN;
        }

        return double.NaN;
    }

    public double[,] ReadDataTable()
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadDataTable1()");

        if (!DgvHasData) return null;

        int rowLength    = dt.Rows.Count;
        int columnLength = dt.Columns.Count;
        double[,] values = new double[rowLength, columnLength];

        for (int i = 0; i < rowLength; i++)
            for (int j = 0; j < columnLength; j++)
                if (dt.Rows[i][j] != DBNull.Value)
                    values[i, j] = double.Parse(dt.Rows[i][j].ToString());

        return values;
    }

    public double[,] ReadDataTable(DataTable dataTable)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadDataTable2()");

        int rowLength    = dataTable.Rows.Count;
        int columnLength = dataTable.Columns.Count;
        double[,] values = new double[rowLength, columnLength];

        for (int i = 0; i < rowLength; i++)
            for (int j = 0; j < columnLength; j++)
                if (dataTable.Rows[i][j] != DBNull.Value)
                    values[i, j] = double.Parse(dataTable.Rows[i][j].ToString());

        return values;
    }

    public double[,] ReadDataGridView()
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadDataGridView()");

        if (!DgvHasData) return null;

        double[] rowHeaders    = ReadRowHeaders();
        double[] columnHeaders = ReadColHeaders();
        double[,] dataTable   = ReadDataTable();

        int rowCount    = rowHeaders.Length;
        int columnCount = columnHeaders.Length;

        double[,] combinedArray = new double[rowCount, columnCount + 1];

        for (int i = 0; i < rowCount; i++)
            combinedArray[i, 0] = rowHeaders[i];

        for (int j = 0; j < columnCount; j++)
            combinedArray[0, j + 1] = columnHeaders[j];

        for (int i = 0; i < rowCount; i++)
            for (int j = 0; j < columnCount; j++)
                combinedArray[i, j + 1] = dataTable[i, j];

        return combinedArray;
    }

    public double ReadCellAtAddress(Point addr)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadCellAtAddress()");

        if (addr.Y > dgv.Rows.Count || addr.X > dgv.Columns.Count)
            return double.NaN;

        return (double)dt.Rows[addr.Y][addr.X];
    }

    public double ReadDt(int row, int col)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReadDt() X{col} Y{row}");

        if (row >= 0 && row < dt.Rows.Count && col >= 0 && col < dt.Columns.Count)
        {
            if (dt.Rows[row][col] == DBNull.Value)
                return 0.0;
            else
                return (double)dt.Rows[row][col];
        }
        else
        {
            throw new IndexOutOfRangeException("Row or column index is out of range.");
        }
    }

    // DGV General Functions

    private void AssignInstanceNames()
    {
        myEvents.InstanceName     = InstanceName;
        incDecTask.InstanceName   = InstanceName;
        dgvNumFormat.InstanceName = InstanceName;
        if (CopyPasteEnabled) copy.InstanceName  = InstanceName;
        if (CopyPasteEnabled) paste.InstanceName = InstanceName;
        if (UndoEnabled)      undo.InstanceName  = InstanceName;
    }

    private void EnableDoubleBuffering()
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, dgv, new object[] { true });

        dgv.GetType().InvokeMember("SetStyle",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
            null, dgv, new object[]
            {
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint, true
            });
    }

    public void ClearDataTable()
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ClearDataTable()");

        for (int i = 0; i < dt.Rows.Count; i++)
            for (int j = 0; j < dt.Columns.Count; j++)
                dt.Rows[i][j] = DBNull.Value;
    }

    public void ResetDataTable()
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ResetDataTable()");

        ReDimensionDataTable_v2(1, 1);

        dgv.Rows[0].HeaderCell.Value = String.Empty;
        dgv.Columns[0].HeaderText   = "0";
        dt.Rows[0][0]               = DBNull.Value;

        DgvHasData             = false;
        dgvNumFormat.CelLckOut = false;
    }

    [Obsolete("Superseded by ReDimensionDataTable_v2")]
    public bool ReDimensionDataTable_v1(int rowLabelLength, int columnLabelLength)
    {
        bool dimensionChanged = false;

        for (int i = 0; i < columnLabelLength; i++)
        {
            string columnName = i.ToString();
            if (!dt.Columns.Contains(columnName))
            {
                dt.Columns.Add(columnName, typeof(double));
                dimensionChanged = true;
            }
        }

        while (dt.Rows.Count < rowLabelLength)
        {
            dt.Rows.Add(new object[columnLabelLength]);
            dimensionChanged = true;
        }

        for (int i = dt.Columns.Count - 1; i >= columnLabelLength; i--)
        {
            dt.Columns.RemoveAt(i);
            dimensionChanged = true;
        }

        for (int i = dt.Rows.Count - 1; i >= rowLabelLength; i--)
        {
            dt.Rows.RemoveAt(i);
            dimensionChanged = true;
        }

        return dimensionChanged;
    }

    public bool ReDimensionDataTable_v2(int rowLength, int colLength)
    {
        if (DgvData_Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ReDimensionDataTable_v2()");

        if (rowLength == dt.Rows.Count && colLength == dt.Columns.Count)
            return false;

        dgv.DataSource = null;

        dt = new DataTable();

        for (int i = 0; i < colLength; i++)
            dt.Columns.Add(i.ToString(), typeof(double));

        for (int i = 0; i < rowLength; i++)
        {
            DataRow newRow = dt.NewRow();
            dt.Rows.Add(newRow);
        }

        dgv.DataSource = dt;

        return true;
    }

    public void AdjustDecimalPlaces(DpDirection direction)
    {
        if (!DgvHasData)
            return;

        dgvNumFormat.CelLckOut = false;

        string s = NumberFormatter.GetNumberFormat(dgv.Rows[0].Cells[0]);

        string[] parts = s.Split('N');
        int dp = Convert.ToInt32(parts[1]);

        if (direction == DpDirection.Increment)
            dp++;

        if (direction == DpDirection.Decrement)
            if (dp > 0)
                dp--;

        s = "N" + dp.ToString();

        dgvNumFormat.CellFormat = s;

        Refresh(RefreshMode.DpAdjust);

        dgvNumFormat.CelLckOut = true;
    }

    public void ClearSelection()
    {
        dgv.ClearSelection();
    }

    public Point GetTopLeftCellAddress(DataGridViewSelectedCellCollection selectedCells)
    {
        if (selectedCells.Count == 0)
            return new Point(-1, -1);

        int minRowIndex    = int.MaxValue;
        int minColumnIndex = int.MaxValue;

        foreach (DataGridViewCell cell in selectedCells)
        {
            if (cell.RowIndex < minRowIndex)       minRowIndex    = cell.RowIndex;
            if (cell.ColumnIndex < minColumnIndex) minColumnIndex = cell.ColumnIndex;
        }

        if (SelectionChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - GetTopLeftCellAddress() X{minColumnIndex} Y{minRowIndex}");

        return new Point(minColumnIndex, minRowIndex);
    }

    public void SetDgvCurrentCell(int row, int col)
    {
        dgv.CurrentCell = dgv.Rows[row].Cells[col];
    }

    // External class event handlers (input)

    private void Paste_Completed_NDR(object sender, DgvData e)
    {
        if (UndoEnabled && undo.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Call Undo_Set() from Paste_Completed_NDR()");

        if (EventDebug && UndoEnabled && !undo.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Paste_Completed_NDR()");

        myEvents.Req_DgvDataChanged_ToHeaders_Event(e);
        SetCellWidths();
        Undo_Set(myEvents.BuildEventArgs_DgvDataChanged_Event());
        myEvents.Req_DgvDataChanged_Event();
    }

    private void Undo_Completed_NDR(object sender, DgvData e)
    {
        if (undo.Debug || EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Undo_Completed_NDR()");

        myEvents.Req_DgvDataChanged_Event(e);
        myEvents.Req_DgvDataChanged_ToHeaders_Event(e);
        SetCellWidths();
    }

    private void IncDec_Incremental_NDR(object sender, EventArgs e)
    {
        if (EventDebug || incDecTask.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - IncDec_Incremental_NDR()");

        myEvents.Req_DgvDataChanged_Event();
    }

    private void IncDec_Completed_NDR(object sender, EventArgs e)
    {
        if (undo.Debug || incDecTask.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Call Undo_Set() from IncDec_Completed_NDR()");

        if (EventDebug && !undo.Debug && !incDecTask.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - IncDec_Completed_NDR()");

        myEvents.Req_DgvDataChanged_Event();
    }

    // DGV event intermediaries

    private void DgvMyEvents_CellValueOrDtRowChanged()
    {
        if (UndoEnabled && !undo.InProgress && !redo.InProgress && CopyPasteEnabled && !paste.InProgress && !incDecTask.Mode.Enabled && !Z_RemoteValuesChanging)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_MyEvents_CellValueOrDtRowChanged()");

            myEvents.Req_DgvDataChanged_Event();
        }
    }

    private void DgvMyEvents_SelectionChanged()
    {
        myEvents.Req_DgvSelectionChanged_Event();
    }

    // MyEvents output (subscribed to by this class)

    private void MyEvents_Dgv_NDR_Debounced(object sender, DgvData e)
    {
        if (UndoEnabled && undo.Debug && !undo.InProgress)
            Console.WriteLine($"{InstanceName} - {ClassName} - Undo_Set() from MyEvents_Raise_Dgv_NDR_Debounced_Event()");

        if (EventDebug && UndoEnabled && !undo.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_NDR_Debounced()");

        Undo_Set(e);
    }

    private void MyEvents_Dgv_NDR_Immediate(object sender, DgvData e)
    {
        // Reserved for future use — redo stack management
    }

    private void MyEvents_Dgv_NewSize_Intermittent(object sender, DgvEvents.SizeEventArgs e)
    {
        // Reserved for layout updates on intermittent size change
    }

    // Events

    public void Events_CellDataAndSelectionChanged_Pause()
    {
        if (EventDebug || DataChangedDebug || SelectionChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Events_CellDataAndSelectionChanged_Pause()");

        dgv.CellValueChanged -= DgvCellValueChanged;
        dt.RowChanged        -= DtRowChanged;
        dgv.SelectionChanged -= DgvSelectionChanged;
    }

    public void Events_CellDataAndSelectionChanged_Resume(bool bypass = false)
    {
        if (EventDebug || DataChangedDebug || SelectionChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Events_CellDataAndSelectionChanged_Resume()");

        // Ensure exactly one subscription for each event
        dgv.CellValueChanged -= DgvCellValueChanged;
        dgv.CellValueChanged += DgvCellValueChanged;
        dt.RowChanged        -= DtRowChanged;
        dt.RowChanged        += DtRowChanged;
        dgv.SelectionChanged -= DgvSelectionChanged;
        dgv.SelectionChanged += DgvSelectionChanged;

        myEvents.Req_DgvDataChanged_Event();
        myEvents.Req_DgvSizeChanged_Event();
        if (UseMyScrollBars)
            myEvents.Req_DgvDataChanged_ToHeaders_Event(myEvents.BuildEventArgs_DgvDataChanged_Event());
    }

    public void DgvSelectionChanged(object sender, EventArgs e)
    {
        if (EventDebug || SelectionChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvSelectionChanged() Current cell X{dgv.CurrentCell.ColumnIndex} Y{dgv.CurrentCell.RowIndex}");

        if (keepCellsSelectedAfterEdit)
        {
            dgv.SelectionChanged -= DgvSelectionChanged;
            myEvents.Pause_SelectionFromDgvCtrl();

            dgv.ClearSelection();
            dgv.CurrentCell = currentCell_Copy;

            foreach (DataGridViewCell cell in selectedCellsCollection_Copy)
                dgv.Rows[cell.RowIndex].Cells[cell.ColumnIndex].Selected = true;

            dgv.SelectionChanged += DgvSelectionChanged;
            myEvents.Resume_SelectionFromDgvCtrl();

            keepCellsSelectedAfterEdit = false;
        }

        SelectedCellAddress = GetTopLeftCellAddress(dgv.SelectedCells);

        DgvMyEvents_SelectionChanged();
    }

    private void DgvSizeChanged(object sender, EventArgs e)
    {
        if (!DgvHasData)
            return;

        if (SizeChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvSizeChanged()");

        myEvents.Req_DgvSizeChanged_Event();
    }

    private void DtRowChanged(object sender, DataRowChangeEventArgs e)
    {
        if (EventDebug || DataChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DtRowChanged()");

        DgvMyEvents_CellValueOrDtRowChanged();
    }

    private void DgvCellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if (EventDebug || DataChangedDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvCellValueChanged()");

        DgvMyEvents_CellValueOrDtRowChanged();
    }

    private void DgvMouseDown(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (EventDebug || DebugMouse)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvMouseDown() Button {e.Button}");

        if (e.Button == MouseButtons.Right)
        {
            if (dgv.SelectedCells.Count <= 1)
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    foreach (DataGridViewCell cell in dgv.SelectedCells)
                        cell.Selected = false;

                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;

                    if (DebugMouse)
                        Console.WriteLine($"{InstanceName} - {ClassName} - DgvMouseDown() Right click cell selected X{dgv.CurrentCell.ColumnIndex} Y{dgv.CurrentCell.RowIndex}");
                }
            }
        }

        userCellEditPending = false;
    }

    private void DgvMouseUp(object sender, MouseEventArgs e)
    {
        if (EventDebug || DebugMouse)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvMouseUp() Button {e.Button}");

        dgv.ReadOnly = false;
    }

    private void DgvEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvEditingControlShowing()");

        if (DgvHasData)
        {
            if (Keyboard.IsKeyDown(Key.Add) || Keyboard.IsKeyDown(Key.Subtract))
            {
                dgv.ReadOnly = true;

                if (!incDecTask.Mode.Enabled)
                    Dgv_IncDecKeyDown();

                return;
            }
        }

        dgv.EditingControl.KeyPress -= DgvEditingControl_KeyPress;
        dgv.EditingControl.KeyPress += DgvEditingControl_KeyPress;
    }

    private void DgvEditingControl_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvEditingControl_KeyPress()");

        Control editingControl = (Control)sender;

        string pattern = @"^-?(0|[1-9]\d*)(\.\d+)?$";

        double.TryParse(editingControl.Text, out double result);

        if (!Regex.IsMatch(result.ToString() + e.KeyChar, pattern))
            e.Handled = true;

        userCellEditPending = true;
    }

    private void DgvCellValidating(object sender, CancelEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvCellValidating()");

        if (userCellEditPending)
        {
            myEvents.Pause_All();

            if (dgv.SelectedCells.Count >= 2 && dgv.CurrentCell.EditedFormattedValue != dgv.CurrentCell.Value)
            {
                double value = double.Parse(dgv.CurrentCell.EditedFormattedValue.ToString());

                foreach (DataGridViewCell cell in dgv.SelectedCells)
                    WriteDt(cell.RowIndex, cell.ColumnIndex, value);

                keepCellsSelectedAfterEdit     = true;
                selectedCellsCollection_Copy   = dgv.SelectedCells;
                currentCell_Copy               = dgv.CurrentCell;
            }
        }
    }

    private void DgvCellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvCellEndEdit()");

        if (userCellEditPending)
            SetCellWidths();

        userCellEditPending = false;

        myEvents.Resume_All();
    }

    private void DgvKeyUp(object sender, KeyEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvKeyUp()");

        if (incDecTask.Mode.Enabled)
            Dgv_IncDecKeyUp();

        dgv.ReadOnly = false;
    }

    private void DgvKeyDown(object sender, KeyEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvKeyDown()");

        if (DgvHasData)
        {
            if (Keyboard.IsKeyDown(Key.Add) || Keyboard.IsKeyDown(Key.Subtract))
            {
                dgv.ReadOnly = true;

                if (!incDecTask.Mode.Enabled)
                    Dgv_IncDecKeyDown();
            }
        }

        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.V))
            paste.ParseClipboardToDgv(this, Paste.eMode.PasteToCurrentCell, Paste.eDataSource.ClipBoard);
    }

    private void DgvTableCellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvTableCellClick()");

        dgv.ReadOnly = true;

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && e.RowIndex != -1 && e.ColumnIndex != -1)
        {
            dgv.SelectionChanged -= DgvSelectionChanged;

            dgv.ClearSelection();
            dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;

            dgv.SelectionChanged += DgvSelectionChanged;
        }
    }

    private void DgvTableCellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        dgv.ReadOnly = true;
    }

    private void RowHeader_CellClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - RowHeader_CellClick()");

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
        {
            dgv.ClearSelection();
            selectedRows.Clear();
        }

        if (selectedRows.Contains(e.RowIndex))
        {
            for (int i = 0; i < dgv.Columns.Count; i++)
                dgv.Rows[e.RowIndex].Cells[i].Selected = false;
            while (selectedRows.Remove(e.RowIndex)) ;
            return;
        }

        for (int i = 0; i < dgv.Columns.Count; i++)
            dgv.Rows[e.RowIndex].Cells[i].Selected = true;

        selectedRows.Add(e.RowIndex);
    }

    private void ColHeader_CellClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (EventDebug)
            Console.WriteLine($"{InstanceName} - {ClassName} - ColHeader_CellClick()");

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
        {
            dgv.ClearSelection();
            selectedCols.Clear();
        }

        if (selectedCols.Contains(e.ColumnIndex))
        {
            for (int i = 0; i < dgv.Rows.Count; i++)
                dgv.Rows[i].Cells[e.ColumnIndex].Selected = false;
            while (selectedCols.Remove(e.ColumnIndex)) ;
            return;
        }

        for (int i = 0; i < dgv.Rows.Count; i++)
            dgv.Rows[i].Cells[e.ColumnIndex].Selected = true;

        selectedCols.Add(e.ColumnIndex);
    }

    private void ScrollBarCtrl_ResetRowHeaderPosition(object sender, EventArgs e)
    {
        dgvHeaders.ResetRowHeaderPosition();
    }

    private void ScrollBarCtrl_ResetColHeaderPosition(object sender, EventArgs e)
    {
        dgvHeaders.ResetColHeaderPosition();
    }

    private void ScrollBarCtrl_PushNewHeaderPosition(object sender, (bool, int) e)
    {
        dgvHeaders.PushNewHeaderLocation(e.Item1, e.Item2);
    }

    // IncDec key handlers

    private void Dgv_IncDecKeyDown()
    {
        if (incDecTask.Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_IncDecKeyDown()");

        Events_CellDataAndSelectionChanged_Pause();
        incDecTask.Start();
    }

    private void Dgv_IncDecKeyUp()
    {
        if (incDecTask.Debug)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_IncDecKeyUp()");
            Console.WriteLine($"{InstanceName} - {ClassName} - Stop requested");
        }

        incDecTask.StopRequest = true;
        Events_CellDataAndSelectionChanged_Resume();
    }
}
