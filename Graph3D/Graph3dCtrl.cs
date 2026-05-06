﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// Plot 3D type aliases â€” these short names are used throughout this file and by callers.
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
using Editor3D     = Plot3D.Editor3D;

namespace TableEditor.Graph3D;

// Wraps the Plot3D.Editor3D user control and owns all plot configuration, data loading,
// point selection callbacks, and view-position management.
// DgvToGraph3d drives this class; TableEditor3D owns both.
public class Graph3dCtrl
{
    // ---------- Defaults ----------

    // Home rotation differs by transpose state so both orientations feel natural.
    const int DefaultRotation           = 210;
    const int DefaultRotationTransposed = 240;
    const int DefaultZoom               = 1350;
    const int DefaultElevation          = 80;

    const bool ShowAxisDefault          = true;
    const bool ShowAxisLabelsDefault    = true;
    const bool ShowToolTipDefault       = false;
    const bool ShowGraphPositionDefault = true;
    static Color DefaultSelPointColour  = Color.Black;
    const float DefaultSelPointSize     = 1.0F;
    const bool ShowLegendDefault        = false;
    const bool MirrorXAxisDefault       = true;
    const bool MirrorYAxisDefault       = true;

    const int DefaultTooltipRadius      = 6;
    const int DefaultSelectRadius       = 32;

    // ---------- Properties ----------

    public string ClassName    { get; set; } = "Graph3dCtrl";
    public string InstanceName { get; set; }

    // Zoom, Elevation, and Rotation all trigger a redraw immediately if the plot is live.
    public int Zoom
    {
        get => zoom;
        set
        {
            if (zoom == value) return;
            zoom = value;
            updatePositionFlag = true;
            if (IsDrawn) DrawPlot();
        }
    }

    public int Elevation
    {
        get => elevation;
        set
        {
            if (elevation == value) return;
            elevation = value;
            updatePositionFlag = true;
            if (IsDrawn) DrawPlot();
        }
    }

    public int Rotation
    {
        get => rotation;
        set
        {
            if (rotation == value) return;
            rotation = value;
            updatePositionFlag = true;
            if (IsDrawn) DrawPlot();
        }
    }

    // Separate rotation value used when TransposeXY is true so each orientation has its own
    // last-known home position.
    public int RotationTranspose
    {
        get => rotationTransposed;
        set
        {
            if (rotationTransposed == value) return;
            rotationTransposed = value;
            updatePositionFlag = true;
            if (IsDrawn) DrawPlot();
        }
    }

    public bool ShowAxis
    {
        get => showAxis;
        set => showAxis = value;
    }

    public bool ShowAxisLabels
    {
        get => showAxisLabels;
        set => showAxisLabels = value;
    }

    public bool MirrorPoints { get; set; }

    public bool ShowToolTip
    {
        get => showToolTip;
        set => showToolTip = value;
    }

    public bool ShowGraphPosition
    {
        get => showGraphPosition;
        set => showGraphPosition = value;
    }

    public Color SelectPointColour
    {
        get => selPointColour;
        set
        {
            selPointColour = value;
            if (IsDrawn) DrawPlot();
        }
    }

    public float SelectPointSize
    {
        get => selPointSize;
        set
        {
            selPointSize = value;
            if (IsDrawn) DrawPlot();
        }
    }

    public bool ShowLegend
    {
        set => showLegend = value;
    }

    public bool MirrorXAxis
    {
        get => mirrorXAxis;
        set => mirrorXAxis = value;
    }

    public bool MirrorYAxis
    {
        get => mirrorYAxis;
        set => mirrorYAxis = value;
    }

    public bool Focused
    {
        get => graph3d.Focused;
        set => graph3d.Focus();
    }

    // TransposeXY setter also triggers a full re-plot through Transpose() so the renderer
    // immediately reflects the new axis orientation.
    public bool TransposeXY
    {
        get => transposeXY;
        set { transposeXY = value; Transpose(); }
    }

    public bool IsDrawn { get; private set; }

    public cPoint3D[] SelectedPoints => graph3d.Selection.GetSelectedPoints(eSelType.All);

    // These properties accept values from the DGV and store them in two sets of backing fields:
    // one "from DGV" (untransposed, used for reverse lookup) and one "to graph" (may be swapped).
    public double[] XAxisLabels
    {
        get => xValuesFromDgv;
        set { xValuesFromDgv = value; xValuesToGraph = value; }
    }

