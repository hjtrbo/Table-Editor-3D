using System;
using System.Drawing;
using System.Windows.Forms;
using TableEditor.Clipboard;
using Timers;

namespace TableEditor.DataGrid;

// Provides debounced, intermittent, and immediate event wrappers around raw DataGridView
// events. Subscribers receive structured event args rather than raw WinForms events.
// Renamed from MyEvents to DgvEvents for clarity.
public class DgvEvents
{
    public string ClassName { get; set; } = "DgvEvents";
    public string InstanceName
    {
        get { return instanceName; }
        set { instanceName = value; AssignTimerInstanceNames(); }
    }
    private bool PauseAllEvents { get; set; }
    private bool PauseDataEventsFromGraph3d { get; set; }
    private bool PauseSelnEventsFromGraph3d { get; set; }
    private bool PauseSelnEventsToGraph3d { get; set; }
    private bool PauseSelnEventsFromDgvCtrl { get; set; }
    public bool DebugAll { get; set; }
    public bool DebugDataChanged { get; set; }
    public bool DebugSizeChanged { get; set; }
    public bool DebugSelnChanged { get; set; }
    public bool DebugMuteHighSpeed { get; set; }
    public bool DebugDbncTmr
    {
        get { return debugDbncTmr; }
        set { debugDbncTmr = value; tmr_DgvDataChanged_Debounced.Debug = value; tmr_SelectionChanged_Debounced.Debug = value; }
    }
    public bool DebugIntTmr
    {
        get { return debugIntTmr; }
        set { debugIntTmr = value; tmr_DgvSizeChanged_Intermittent.Debug = value; tmr_SelectionChanged_Intermittent.Debug = value; }
    }
    public bool NewTablePasted { get; set; } = false;

    // Variables
    DgvCtrl dgvCtrl;
    DgvData dgvData = new DgvData();
    DgvData dgvDataPrev = new DgvData();
    SizeEventArgs sizeEventArgs = new SizeEventArgs();
    SelectEventArgs selectEventArgs = new SelectEventArgs();

    public TimerOffDelay tmr_DgvDataChanged_Debounced;
    public TimerOnDelay  tmr_DgvSizeChanged_Intermittent;
    public TimerOffDelay tmr_SelectionChanged_Debounced;
    public TimerOnDelay  tmr_SelectionChanged_Intermittent;

    bool debugDbncTmr;
    bool debugIntTmr;
    string instanceName;

    // Events
    public event EventHandler<DgvData>       DgvDataChanged_Debounced;
    public event EventHandler<DgvData>       DgvDataChanged_Immediate;
    public event EventHandler<SizeEventArgs> DgvSizeChanged_Immediate;
    public event EventHandler<SizeEventArgs> DgvSizeChanged_Intermittent;
    public event EventHandler<SelectEventArgs> DgvSelectionChanged_Immediate;
    public event EventHandler<SelectEventArgs> DgvSelectionChanged_ToGraph3d_Immediate;
    public event EventHandler<SelectEventArgs> DgvSelectionChanged_Intermittent;
    public event EventHandler<SelectEventArgs> DgvSelectionChanged_Debounced;
    public event EventHandler<DgvData>       DgvDataChangedToHeaders;

    public void AssignTimerInstanceNames()
    {
        tmr_DgvDataChanged_Debounced.DebugInstanceName    = instanceName;
        tmr_DgvSizeChanged_Intermittent.DebugInstanceName = instanceName;
        tmr_SelectionChanged_Debounced.DebugInstanceName  = instanceName;
        tmr_SelectionChanged_Intermittent.DebugInstanceName = instanceName;
    }

    public DgvEvents(DgvCtrl dgvCtrl)
    {
        this.dgvCtrl = dgvCtrl;

        tmr_DgvDataChanged_Debounced = new TimerOffDelay
        {
            Preset          = 50,
            UiControl       = dgvCtrl.dgv,
            DebugTimerName  = "tmr_DgvDataChanged_Debounced",
            OnTimingDone    = Raise_DgvDataChanged_Debounced_Event
        };

        tmr_DgvSizeChanged_Intermittent = new TimerOnDelay
        {
            Preset                 = 50,
            UiControl              = dgvCtrl.dgv,
            AutoStop               = true,
            AutoStop_CountsPreset  = 4,
            DebugTimerName         = "tmr_DgvSizeChanged_Intermittent",
            OnTimingDone           = Raise_DgvSizeChanged_Intermittent_Event
        };

        tmr_SelectionChanged_Intermittent = new TimerOnDelay
        {
            Preset                = 50,
            UiControl             = dgvCtrl.dgv,
            AutoStop              = true,
            AutoStop_CountsPreset = 4,
            DebugTimerName        = "tmr_SelectionChanged_Intermittent",
            OnTimingDone          = Raise_DgvSelectionChanged_Intermittent_Event
        };

        tmr_SelectionChanged_Debounced = new TimerOffDelay
        {
            Preset         = 50,
            UiControl      = dgvCtrl.dgv,
            DebugTimerName = "tmr_SelectionChanged_Debounced",
            OnTimingDone   = Raise_DgvSelectionChanged_Debounced_Event
        };
    }

