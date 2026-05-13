using System.Diagnostics;

namespace TableEditor.Common;

// Each field is a named TraceSwitch whose level is read from App.config at startup via
// the standard <system.diagnostics><switches> section.  Setting a switch to level 4
// (Verbose) in config is equivalent to the old "set MyDebug.xxx = true" calls.
//
// Example App.config entry:
//   <system.diagnostics>
//     <switches>
//       <add name="Mouse" value="4" />
//     </switches>
//   </system.diagnostics>
public static class TraceCategories
{
    public static TraceSwitch Mouse     = new TraceSwitch("Mouse",     "Mouse events");
    public static TraceSwitch Form      = new TraceSwitch("Form",      "Form/splitter events");
    public static TraceSwitch Dgv       = new TraceSwitch("Dgv",       "DataGridView events");
    public static TraceSwitch Graph3d   = new TraceSwitch("Graph3d",   "3D graph events");
    public static TraceSwitch Selection = new TraceSwitch("Selection", "Selection events");
    public static TraceSwitch Clipboard = new TraceSwitch("Clipboard", "Copy/paste events");
    public static TraceSwitch Splitter  = new TraceSwitch("Splitter",  "Splitter events");
}

// Thin compatibility shim so existing code that sets MyDebug boolean properties
// continues to compile without changes.  Each setter maps to the corresponding
// TraceSwitch level — true → Verbose (4), false → Off (0).
public class MyDebug
{
    public bool TableEditor_Mouse
    {
        set { TraceCategories.Mouse.Level = value ? TraceLevel.Verbose : TraceLevel.Off; }
    }

    public bool TableEditor_Form
    {
        set { TraceCategories.Form.Level = value ? TraceLevel.Verbose : TraceLevel.Off; }
    }

    public bool TableEditor_SplitContainer
    {
        set { TraceCategories.Splitter.Level = value ? TraceLevel.Verbose : TraceLevel.Off; }
    }

    public bool DgvCtrl_SelectionChangedDebug
    {
        set { TraceCategories.Selection.Level = value ? TraceLevel.Verbose : TraceLevel.Off; }
    }

    public bool DgvCtrl_paste_Debug
    {
        set { TraceCategories.Clipboard.Level = value ? TraceLevel.Verbose : TraceLevel.Off; }
    }

    public bool DgvGrph3dIntfc_DebugAll
    {
        set { TraceCategories.Graph3d.Level = value ? TraceLevel.Verbose : TraceLevel.Off; }
    }

    // Properties that mapped to per-instance debug flags on sub-controls are kept as
    // no-ops here so call sites still compile.  Wire them to per-instance fields when
    // those controls are refactored to use TraceSwitch directly.
    public bool DgvCtrl_DataChangedDebug      { set { } }
    public bool DgvCtrl_SizeChangedDebug      { set { } }
    public bool DgvCtrl_EventDebug            { set { } }
    public bool DgvCtrl_incDecTask_Debug      { set { } }
    public bool DgvCtrl_undo_Debug            { set { } }
    public bool DgvCtrl_DgvData_Debug         { set { } }
    public bool DgvCtrl_MouseDebug            { set { } }
    public bool DgvCtrl_myEvents_DebugAll     { set { } }
    public bool DgvCtrl_myEvents_DebugDataChngd  { set { } }
    public bool DgvCtrl_myEvents_DebugSizeChngd  { set { } }
    public bool DgvCtrl_myEvents_DebugSelnChngd  { set { } }
    public bool DgvCtrl_myEvents_MuteHghSpd      { set { } }
    public bool DgvCtrl_myEvents_DebugDbncTmr    { set { } }
    public bool DgvCtrl_myEvents_DebugIntTmr     { set { } }
    public bool ScrollBarCtrl_DebugPosition      { set { } }
    public bool ScrollBarCtrl_DebugExternalEvents { set { } }
    public bool ScrollBarCtrl_DebugMouseWheel    { set { } }
    public bool ScrollBarCtrl_DebugValues        { set { } }
    public bool DgvHeaders_DebugHeaders          { set { } }
    public bool DgvGrph3dIntfc_DebugData         { set { } }
    public bool DgvGrph3dIntfc_DebugTimers       { set { } }
    public bool DgvGrph3dIntfc_DebugHoverPoint   { set { } }
    public bool DgvGrph3dIntfc_DebugPointMoveMode   { set { } }
    public bool DgvGrph3dIntfc_DebugSelectionPoints { set { } }
    public bool Graph3dCtrl_DebugPointMoveMode   { set { } }
    public bool Graph3dCtrl_DebugData            { set { } }
    public bool Graph3dCtrl_DebugData_WithPrint  { set { } }
    public bool Graph3dCtrl_DebugPointSelectMode { set { } }
}