    public double[] YAxisLabels
    {
        get => yValuesFromDgv;
        set { yValuesFromDgv = value; yValuesToGraph = value; }
    }

    public double[,] ZValues
    {
        get => zValuesFromDgv;
        set { zValuesFromDgv = value; zValuesToGraph = value; }
    }

    // Tool-tip text fragments â€” combined into a tooltip string when rendering.
    public string ToolTipTextTitle { set => toolTipTextTitle = value; }
    public string XUnit            { set => xUnit = " " + value; }
    public string YUnit            { set => yUnit = " " + value; }
    public string ZUnit            { set => zUnit = " " + value; }
    public int    TooltipRadius    { get => toolTipRadius;  set => toolTipRadius  = value; }
    public int    SelectRadius     { get => selectRadius;   set => selectRadius   = value; }

    // Axis legend labels â€” applied to the graph axes when the plot is drawn.
    public string LegendXAxis { set => legendXAxis = value; }
    public string LegendYAxis { set => legendYAxis = value; }
    public string LegendZAxis { set => legendZAxis = value; }

    public Size ClientSize => graph3d.ClientSize;

    // Debug flags
    public bool DebugPointSelectMode  { get; set; }
    public bool DebugPointMoveMode    { get; set; }
    public bool DebugData             { get; set; }
    public bool DebugDataWithPrint    { get; set; }

    // The user-selection collection is set by DgvToGraph3d so that Graph3dCtrl can consult it
    // during selection callbacks without creating a circular dependency.
    public MyPoints UserSelectedMyPoints { get; set; }

    // ---------- Backing fields ----------

    string toolTipTextTitle = "";
    string toolTipText      = "";
    string xUnit            = "";
    string yUnit            = "";
    string zUnit            = "";

    int   zoom              = DefaultZoom;
    int   elevation         = DefaultElevation;
    int   rotation          = DefaultRotation;
    int   rotationTransposed = DefaultRotationTransposed;
    bool  showAxis          = ShowAxisDefault;
    bool  showAxisLabels    = ShowAxisLabelsDefault;
    bool  showToolTip       = ShowToolTipDefault;
    Color selPointColour    = DefaultSelPointColour;
    float selPointSize      = DefaultSelPointSize;
    bool  showGraphPosition = ShowGraphPositionDefault;
    bool  showLegend        = ShowLegendDefault;
    bool  mirrorXAxis       = MirrorXAxisDefault;
    bool  mirrorYAxis       = MirrorYAxisDefault;
    int   toolTipRadius     = DefaultTooltipRadius;
    int   selectRadius      = DefaultSelectRadius;

    string legendXAxis = "";
    string legendYAxis = "";
    string legendZAxis = "";

    // ---------- Internal state ----------

    // The Editor3D user control â€” passed in from the host form.
    public Editor3D graph3d;

    // Axis values in graph space (may be transposed relative to DGV space).
    double[] xValuesToGraph;
    double[] yValuesToGraph;
    double[,] zValuesToGraph;

    // Original axis values from the DGV â€” kept untransposed for reverse index lookups.
    double[] xValuesFromDgv;
    double[] yValuesFromDgv;
    double[,] zValuesFromDgv;

    bool resetViewPosition;
    bool transposeXY;

    // When a property like Zoom changes we only want to update the camera, not reset it home.
    bool updatePositionFlag;

    // Set briefly during a Transpose() call so LoadOptions() shifts the rotation angle rather
    // than resetting to the home position.
    bool transposeReq;

    // The surface data object is built in DrawPlot() and kept alive so other code can read or
    // mutate individual point selected states without rebuilding the whole surface.
    public cSurfaceData surfaceData;

    // Last set of moved points from a drag event â€” forwarded via Graph3dNdr.
    public List<cPoint3D> pointsMoved;

    // Raised when the user alt-clicks a graph point (single-point selection toggle).
    public event EventHandler<cObject3D> AltSelection;

    // Raised continuously while the user drags selected points (Z-value editing).
    public event EventHandler<List<cPoint3D>> Graph3dNdr;

    // ---------- Constructor ----------

    public Graph3dCtrl(Editor3D graph3dUserControl, string instanceName)
    {
        graph3d      = graph3dUserControl;
        InstanceName = instanceName;

        LoadOptions();
    }

    // ---------- Plot management ----------