    // Pause / Resume

    public void Pause_All()
    {
        if (!PauseAllEvents && (DebugAll || DebugDataChanged || DebugSizeChanged || DebugSelnChanged || DebugDbncTmr || DebugIntTmr))
            Console.WriteLine($"{InstanceName} - {ClassName} - Pause_All()");

        PauseAllEvents = true;
    }

    public void Resume_All()
    {
        if (DebugAll || DebugDataChanged || DebugSizeChanged || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
            Console.WriteLine($"{InstanceName} - {ClassName} - Resume_All()");

        PauseAllEvents = false;

        Req_DgvDataChanged_Event();
        Req_DgvSizeChanged_Event();
        Req_DgvSelectionChanged_Event();
    }

    public void Pause_DataFromGraph3d()
    {
        if (!PauseDataEventsFromGraph3d && (DebugAll || DebugDataChanged || DebugDbncTmr || DebugIntTmr))
            Console.WriteLine($"{InstanceName} - {ClassName} - Pause_DataFromGraph3d");

        PauseDataEventsFromGraph3d = true;
    }

    public void Resume_DataFromGraph3d()
    {
        if (DebugAll || DebugDataChanged || DebugDbncTmr || DebugIntTmr)
            Console.WriteLine($"{InstanceName} - {ClassName} - Resume_DataFromGraph3d()");

        PauseDataEventsFromGraph3d = false;
    }

    public void Pause_SelectionFromGraph3d()
    {
        if (!PauseSelnEventsFromGraph3d && (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr))
            Console.WriteLine($"{InstanceName} - {ClassName} - Pause_SelectionFromGraph3d()");

        PauseSelnEventsFromGraph3d = true;
    }

    public void Resume_SelectionFromGraph3d()
    {
        if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
            Console.WriteLine($"{InstanceName} - {ClassName} - Resume_SelectionFromGraph3d()");

        PauseSelnEventsFromGraph3d = false;
    }

    public void Pause_SelectionToGraph3d()
    {
        if (!PauseSelnEventsFromGraph3d && (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr))
            Console.WriteLine($"{InstanceName} - {ClassName} - Pause_SelectionToGraph3d()");

        PauseSelnEventsToGraph3d = true;
    }

    public void Resume_SelectionToGraph3d()
    {
        if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
            Console.WriteLine($"{InstanceName} - {ClassName} - Resume_SelectionToGraph3d()");

        PauseSelnEventsToGraph3d = false;
    }

    public void Pause_SelectionFromDgvCtrl()
    {
        if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
            Console.WriteLine($"{InstanceName} - {ClassName} - Pause_SelectionFromDgvCtrl()");

        PauseSelnEventsFromDgvCtrl = true;
    }

    public void Resume_SelectionFromDgvCtrl()
    {
        if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
            Console.WriteLine($"{InstanceName} - {ClassName} - Resume_SelectionFromDgvCtrl()");

        PauseSelnEventsFromDgvCtrl = false;
    }

    // Data Changed

    public void Req_DgvDataChanged_Event()
    {
        if (PauseAllEvents || PauseDataEventsFromGraph3d)
            return;

        if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
            Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvDataChanged_Event()");

        if (DebugAll || (DebugDataChanged && !tmr_DgvDataChanged_Debounced.TimerTiming))
            Console.WriteLine($"{InstanceName} - {ClassName} - Started timer_DgvDataChanged_Debounced");

        tmr_DgvDataChanged_Debounced.Start();

        Raise_DgvDataChanged_Immediate_Event();
    }

    public void Req_DgvDataChanged_Event(DgvData e)
    {
        if (PauseAllEvents || PauseDataEventsFromGraph3d)
            return;

        dgvData = e;

        if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
            Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvDataChanged_Event()");

        if (DebugAll || (DebugDataChanged && !tmr_DgvDataChanged_Debounced.TimerTiming))
            Console.WriteLine($"{InstanceName} - {ClassName} - Started timer_DgvDataChanged_Debounced");

        tmr_DgvDataChanged_Debounced.Start();

        Raise_DgvDataChanged_Immediate_Event();
    }

    public void Req_DgvDataChanged_ToHeaders_Event(DgvData e)
    {
        if (PauseAllEvents || PauseDataEventsFromGraph3d)
            return;

        Raise_DgvDataChanged_ToHeaders_Event(e);
    }

    public DgvData BuildEventArgs_DgvDataChanged_Event()
    {
        DgvData e = new DgvData();

        e.RowHeaders = dgvCtrl.ReadRowHeaders();
        e.ColHeaders = dgvCtrl.ReadColHeaders();
        e.TableData  = dgvCtrl.ReadDataTable();

        e.RowHeaderFormat = dgvCtrl.RowHeaderFormat;
        e.ColHeaderFormat = dgvCtrl.ColHeaderFormat;
        e.TableDataFormat = dgvCtrl.DataTableFormat;

        if (!dgvCtrl.UseMyScrollBars)
        {
            e.RowHeadersText = dgvCtrl.dgvHeaders.ReadRowHeaders();
            e.ColHeadersText = dgvCtrl.dgvHeaders.ReadColHeaders();
        }
        else
        {
            e.RowHeadersText = Array.ConvertAll(dgvCtrl.ReadRowHeaders(), x => x.ToString(dgvCtrl.dgvNumFormat.RowHdrFormat));
            e.ColHeadersText = Array.ConvertAll(dgvCtrl.ReadColHeaders(), x => x.ToString(dgvCtrl.dgvNumFormat.ColHdrFormat));
        }

        dgvDataPrev = e.Copy();

        switch (e.CopyPasteMode)
        {
            case Paste.eMode.PasteTableWithXYAxis:
            case Paste.eMode.PasteTableWithXAxis:
            case Paste.eMode.PasteTableWithYAxis:
            case Paste.eMode.PasteTableWithNoAxis:
                NewTablePasted = true;
                break;
        }

        return e;
    }

    private void Raise_DgvDataChanged_Debounced_Event()
    {
        if (DebugAll || DebugDataChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Raise_DgvDataChanged_Debounced_Event()");

        DgvDataChanged_Debounced?.Invoke(this, BuildEventArgs_DgvDataChanged_Event());
    }

    private void Raise_DgvDataChanged_Immediate_Event()
    {
        if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
            Console.WriteLine($"{InstanceName} - {ClassName} - Raise_DgvDataChanged_Immediate_Event()");

        DgvDataChanged_Immediate?.Invoke(this, BuildEventArgs_DgvDataChanged_Event());
    }

    private void Raise_DgvDataChanged_ToHeaders_Event(DgvData e)
    {
        if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
            Console.WriteLine($"{InstanceName} - {ClassName} - Raise_DgvDataChanged_ToHeaders_Event()");

        DgvDataChangedToHeaders?.Invoke(this, e);
    }

    // Size Changed

    public void Req_DgvSizeChanged_Event()
    {
        if (PauseAllEvents)
            return;

        if (DebugAll || DebugSizeChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvSizeChanged_Event()");

        BuildEventArgs_DgvSizeChanged_Event();

        if ((DebugAll || DebugSizeChanged) && !tmr_DgvSizeChanged_Intermittent.TimerTiming)
            Console.WriteLine($"{InstanceName} - {ClassName} - Started tmr_DgvSizeChanged_Intermittent");

        tmr_DgvSizeChanged_Intermittent.Start();

        Raise_DgvSizeChanged_Immediate_Event();
    }

    private void BuildEventArgs_DgvSizeChanged_Event()
    {
        sizeEventArgs = new SizeEventArgs();
        sizeEventArgs.Sender               = dgvCtrl;
        sizeEventArgs.DgvSize              = dgvCtrl.dgv.Size;
        sizeEventArgs.DgvDisplayRectangle  = dgvCtrl.dgv.DisplayRectangle;
        sizeEventArgs.DgvClientRectangle   = dgvCtrl.dgv.ClientRectangle;
        sizeEventArgs.DgvLocation          = dgvCtrl.dgv.Location;
        sizeEventArgs.DgvRowHeaderWidth    = dgvCtrl.dgv.RowHeadersWidth;
        sizeEventArgs.DgvColumnHeaderWidth = dgvCtrl.dgv.Columns[0].Width;
        sizeEventArgs.DgvColumnWidth       = dgvCtrl.dgv.Columns[0].Width;
        sizeEventArgs.DgvRowHeight         = dgvCtrl.dgv.Rows[0].Height;
        sizeEventArgs.DgvColumnHeaderHeight = dgvCtrl.dgv.ColumnHeadersHeight;

        if (DebugAll || DebugSizeChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvSizeChanged EventArgs built");
    }

    private void Raise_DgvSizeChanged_Immediate_Event()
    {
        if (DebugAll || DebugSizeChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSizeChanged_Immediate_Event");

        DgvSizeChanged_Immediate?.Invoke(this, sizeEventArgs);
    }

    private void Raise_DgvSizeChanged_Intermittent_Event()
    {
        if (DebugAll || DebugSizeChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSizeChanged_Intermittent_Event");

        DgvSizeChanged_Intermittent?.Invoke(this, sizeEventArgs);
    }

    // Selection Changed

    public void Req_DgvSelectionChanged_Event(object sender, EventArgs e)
    {
        Req_DgvSelectionChanged_Event();
    }

    public void Req_DgvSelectionChanged_Event()
    {
        if (PauseAllEvents || PauseSelnEventsFromGraph3d || PauseSelnEventsFromDgvCtrl)
            return;

        if (DebugAll || DebugSelnChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvSelectionChanged_Event()");

        BuildEventArgs_DgvSelectionChanged_Event();

        if ((DebugAll || DebugSelnChanged) && !tmr_SelectionChanged_Intermittent.TimerTiming)
            Console.WriteLine($"{InstanceName} - {ClassName} - Started tmr_DgvSelectionChanged_Intermittent");

        if ((DebugAll || DebugSelnChanged) && !tmr_SelectionChanged_Intermittent.TimerTiming)
            Console.WriteLine($"{InstanceName} - {ClassName} - Started tmr_DgvSelectionChanged_Debounced");

        tmr_SelectionChanged_Intermittent.Start();
        tmr_SelectionChanged_Debounced.Start();

        Raise_DgvSelectionChanged_Event();
    }

    private void BuildEventArgs_DgvSelectionChanged_Event()
    {
        selectEventArgs = new SelectEventArgs();
        selectEventArgs.Sender                 = dgvCtrl;
        selectEventArgs.SelectedCellCollection = dgvCtrl.dgv.SelectedCells;

        if (DebugAll || DebugSelnChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - DgvSelectionChanged EventArgs built");
    }

    private void Raise_DgvSelectionChanged_Intermittent_Event()
    {
        if (DebugAll || DebugSelnChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSelectionChanged_Intermittent_Event");

        DgvSelectionChanged_Intermittent?.Invoke(this, selectEventArgs);
    }

    private void Raise_DgvSelectionChanged_Debounced_Event()
    {
        if (DebugAll || DebugSelnChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSelectionChanged_Debounced_Event");

        DgvSelectionChanged_Debounced?.Invoke(this, selectEventArgs);
    }

    private void Raise_DgvSelectionChanged_Event()
    {
        if (DebugAll || DebugSelnChanged)
            Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSelectionChanged_Event");

        DgvSelectionChanged_Immediate?.Invoke(this, selectEventArgs);

        if (!PauseSelnEventsToGraph3d)
            DgvSelectionChanged_ToGraph3d_Immediate?.Invoke(this, selectEventArgs);
    }

    // Nested event arg classes

    public class SizeEventArgs : EventArgs
    {
        public object    Sender               { get; set; }
        public Size      DgvSize              { get; set; }
        public Rectangle DgvDisplayRectangle  { get; set; }
        public Rectangle DgvClientRectangle   { get; set; }
        public Point     DgvLocation          { get; set; }
        public int       DgvRowHeaderWidth    { get; set; }
        public int       DgvColumnHeaderWidth { get; set; }
        public int       DgvColumnWidth       { get; set; }
        public int       DgvRowHeight         { get; set; }
        public int       DgvColumnHeaderHeight { get; set; }
        public string    RowHeaderFormat      { get; set; } = "N0";
        public string    ColHeaderFormat      { get; set; } = "N0";

        public SizeEventArgs()
        { }

        public static new SizeEventArgs Empty;
    }

    public class SelectEventArgs : EventArgs
    {
        public object                           Sender                 { get; set; }
        public DataGridViewSelectedCellCollection SelectedCellCollection { get; set; }

        public SelectEventArgs()
        { }

        public static new SelectEventArgs Empty;
    }
}
