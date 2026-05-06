using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using TableEditor.DataGrid;
using TableEditor.Graph3D;
using TableEditor.Layout;
using TableEditor.Settings;
using TableEditor.Forms;
using TableEditor.Clipboard;
using TableEditor.UndoRedo;
using Timers;
using MicroLibrary;

// Plot3D
using Plot3D;

// Plot3D class aliases
using cColorScheme = Plot3D.Editor3D.cColorScheme;
using cObject3D    = Plot3D.Editor3D.cObject3D;
using cPoint3D     = Plot3D.Editor3D.cPoint3D;
using cSurfaceData = Plot3D.Editor3D.cSurfaceData;
using eColorScheme = Plot3D.Editor3D.eColorScheme;
using eInvalidate  = Plot3D.Editor3D.eInvalidate;
using eLegendPos   = Plot3D.Editor3D.eLegendPos;
using eMouseCtrl   = Plot3D.Editor3D.eMouseCtrl;
using eNormalize   = Plot3D.Editor3D.eNormalize;
using ePolygonMode = Plot3D.Editor3D.ePolygonMode;
using eRaster      = Plot3D.Editor3D.eRaster;
using eSelEvent    = Plot3D.Editor3D.eSelEvent;
using eSelType     = Plot3D.Editor3D.eSelType;
using eTooltip     = Plot3D.Editor3D.eTooltip;

// Keyboard
using Key      = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;

// Graph3D type aliases (types moved to TableEditor.Graph3D namespace)
using MyPoint  = TableEditor.Graph3D.MyPoint;
using MyPoints = TableEditor.Graph3D.MyPoints;

namespace TableEditor;