    // Full re-plot: updates axis labels, resets the view position, and redraws everything.
    // Call this when the table dimensions change (new data paste, file load, etc.).
    public void SetPlotData(double[] x, double[] y, double[,] z)
    {
        if (!ValidValuesToGraph(x, y, z))
        {
            if (DebugData) Console.WriteLine($"{InstanceName} - {ClassName} - SetPlotData() returned: invalid values");
            if (DebugDataWithPrint) PrintGraphValues(x, y, z);

            graph3d.Clear();
            return;
        }

        if (DebugData) Console.WriteLine($"{InstanceName} - {ClassName} - SetPlotData(x, y, z)");
        if (DebugDataWithPrint) PrintGraphValues(x, y, z);

        ClearGraphSelection();

        xValuesFromDgv = x;
        yValuesFromDgv = y;
        zValuesFromDgv = z;

        // Only reset the camera home when we are not in the middle of a transpose operation,
        // because transpose handles its own rotation adjustment.
        if (!transposeReq)
            ResetViewPosition();

        UpdatePlot(x, y, z);

        IsDrawn = true;
    }

    // Builds the cSurfaceData structure and issues a draw to the Editor3D control.
    // Called by UpdatePlot() and by property setters that affect visual appearance.
    public void DrawPlot()
    {
        if (!ValidValuesToGraph(xValuesToGraph, yValuesToGraph, zValuesToGraph))
        {
            if (DebugData) Console.WriteLine($"{InstanceName} - {ClassName} - DrawPlot() returned: invalid values");
            if (DebugDataWithPrint) PrintGraphValues(xValuesToGraph, yValuesToGraph, zValuesToGraph);

            graph3d.Clear();
            return;
        }

        if (DebugData) Console.WriteLine($"{InstanceName} - {ClassName} - DrawPlot()");

        int cols = xValuesToGraph.Length;
        int rows = yValuesToGraph.Length;

        // Fill mode draws thin 1-pixel black separator lines between cells.
        var polygonMode = ePolygonMode.Fill;
        var pen         = (polygonMode == ePolygonMode.Lines) ? new Pen(Color.Yellow, 2) : Pens.Black;
        var colorScheme = new cColorScheme(eColorScheme.HP_Tuners);
        surfaceData     = new cSurfaceData(cols, rows, polygonMode, pen, colorScheme);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                double xVal = xValuesToGraph[x];
                double yVal = yValuesToGraph[y];
                double zVal = zValuesToGraph[y, x];

                // The fourth argument is the tooltip text; the fifth is the normalisation key.
                var point = new cPoint3D(xVal, yVal, zVal, toolTipText, zVal);
                surfaceData.SetPointAt(x, y, point);
            }
        }

        // Snapshot the current selection before clearing so we can restore it afterward â€”
        // the Editor3D Clear() call destroys all selection state.
        cPoint3D[] previousSelection = graph3d.Selection.GetSelectedPoints(eSelType.All);

        graph3d.Clear();
        LoadOptions();

        graph3d.AddRenderData(surfaceData);
        graph3d.Invalidate();

        ReloadSelectedPoints(previousSelection);
    }

    // Reapplies selected state for a set of previously selected points after a redraw.
    // Uses exact value comparison via the axis arrays â€” floating-point labels match because
    // they are stored directly and not recomputed.
    private void ReloadSelectedPoints(cPoint3D[] selPoints)
    {
        if (selPoints.Length == 0) return;

        foreach (cPoint3D pt in selPoints)
        {
            int xIndex = Array.FindIndex(xValuesToGraph, x => x == pt.X);
            int yIndex = Array.FindIndex(yValuesToGraph, y => y == pt.Y);

            surfaceData.GetPointAt(xIndex, yIndex).Selected = true;
        }
    }

    // Applies all visual/configuration options to the Editor3D control.
    // Called from DrawPlot() and from the constructor to set initial state.
    private void LoadOptions()
    {
        graph3d.Normalize          = eNormalize.Separate;
        graph3d.Selection.SinglePoints = true;
        Editor3D.SelSizeK          = selPointSize;
        Editor3D.SelectRadius      = selectRadius;
        Editor3D.ToolTipRadius     = toolTipRadius;

        graph3d.SetUserInputs(eMouseCtrl.L_Theta_L_Phi);

        // Tooltip and hover-point modes are mutually exclusive â€” MirrorPoints drives hover
        // point display through DgvToGraph3d, so enable that mode when requested.
        if (showToolTip && !MirrorPoints)       graph3d.TooltipMode = eTooltip.Coord;
        else if (!showToolTip && MirrorPoints)  graph3d.TooltipMode = eTooltip.Hover;
        else                                    graph3d.TooltipMode = eTooltip.Off;

        graph3d.BackColor          = SystemColors.Control;
        graph3d.BorderColorFocus   = SystemColors.Control;
        graph3d.BorderColorNormal  = SystemColors.Control;
        graph3d.Selection.HighlightColor = selPointColour;

        graph3d.AxisX.Mirror       = mirrorXAxis;
        graph3d.AxisY.Mirror       = mirrorYAxis;
        graph3d.AxisX.LegendText   = legendXAxis;
        graph3d.AxisY.LegendText   = legendYAxis;
        graph3d.AxisZ.LegendText   = legendZAxis;
        graph3d.LegendPos          = eLegendPos.BottomLeft;

        // Axis raster mode: show lines only, lines + labels, or nothing.
        if (showAxis && !showAxisLabels)  graph3d.Raster = eRaster.MainAxes;
        else if (showAxis)                graph3d.Raster = eRaster.Labels;
        else                              graph3d.Raster = eRaster.Off;

        // Camera position is only updated when something changed â€” avoids resetting the user's
        // current view on every cell edit.
        if (!IsDrawn || resetViewPosition || updatePositionFlag || transposeReq)
        {
            if (transposeReq)
            {
                // Nudge the rotation angle when toggling transpose so the new orientation
                // still faces the user in roughly the right direction.
                if (TransposeXY) rotation -= 30;
                else             rotation += 30;
            }
            else if (!TransposeXY)
                graph3d.SetCoefficients(zoom, elevation, rotation);
            else
                graph3d.SetCoefficients(zoom, elevation, rotationTransposed);
        }

        updatePositionFlag = false;
        transposeReq       = false;

        // The top-legend area doubles as a "graph position" readout â€” hide it by setting
        // the colour to Empty (transparent).
        graph3d.TopLegendColor = showGraphPosition ? Color.Black : Color.Empty;

        graph3d.Selection.Callback     = OnSelectEvent;
        graph3d.Selection.MultiSelect  = false;
        graph3d.Selection.Enabled      = true;
        graph3d.Selection.SinglePoints = true;

        // Disable the Editor3D's built-in undo buffer â€” we manage undo ourselves in DgvCtrl.
        graph3d.UndoBuffer.Enabled = false;

        resetViewPosition = false;
    }

    public void Invalidate()
    {
        graph3d.Invalidate();
    }

    public void Reset()
    {
        graph3d.Clear();
        graph3d.Invalidate();
    }

    public void ClearGraphSelection()
    {
        graph3d.Selection.DeSelectAll();
        Invalidate();
    }

    // Validates that axis and Z arrays meet the minimum size requirements for rendering.
    // The graph requires at least a 3Ã—3 grid to produce meaningful surface geometry.
    public bool ValidValuesToGraph(double[] x, double[] y, double[,] z)
    {
        if (x == null || x.Length < 3) return false;
        if (y == null || y.Length < 3) return false;
        if (z == null || z.GetLength(0) < 3 || z.GetLength(1) < 3) return false;
        return true;
    }

    // Returns the MyPoint nearest to the given mouse coordinates, or an invalidated MyPoint
    // if the cursor is outside the selection radius or the plot is not yet drawn.
    public MyPoint GetNearestPoint(MouseEventArgs e)
    {
        var myPoint = new MyPoint();

        if (!IsDrawn)
        {
            myPoint.Invalidate();
            return myPoint;
        }

        // The Editor3D control returns null when nothing is within the select radius.
        cObject3D found = graph3d.FindObjectAt(e.X, e.Y, true);

        if (found == null)
        {
            myPoint.Invalidate();
            return myPoint;
        }

        // When transposed, graph X maps to DGV columns (Y axis label) and vice-versa, so we
        // reverse the mapping here to get back to DGV coordinate space.
        if (TransposeXY)
        {
            myPoint.XAxisTag = found.Points[0].Y;
            myPoint.YAxisTag = found.Points[0].X;
        }
        else
        {
            myPoint.XAxisTag = found.Points[0].X;
            myPoint.YAxisTag = found.Points[0].Y;
        }

        myPoint.Z = found.Points[0].Z;

        // Resolve axis tags back to DGV array indexes using tolerance-based comparison to
        // avoid false misses from floating-point round-trips through the renderer.
        if (TransposeXY)
        {
            myPoint.ColIndex = Array.FindIndex(yValuesFromDgv, v => System.Math.Abs(v - myPoint.YAxisTag) < 1e-9);
            myPoint.RowIndex = Array.FindIndex(xValuesFromDgv, v => System.Math.Abs(v - myPoint.XAxisTag) < 1e-9);
        }
        else
        {
            myPoint.ColIndex = Array.FindIndex(xValuesFromDgv, v => System.Math.Abs(v - myPoint.XAxisTag) < 1e-9);
            myPoint.RowIndex = Array.FindIndex(yValuesFromDgv, v => System.Math.Abs(v - myPoint.YAxisTag) < 1e-9);
        }

        myPoint.Found = true;

        return myPoint;
    }

    // Applies transposing to axis arrays and Z matrix then redraws.
    // Called when Z values change but the axis dimensions are the same.
    public void UpdatePlot(double[] x, double[] y, double[,] z)
    {
        if (DebugData) Console.WriteLine($"{InstanceName} - {ClassName} - UpdatePlot(x, y, z)");
        if (DebugDataWithPrint) PrintGraphValues(x, y, z);

        if (TransposeXY)
        {
            xValuesToGraph = y;
            yValuesToGraph = x;
            zValuesToGraph = TransposeZ(z);
        }
        else
        {
            xValuesToGraph = x;
            yValuesToGraph = y;
            zValuesToGraph = z;
        }

        DrawPlot();
    }

    // Triggers a full re-plot using the last received DGV values, with the current transpose
    // state applied. Called when TransposeXY is toggled.
    public void Transpose()
    {
        SetPlotData(xValuesFromDgv, yValuesFromDgv, zValuesFromDgv);
    }

    // Returns a new 2-D array that is the transpose of the input.
    public double[,] TransposeZ(double[,] array)
    {
        int rows = array.GetLength(0);
        int cols = array.GetLength(1);
        var transposed = new double[cols, rows];

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                transposed[j, i] = array[i, j];

        return transposed;
    }

    // Flags the view position to be reset to the home angle on the next DrawPlot() call.
    public void ResetViewPosition()
    {
        resetViewPosition = true;
    }

    // Prints axis and Z values to the console in a readable grid format for debugging.
    private void PrintGraphValues(double[] x, double[] y, double[,] z)
    {
        Console.Write(" ");
        for (int i = 0; i < x.Length; i++)
            Console.Write($"{x[i]} ");
        Console.Write('\r');

        for (int i = 0; i < y.Length; i++)
        {
            Console.Write($"{y[i]} ");
            for (int j = 0; j < x.Length; j++)
                Console.Write($"{z[i, j]} ");
            Console.Write('\r');
        }
    }

    // Editor3D selection callback. Called by the renderer on mouse-down and mouse-drag events.
    // Returns an eInvalidate flag that tells Editor3D whether to redraw.
    private eInvalidate OnSelectEvent(eSelEvent selEvent, Keys modifiers, int deltaX, int deltaY, cObject3D obj)
    {
        bool ctrlDown = (modifiers & Keys.Control) > 0;

        var result = eInvalidate.NoChange;

        // Alt + left mouse button down (no CTRL) â€” single point selection toggle.
        if (selEvent == eSelEvent.MouseDown && !ctrlDown && obj != null)
        {
            if (DebugPointSelectMode)
                Console.WriteLine($"{InstanceName} - {ClassName} - AltSelection event fired");

            // Delegate the actual selection state change to DgvToGraph3d via the event so
            // both the graph and DGV selection states stay in sync.
            AltSelection?.Invoke(null, obj);

            result = eInvalidate.Invalidate;
        }
        else if (selEvent == eSelEvent.MouseDrag && ctrlDown)
        {
            // Alt + CTRL + drag â€” move selected points along the Z axis only.
            // ReverseProject converts the 2D mouse delta into 3D coordinate space.
            cPoint3D projected = graph3d.ReverseProject(deltaX, deltaY);

            pointsMoved = new List<cPoint3D>();

            foreach (cPoint3D selected in graph3d.Selection.GetSelectedPoints(eSelType.All))
            {
                // Surface points have fixed X,Y â€” only Z can be edited via drag.
                selected.Move(0, 0, projected.Z);
                pointsMoved.Add(selected);
            }

            if (DebugPointMoveMode)
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dNdr event fired");

            // Notify DgvToGraph3d that new Z values are available for writing to the DGV.
            Graph3dNdr?.Invoke(null, pointsMoved);

            result = eInvalidate.CoordSystem;
        }

        return result;
    }
}