public partial class TableEditor3D : UserControl
{
    // ----- Public properties -----

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string ClassName { get; set; } = "TableEditor3D";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string InstanceName { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool Graph3dEnabled { get; set; } = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool UseMyScrollBars { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Font DgvFont
    {
        get { return DgvCtrl.Font; }
        set { DgvCtrl.Font = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int hTextPadding
    {
        get { return DgvCtrl.hTextPadding; }
        set { DgvCtrl.hTextPadding = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int vTextPadding
    {
        get { return DgvCtrl.vTextPadding; }
        set { DgvCtrl.vTextPadding = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(true)]
    public bool UseToolBar
    {
        get { return useToolBar; }
        set
        {
            useToolBar = value;

            if (useToolBar)
                toolBar.Show();
            else
                toolBar.Hide();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool UndoEnabled { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool CopyPasteEnabled { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool AverageEnabled { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public ColourScheme ColourTheme { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public CopyPasteMode CopyPasteSetMode { set { copyPasteMode = value; } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool HasData { get { return DgvCtrl.DgvHasData; } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Size DgvSize { get { return DgvCtrl.DgvSize; } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public DgvNumFormat DgvNumberFormat
    {
        get { return DgvCtrl.dgvNumFormat; }
        set { DgvCtrl.dgvNumFormat = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool Graph3dPointSelectMode
    {
        get { return graph3dPointSelectMode; }
        set
        {
            graph3dPointSelectMode = value;
            DgvToGraph3d.PointSelectMode = value;
            Graph3dCtrl.graph3d.PointSelectMode = value;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool Graph3dPointMoveMode
    {
        get { return graph3dPointMoveMode; }
        set
        {
            graph3dPointMoveMode = value;
            DgvToGraph3d.PointMoveMode = value;
            Graph3dCtrl.graph3d.PointMoveMode = value;

            // Reflect active state in the button icon
            if (Graph3dPointMoveMode)
                btn_Graph3d_PointMoveMode.ImageIndex = 9;
            else
                btn_Graph3d_PointMoveMode.ImageIndex = 8;
        }
    }

    // ISettings-backed properties — values come from SettingsService and are pushed out to
    // Graph3dCtrl when Graph3d is initialised so the first draw uses the saved preferences.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowSamples { get { return showSamples; } set { showSamples = value; ShowSampleButtons(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowButtonToolTips { get { return toolTip.Active; } set { toolTip.Active = value; } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowGraphPanel { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowAxis
    {
        get { return showAxis; }
        set { showAxis = value; if (Graph3dCtrl != null) Graph3dCtrl.ShowAxis = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowAxisLabels
    {
        get { return showAxisLabels; }
        set { showAxisLabels = value; if (Graph3dCtrl != null) Graph3dCtrl.ShowAxisLabels = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool MirrorPoints
    {
        get { return mirrorPoints; }
        set
        {
            mirrorPoints = value;

            // Clearing selections avoids stale highlighted points after a mirror-mode change
            if (DgvCtrl != null)
                DgvCtrl.ClearSelection();

            if (Graph3dCtrl != null)
                Graph3dCtrl.ClearGraphSelection();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowGraphPosition
    {
        get { return showGraphPosition; }
        set { showGraphPosition = value; if (Graph3dCtrl != null) Graph3dCtrl.ShowGraphPosition = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Color GraphPointsColour
    {
        get { return graphPointsColour; }
        set { graphPointsColour = value; if (Graph3dCtrl != null) Graph3dCtrl.SelectPointColour = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public float GraphPointSize
    {
        get { return graphPointSize; }
        set { graphPointSize = value; if (Graph3dCtrl != null) Graph3dCtrl.SelectPointSize = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int SelectRadius
    {
        get { return selectRadius; }
        set { selectRadius = value; if (Graph3dCtrl != null) Graph3dCtrl.SelectRadius = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int Rotation
    {
        get { return rotation; }
        set { rotation = value; if (Graph3dCtrl != null) Graph3dCtrl.Rotation = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int RotationTransposed
    {
        get { return rotationTransposed; }
        set { rotationTransposed = value; if (Graph3dCtrl != null) Graph3dCtrl.RotationTranspose = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int Elevation
    {
        get { return elevation; }
        set { elevation = value; if (Graph3dCtrl != null) Graph3dCtrl.Elevation = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int Zoom
    {
        get { return zoom; }
        set { zoom = value; if (Graph3dCtrl != null) Graph3dCtrl.Zoom = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool DebugSplitContainer { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool DebugForm { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool DebugMouse { get; set; }

    // ----- Public instance references -----

    // PascalCase properties expose formerly-public fields so callers get a stable contract
    // even after the backing implementation changes.
    public DgvCtrl DgvCtrl { get; private set; }
    public Graph3dCtrl Graph3dCtrl { get; private set; }
    public DgvToGraph3d DgvToGraph3d { get; private set; }

    // Both scroll-bar and DGV-header controls share the same set of references, so a single
    // LayoutControls instance satisfies both roles (replaces ScrollBarCntrls / DgvHeaderCntrls).
    public LayoutControls ScrollBarControls { get; private set; }
    public LayoutControls DgvHeaderControls { get; private set; }

    // ----- Private fields -----

    private AverageTool averageTool;

    // Backing fields for public properties
    bool showSamples;
    bool useToolBar;
    bool graph3dPointSelectMode;
    bool graph3dPointMoveMode;
    bool showAxis;
    bool showAxisLabels;
    bool mirrorPoints;
    float graphPointSize;
    int selectRadius;
    Color graphPointsColour;
    bool showGraphPosition;
    int rotation;
    int rotationTransposed;
    int elevation;
    int zoom;
    CopyPasteMode copyPasteMode;

    bool transposeXY;
    int lastSplitterDistance = 0;

    // Re-entrancy guard: skip a timer tick if the previous one has not finished yet.
    // Without this the 200ms timer can queue up work faster than it can be processed.
    bool timerBusy;

    enum InterpolateMode
    {
        Vertical,
        Horizontal,
        All
    }

    enum GraphDisplay
    {
        Nothing,
        Right,
        Fill,
        Hide,
    }

    GraphDisplay graphDisplay;

    // ----- Constructor -----

    public TableEditor3D()
    {
        InitializeComponent();

        // Eagerly read the persisted settings so properties have sane defaults even before
        // Initialise() is called (e.g. when the Designer creates an instance).
        UserSettings_Load();
    }

    // ----- Initialise -----

    // Call this after setting all configuration properties, from the host form's
    // IsHandleCreated / Shown event.  Creates sub-controllers and wires events.
    public void Initialise()
    {
        if (UseMyScrollBars)
        {
            // Package the layout controls into a single bundle for DgvCtrl to distribute
            // to both the scrollbar controller and the header controller.
            ScrollBarControls = new LayoutControls
            {
                VScrollBar     = vScrollBar,
                HScrollBar     = hScrollBar,
                SplitContainer = splitContainer1
            };

            DgvHeaderControls = new LayoutControls
            {
                RowHeader      = rowHeader,
                ColHeader      = colHeader,
                BlankingPanel  = blankingPanel,
                VScrollBar     = vScrollBar,
                HScrollBar     = hScrollBar,
                SplitContainer = splitContainer1
            };
        }

        DgvCtrl = new DgvCtrl
        {
            Dgv              = DgvTable,
            UseMyScrollBars  = UseMyScrollBars,
            ScrollBarCntrls  = ScrollBarControls,
            DgvHeaderCntrls  = DgvHeaderControls,
            UndoEnabled      = UndoEnabled,
            CopyPasteEnabled = CopyPasteEnabled,
            ColourTheme      = ColourTheme,
            InstanceName     = InstanceName
        };
        DgvCtrl.Initialise();

        if (Graph3dEnabled)
        {
            Graph3dCtrl   = new Graph3dCtrl(Graph3d_UserControl, InstanceName);
            DgvToGraph3d  = new DgvToGraph3d(DgvCtrl, Graph3dCtrl, InstanceName);
            Graph3d_Initialise();
        }

        if (UndoEnabled) DgvCtrl.FormButton_UndoEnabled += DgvCtrl_FormButton_UndoEnabled;
        if (UndoEnabled) DgvCtrl.FormButton_RedoEnabled += DgvCtrl_FormButton_RedoEnabled;

        this.ContextMenuStrip = contextMenuStrip;

        // SettingsService broadcasts when the user saves the settings dialog so every open
        // instance reloads without needing a static event that is never unsubscribed.
        SettingsService.Default.SettingsChanged += OnSettingsChanged;

        SplitContainer_Initialise();

        Main_Timer.Start();
    }

    // ----- Graph3D initialisation -----

    private void Graph3d_Initialise()
    {
        // Focus events keep keyboard shortcuts working regardless of which panel the cursor
        // is over.
        DgvCtrl.dgv.MouseEnter           += Dgv_MouseEnter;
        DgvCtrl.dgv.MouseLeave           += Dgv_MouseLeave;
        Graph3dCtrl.graph3d.MouseEnter   += Graph3d_MouseEnter;
        Graph3dCtrl.graph3d.MouseLeave   += Graph3d_MouseLeave;

        // Left-click in the DGV cancels point-manipulation button states.
        DgvCtrl.dgv.MouseClick           += Dgv_MouseClick;

        // Double-clicking the splitter snaps it to the natural DGV edge.
        splitContainer1.DoubleClick      += SplitContainer1_DoubleClick;

        Graph3dCtrl.graph3d.KeyDown      += Graph3d_KeyDown;
        Graph3dCtrl.graph3d.KeyUp        += Graph3d_KeyUp;

        // Override the designer back colour so it matches the app theme at runtime.
        Graph3d_UserControl.BackColor = SystemColors.Control;

        // Push saved settings into the graph controller on first load.
        Graph3dCtrl.Zoom              = Zoom;
        Graph3dCtrl.Elevation         = Elevation;
        Graph3dCtrl.Rotation          = Rotation;
        Graph3dCtrl.RotationTranspose = RotationTransposed;
        Graph3dCtrl.ShowAxis          = ShowAxis;
        Graph3dCtrl.ShowAxisLabels    = ShowAxisLabels;
        Graph3dCtrl.MirrorPoints      = MirrorPoints;
        Graph3dCtrl.ShowGraphPosition = ShowGraphPosition;
        Graph3dCtrl.SelectPointColour = GraphPointsColour;
        Graph3dCtrl.SelectPointSize   = GraphPointSize;
        Graph3dCtrl.InstanceName      = InstanceName;
        DgvToGraph3d.MirrorPoints     = MirrorPoints;
    }

    // ----- SplitContainer initialisation -----

    private void SplitContainer_Initialise()
    {
        if (!UseMyScrollBars)
        {
            splitContainer1.Panel2Collapsed = true;
            return;
        }

        splitContainer1.Panel1MinSize    = Init.SplitContainerPanel1MinSize;
        splitContainer1.Panel2MinSize    = Init.SplitContainerPanel2MinSize;
        splitContainer1.SplitterDistance = Init.SplitContainerSplitterDistance;

        if (Graph3dEnabled && ShowGraphPanel)
        {
            splitContainer1.Panel2Collapsed = false;
            graphDisplay = GraphDisplay.Right;
        }
        else
        {
            splitContainer1.Panel2Collapsed = true;
        }
    }

    // ----- Helpers -----

    // Calculates the splitter position that places the divider at the right edge of the DGV
    // content, leaving the minimum viable space for the graph panel.
    public int CalcSplitterDistance()
    {
        int scrlBarWdth = 0;
        if (UseMyScrollBars)
            scrlBarWdth = vScrollBar.Visible ? vScrollBar.Width : 0;

        int calc1 = ClientRectangle.Width - Init.Graph3dMinimumSize.Width - splitContainer1.SplitterWidth;
        int calc2 = DgvCtrl.DgvSize.Width + splitContainer1.SplitterWidth + scrlBarWdth;

        int result = 0;
        if (DgvCtrl.DgvHasData)
        {
            if (calc1 > calc2)
            {
                if (calc2 > 0)
                    result = calc2;
            }
            else
            {
                if (calc1 > 0)
                    result = calc1;
            }
        }

        if (DebugSplitContainer)
        {
            Console.WriteLine($"{InstanceName} - SplitterDistance {result}");
            Console.WriteLine($"{InstanceName} - calc1 {calc1}, calc2 {calc2}");
        }

        return result;
    }

    public override string ToString()
    {
        return InstanceName;
    }

    // ----- Settings -----

    // Reads all user-configurable settings from SettingsService.Default (the singleton that
    // wraps Properties.Settings).  Called at construction and again each time the user saves
    // the settings dialog via SettingsService.SettingsChanged.
    public void UserSettings_Load()
    {
        ISettings s = SettingsService.Default;

        ShowSamples        = s.ShowSamples;
        ShowButtonToolTips = s.ShowButtonToolTips;
        ShowGraphPanel     = s.ShowGraphPanelOnStart;
        ShowAxis           = s.ShowAxis;
        ShowAxisLabels     = s.ShowAxisLabels;
        MirrorPoints       = s.MirrorPoints;
        ShowGraphPosition  = s.ShowGraphPosition;
        GraphPointsColour  = s.GraphPointsColour;
        GraphPointSize     = s.GraphPointSize;
        SelectRadius       = s.SelectRadius;
        Rotation           = s.Rotation;
        RotationTransposed = s.RotationTransposed;
        Elevation          = s.Elevation;
        Zoom               = s.Zoom;
    }

    // Forwarding handler for the SettingsService.SettingsChanged event.
    private void OnSettingsChanged(object sender, EventArgs e)
    {
        UserSettings_Load();
    }

    // ----- Timers -----

    // Stops timers that fire on background threads.  Call from the host form's Closing event
    // so the timers do not continue ticking after the application exits.
    public void KillTimers()
    {
        DgvCtrl.myEvents.tmr_DgvSizeChanged_Intermittent.Stop();
        DgvCtrl.myEvents.tmr_DgvDataChanged_Debounced.Stop();
        DgvCtrl.incDecTask.tmr_IncDec.Stop();

        if (Graph3dEnabled)
            DgvToGraph3d.tmrZValuesToDgvIntermittent.Stop();
    }

    // ----- Dispose -----

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe before anything else to prevent callbacks into a partially-disposed
            // object if the event fires during shutdown.
            SettingsService.Default.SettingsChanged -= OnSettingsChanged;

            KillTimers();

            DgvCtrl?.Dispose();

            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    // ----- Public read API -----

    public double[] ReadFromDgv_RowLabels()
    {
        double[] y = new double[0];

        try
        {
            y = DgvCtrl.ReadRowHeaders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"External read from the table editor failed.\r\n \r\n" +
                $"{ex.Message} at line {ExceptionHelper.FormatStackTrace(ex)}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return y;
    }

    public double[] ReadFromDgv_ColLabels()
    {
        double[] x = new double[0];

        try
        {
            x = DgvCtrl.ReadColHeaders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"External read from the table editor failed.\r\n \r\n" +
                $"{ex.Message} at line {ExceptionHelper.FormatStackTrace(ex)}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return x;
    }

    public double[,] ReadFromDgv_TableData()
    {
        double[,] z = new double[0, 0];

        try
        {
            z = DgvCtrl.ReadDataTable();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"External read from the table editor failed.\r\n \r\n" +
                $"{ex.Message} at line {ExceptionHelper.FormatStackTrace(ex)}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return z;
    }

    public (double[], double[], double[,]) ReadFromDgv()
    {
        double[] d1   = ReadFromDgv_RowLabels();
        double[] d2   = ReadFromDgv_ColLabels();
        double[,] d3  = ReadFromDgv_TableData();
        return (d1, d2, d3);
    }

    public void DgvClearSelection()
    {
        DgvCtrl.ClearSelection();
    }

    // ----- Host-form API -----

    // Call from the host form's Resize event when the window is maximised so the splitter
    // re-snaps to the DGV edge.
    public void FormResizedToMaximum()
    {
        if (DebugSplitContainer)
            Console.WriteLine($"{InstanceName} - FormResizedToMaximum() {DgvSize.Width}");

        splitContainer1.SplitterDistance = CalcSplitterDistance();
    }

    // ----- Simple event handlers -----

    private void DgvCtrl_FormButton_UndoEnabled(object sender, bool e)
    {
        btn_Undo.Enabled = e;
    }

    private void DgvCtrl_FormButton_RedoEnabled(object sender, bool e)
    {
        btn_Redo.Enabled = e;
    }

    private void DgvCtrl_FormButton_AverageEnabled(object sender, bool e)
    {
        btn_AverageTool.Enabled = e;
    }

    private void SplitContainer1_DoubleClick(object sender, EventArgs e)
    {
        if (DebugSplitContainer)
            Console.WriteLine($"{InstanceName} - SplitContainer1_DoubleClick() {DgvSize.Width}");

        splitContainer1.SplitterDistance = CalcSplitterDistance();
    }

    private void Dgv_MouseEnter(object sender, EventArgs e)
    {
        DgvCtrl.dgv.Focus();
    }

    private void Dgv_MouseLeave(object sender, EventArgs e)
    {
        if (DebugMouse)
            Console.WriteLine($"{InstanceName} - Dgv_MouseLeave()");

        // Don't clear hover points while the context menu is open; clearing them would reset
        // the selection the user is about to act on.
        if (!this.contextMenuStrip.Visible)
            DgvToGraph3d.ClearHoverPoints();

        this.Focus();
    }

    private void Graph3d_MouseEnter(object sender, EventArgs e)
    {
        Graph3dCtrl.graph3d.Focus();
    }

    private void Graph3d_MouseLeave(object sender, EventArgs e)
    {
        DgvToGraph3d.ClearHoverPoints();
        this.Focus();
    }

    private void Dgv_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // A left click in the DGV panel cancels both point modes because the user has
            // switched attention back to table editing.
            Graph3dPointSelectMode = false;
            Graph3dPointMoveMode   = false;

            btn_Graph3d_PointSelectMode.ImageIndex = 6;
        }
    }

    private void Graph3d_KeyDown(object sender, KeyEventArgs e)
    {
        // Alt key activates graph rotation via Editor3D; while it is held the point-select
        // mode must be suspended so Alt+drag does not fight with the selection lasso.
        if (e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu)
        {
            Graph3dPointSelectMode = false;
            Graph3dPointMoveMode   = false;

            btn_Graph3d_PointSelectMode.Enabled    = false;
            btn_Graph3d_PointMoveMode.Enabled      = false;
            btn_Graph3d_PointSelectMode.ImageIndex = 6;
        }
    }

    private void Graph3d_KeyUp(object sender, KeyEventArgs e)
    {
        // Re-enable the select button once the user releases Alt so they can use the mode
        // again without having to click away and back.
        if (e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu)
            btn_Graph3d_PointSelectMode.Enabled = true;
    }
}
