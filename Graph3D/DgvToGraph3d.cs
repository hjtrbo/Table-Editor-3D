﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
// DgvCtrl lives in the DataGrid namespace after the parallel refactor.
using TableEditor.Common;
using TableEditor.DataGrid;
using TableEditor.Timers;
// Plot 3D type aliases â€” required because Graph3dCtrl exposes cPoint3D in its NDR event.
using cObject3D    = Plot3D.Editor3D.cObject3D;
using cPoint3D     = Plot3D.Editor3D.cPoint3D;

namespace TableEditor.Graph3D;

// Bridges the DataGridView (DgvCtrl) and the 3D graph (Graph3dCtrl).
// Responsible for:
//   - Forwarding new table data from the DGV to the graph renderer.
//   - Mirroring hover and selection state between the DGV and graph in both directions.
//   - Converting graph point-drag deltas back into DGV cell writes.
public class DgvToGraph3d
{
    // ---------- Properties ----------

    public string ClassName    { get; set; } = "DgvToGraph3d";
    public string InstanceName { get; set; }

    // When true, hovering over a DGV cell selects the matching graph point and vice-versa.
    public bool MirrorPoints { get; set; }

    // When true, X and Y axes are swapped for both the DGV and the graph.
    public bool TransposeXY { get; set; }

    // Axis label arrays that track the current DGV header values.
    public double[] XAxisLabels { get; set; }
    public double[] YAxisLabels { get; set; }
    public double[,] ZValues    { get; set; }

    // Enables graph-to-DGV "target mode" click selection.
    public bool PointSelectMode { get; set; }

    // Enables graph-to-DGV point drag (Z value editing via mouse drag on the graph).
    public bool PointMoveMode { get; set; }

    // Setting this also propagates to DgvCtrl.Z_RemoteValuesChanging so the DGV knows to
    // suppress its normal edit path and accept values from the graph instead.
    public bool PointMoveInProgress
    {
        get => pointMoveInProgress;
        set
        {
            pointMoveInProgress = value;
            dgvCtrl.Z_RemoteValuesChanging = value;
        }
    }

    // Last known mouse state from the graph â€” shared across mouse event handlers.
    public MouseEventArgs MouseArgs { get; set; }

    // Debug flags â€” enable selectively to avoid flooding the console.
    public bool DebugAll             { get; set; }
    public bool DebugData            { get; set; }
    public bool DebugHoverPoint      { get; set; }
    public bool DebugSelection       { get; set; }
    public bool DebugPointMoveMode   { get; set; }
    public bool DebugTimers          { get; set; }

    // ---------- Private fields ----------

    DgvCtrl dgvCtrl;
    Graph3dCtrl graph3dCtrl;
    DgvData dgvDataPrev;

    // All live points tracked in the two-direction selection model.
    MyPoints myPoints = new MyPoints();

    // Debounce timer that throttles Z-value writes to the DGV during point drag operations.
    // Public so the host can inspect or stop it externally if needed.
    public TimerOnDelay tmrZValuesToDgvIntermittent;

    // Current and previous hover points for DGVâ†’Graph3d direction.
    MyPoint dgvHoverPoint;
    MyPoint graph3dHoverPoint;
    MyPoint dgvHoverPointPrev;
    MyPoint graph3dHoverPointPrev;

    // Selection state tracked independently for DGV and graph to detect source of changes.
    public MyPoints dgvSelections;
    MyPoints graph3dSelections;

    // Accumulated Z-value updates from the latest graph drag event.
    List<cPoint3D> zValuesToDgv = new List<cPoint3D>();

    // Backing field for PointMoveInProgress.
    bool pointMoveInProgress;

    // ---------- Constructor ----------

    public DgvToGraph3d(DgvCtrl dgvCtrl, Graph3dCtrl graph3dCtrl, string instanceName)
    {
        InstanceName = instanceName;

        this.dgvCtrl      = dgvCtrl;
        this.graph3dCtrl  = graph3dCtrl;

        // Initialise all hover point sentinels in the invalidated state.
        dgvHoverPoint         = new MyPoint();
        graph3dHoverPoint     = new MyPoint();
        dgvHoverPointPrev     = new MyPoint();
        graph3dHoverPointPrev = new MyPoint();

        dgvSelections     = new MyPoints();
        graph3dSelections = new MyPoints();

        // Wire up DGV events â€” data change drives a full or partial graph redraw.
        dgvCtrl.myEvents.DgvDataChanged_Debounced            += MyEvents_Dgv_NdrDebounced;
        dgvCtrl.myEvents.DgvSelectionChanged_ToGraph3d_Immediate += MyEvents_Dgv_SelectionChanged;

        // Graph events â€” point drag and mouse input.
        graph3dCtrl.Graph3dNdr  += Graph3dCtrl_Ndr;
        graph3dCtrl.graph3d.MouseUp += Graph3d_MouseUp;

        // Hover mirroring from DGV to graph.
        dgvCtrl.dgv.CellMouseEnter += Dgv_CellMouseEnter;

        // Alt-click selection on the graph.
        graph3dCtrl.AltSelection += Graph3dCtrl_AltClickSelection;

        // Mouse events on the graph for hover and target-mode selection.
        graph3dCtrl.graph3d.MouseClick += Graph3d_MouseClick;
        graph3dCtrl.graph3d.MouseMove  += Graph3d_MouseMove;
        graph3dCtrl.graph3d.MouseHover += Graph3d_MouseHover;

        // The Z-value write timer runs continuously while the user is dragging points and
        // delivers updates to the DGV at a fixed 125 ms interval to avoid flooding it.
        tmrZValuesToDgvIntermittent = new TimerOnDelay
        {
            Preset            = 125,
            AutoRestart       = true,
            UiControl         = dgvCtrl.dgv,
            OnTimingDone      = Timer_ZValuesToDgv_Tick,
            DebugInstanceName = InstanceName,
            DebugTimerName    = "tmrZValuesToDgv",
            Debug             = DebugTimers
        };
    }

    // ---------- Values (DGV â†’ Graph) ----------

    // Called when the DGV signals that its table data has changed (debounced).
    // Decides whether to do a full re-plot (axes changed) or a Z-only update.
    private void MyEvents_Dgv_NdrDebounced(object sender, DgvData e)
    {
        if (DebugAll || DebugData)
            Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_Dgv_NdrDebounced()");

        // Bail early if any required data is missing to avoid downstream null refs.
        if (e.RowHeaders == null || e.ColHeaders == null || e.TableData == null)
        {
            if (DebugAll || DebugData)
                Console.WriteLine($"{InstanceName} - {ClassName} - Returned: null checks failed");
            return;
        }

        XAxisLabels = (double[])e.ColHeaders.Clone();
        YAxisLabels = (double[])e.RowHeaders.Clone();

        // Keep the axis labels in sync across all selection collections.
        dgvSelections.XAxisLabels     = XAxisLabels;
        dgvSelections.YAxisLabels     = YAxisLabels;
        graph3dSelections.XAxisLabels = XAxisLabels;
        graph3dSelections.YAxisLabels = YAxisLabels;

        graph3dCtrl.XAxisLabels = XAxisLabels;
        graph3dCtrl.YAxisLabels = YAxisLabels;

        if (!e.HeadersEqual(dgvDataPrev))
        {
            // Axis dimensions changed â€” discard all stale hover and selection state then
            // do a full re-plot which also resets the view position.
            dgvHoverPoint.Invalidate();
            graph3dHoverPoint.Invalidate();
            dgvHoverPointPrev.Invalidate();
            graph3dHoverPointPrev.Invalidate();

            if (DebugAll || DebugHoverPoint)
                Console.WriteLine($"{InstanceName} - {ClassName} - Hover point invalidated");

            dgvSelections.Clear();
            graph3dSelections.Clear();

            graph3dCtrl.SetPlotData(e.ColHeaders, e.RowHeaders, e.TableData);
        }
        else
        {
            // Axis labels are unchanged â€” only Z values need updating, which is faster.
            graph3dCtrl.UpdatePlot(e.ColHeaders, e.RowHeaders, e.TableData);
        }

        dgvDataPrev = e.Copy();
    }

    // Receives new point positions from the graph during a drag operation.
    private void Graph3dCtrl_Ndr(object sender, List<cPoint3D> e)
    {
        if (DebugAll || DebugPointMoveMode)
            Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dCtrl_Ndr");

        // Prevent the DGV's own data-changed events from echoing back to the graph while we
        // are in the middle of writing graph-originated values to it.
        dgvCtrl.myEvents.Pause_DataFromGraph3d();

        // Flip into move mode â€” this also tells DgvCtrl to accept remote values.
        PointMoveInProgress = true;

        zValuesToDgv = e;

        // The timer fires every 125 ms while the user holds the mouse button down and pushes
        // the accumulated Z values into the DGV cells at a controlled rate.
        tmrZValuesToDgvIntermittent.Start();
    }

    // Timer tick: writes the current set of dragged-point Z values into the DGV.
    private void Timer_ZValuesToDgv_Tick()
    {
        if (DebugAll || DebugPointMoveMode)
            Console.WriteLine($"{InstanceName} - {ClassName} - Timer_ZValuesToDgv_Tick()");

        foreach (cPoint3D p in zValuesToDgv)
        {
            int col, row;

            if (TransposeXY)
            {
                // When transposed the graph X corresponds to DGV rows and graph Y to DGV columns.
                col = Array.FindIndex(XAxisLabels, v => System.Math.Abs(v - p.Y) < 1e-9);
                row = Array.FindIndex(YAxisLabels, v => System.Math.Abs(v - p.X) < 1e-9);
            }
            else
            {
                col = Array.FindIndex(XAxisLabels, v => System.Math.Abs(v - p.X) < 1e-9);
                row = Array.FindIndex(YAxisLabels, v => System.Math.Abs(v - p.Y) < 1e-9);
            }

            dgvCtrl.WriteDt(row, col, p.Z);
        }

        dgvCtrl.Refresh(RefreshMode.Partial);
    }

    // When the left button is released the drag is over â€” stop the timer and commit undo.
    private void Graph3d_MouseUp(object sender, MouseEventArgs e)
    {
        if (!dgvCtrl.DgvHasData)
            return;

        if (e.Button == MouseButtons.Left)
        {
            if (DebugAll || DebugPointMoveMode)
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3d_MouseUp()");

            tmrZValuesToDgvIntermittent.Stop();

            // Exiting move mode also clears Z_RemoteValuesChanging on DgvCtrl.
            PointMoveInProgress = false;

            // Capture the post-drag state as an undo snapshot.
            dgvCtrl.Undo_Set(dgvCtrl.myEvents.BuildEventArgs_DgvDataChanged_Event());

            dgvCtrl.myEvents.Resume_DataFromGraph3d();
        }
    }

    // ---------- Hover point (DGV â†’ Graph) ----------

    // Fires when the mouse enters a DGV cell. Selects the matching graph point temporarily.
    private void Dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
    {
        // Guard against conditions where hover mirroring is not possible or meaningful.
        if (!MirrorPoints || !dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn
            || dgvCtrl.undo.InProgress || dgvCtrl.paste.InProgress
            || e.RowIndex == -1 || e.ColumnIndex == -1)
        {
            graph3dHoverPoint.Invalidate();
            graph3dHoverPointPrev.Invalidate();

            if (DebugAll || DebugHoverPoint)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_CellMouseEnter() conditions not valid");

            return;
        }

        MyPoint myPoint = BuildPointFromDgvCellEventArgs(e);

        if (myPoint.IsValid())
            dgvHoverPoint = (MyPoint)myPoint.Clone();

        // Detect a multi-cell drag selection in progress via the mouse button state.
        bool multiCellSelectionInProgress = (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;

        if (multiCellSelectionInProgress)
        {
            // During a drag-select we do not want to hover-select anything; just deselect the
            // previously hover-selected graph point and bail.
            if (dgvHoverPointPrev.IsValid() && dgvHoverPointPrev.HoverSelected)
            {
                graph3dCtrl.surfaceData.GetPointAt(dgvHoverPointPrev.ColIndex, dgvHoverPointPrev.RowIndex).Selected = false;
                dgvHoverPointPrev.HoverSelected = false;

                // Mark the current cell as user-selected to fix the top-left cell issue
                // in a multi-select where it would otherwise remain unselected on the graph.
                dgvHoverPoint.UserSelected = true;
            }

            ClearHoverPoints();
            return;
        }

        // Deselect the previous hover point only if we hover-selected it â€” never deselect a
        // point the user selected intentionally.
        if (dgvHoverPointPrev.IsValid() && dgvHoverPointPrev.HoverSelected)
        {
            if (TransposeXY)
                graph3dCtrl.surfaceData.GetPointAt(dgvHoverPointPrev.RowIndex, dgvHoverPointPrev.ColIndex).Selected = false;
            else
                graph3dCtrl.surfaceData.GetPointAt(dgvHoverPointPrev.ColIndex, dgvHoverPointPrev.RowIndex).Selected = false;

            dgvHoverPointPrev.HoverSelected = false;
        }

        // Hover-select the current cell's matching graph point only when the DGV cell itself
        // is not already user-selected (user selection takes priority).
        if (dgvHoverPoint.IsValid())
        {
            if (!dgvCtrl.dgv.Rows[dgvHoverPoint.RowIndex].Cells[dgvHoverPoint.ColIndex].Selected)
            {
                if (TransposeXY)
                    graph3dCtrl.surfaceData.GetPointAt(dgvHoverPoint.RowIndex, dgvHoverPoint.ColIndex).Selected = true;
                else
                    graph3dCtrl.surfaceData.GetPointAt(dgvHoverPoint.ColIndex, dgvHoverPoint.RowIndex).Selected = true;

                dgvHoverPoint.HoverSelected = true;
            }
            else
            {
                dgvHoverPoint.UserSelected = true;
            }
        }

        dgvHoverPointPrev = (MyPoint)dgvHoverPoint.Clone();

        graph3dCtrl.Invalidate();

        if (DebugAll || DebugHoverPoint)
            Console.WriteLine($"{InstanceName} - {ClassName} - dgvHoverPoint" + dgvHoverPoint.ToString());
    }

    // Fires from the Graph3d mouse-move handler to mirror the nearest graph point back to the DGV.
    private void Graph3dToDgv_SelectHoverPoint()
    {
        if (DebugAll)
            Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dToDgv_SelectHoverPoint() start");

        // Guard: suppress hover mirroring when point-move is in progress to avoid conflicting
        // cell selections during drag.
        if (!MirrorPoints || !dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn || PointMoveMode)
        {
            graph3dHoverPoint.Invalidate();
            graph3dHoverPointPrev.Invalidate();

            if (DebugAll || DebugHoverPoint)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dToDgv_SelectHoverPoint() invalidated");
                Console.WriteLine($"{InstanceName} - {ClassName} - Exit: Mirror={MirrorPoints} HasData={dgvCtrl.DgvHasData} Drawn={graph3dCtrl.IsDrawn} MoveMode={PointMoveMode}");
            }

            return;
        }

        bool newPointFound = false;

        MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(MouseArgs);

        // Only act when we have a valid point that is different from the last hover.
        if (hoverPoint.IsValid() && !graph3dHoverPoint.Equals(hoverPoint))
        {
            newPointFound = true;

            graph3dHoverPointPrev = (MyPoint)graph3dHoverPoint.Clone();
            graph3dHoverPoint     = (MyPoint)hoverPoint.Clone();

            if (DebugAll || DebugHoverPoint)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - graph3dHoverPoint      " + graph3dHoverPoint.ToString());
                Console.WriteLine($"{InstanceName} - {ClassName} - graph3dHoverPointPrev " + graph3dHoverPointPrev.ToString());
            }
        }

        if (newPointFound)
        {
            // Select the corresponding DGV cell and graph point only when the DGV cell is not
            // already user-selected â€” user selection always takes priority over hover.
            if (TransposeXY)
            {
                if (!dgvCtrl.dgv.Rows[graph3dHoverPoint.ColIndex].Cells[graph3dHoverPoint.RowIndex].Selected)
                {
                    dgvCtrl.dgv.Rows[graph3dHoverPoint.ColIndex].Cells[graph3dHoverPoint.RowIndex].Selected = true;
                    graph3dCtrl.surfaceData.GetPointAt(graph3dHoverPoint.ColIndex, graph3dHoverPoint.RowIndex).Selected = true;
                    graph3dHoverPoint.HoverSelected = true;
                }
                else
                {
                    graph3dHoverPoint.UserSelected = true;
                }
            }
            else
            {
                if (!dgvCtrl.dgv.Rows[graph3dHoverPoint.RowIndex].Cells[graph3dHoverPoint.ColIndex].Selected)
                {
                    dgvCtrl.dgv.Rows[graph3dHoverPoint.RowIndex].Cells[graph3dHoverPoint.ColIndex].Selected = true;
                    graph3dCtrl.surfaceData.GetPointAt(graph3dHoverPoint.ColIndex, graph3dHoverPoint.RowIndex).Selected = true;
                    graph3dHoverPoint.HoverSelected = true;
                }
                else
                {
                    graph3dHoverPoint.UserSelected = true;
                }
            }

            // Deselect the previous hover point in both DGV and graph, but only if we placed
            // it (HoverSelected) â€” never remove a user-placed selection.
            if (TransposeXY)
            {
                if (graph3dHoverPointPrev.HoverSelected)
                {
                    dgvCtrl.dgv.Rows[graph3dHoverPointPrev.ColIndex].Cells[graph3dHoverPointPrev.RowIndex].Selected = false;
                    graph3dCtrl.surfaceData.GetPointAt(graph3dHoverPointPrev.ColIndex, graph3dHoverPointPrev.RowIndex).Selected = false;
                    graph3dHoverPointPrev.HoverSelected = false;
                }
            }
            else
            {
                if (graph3dHoverPointPrev.HoverSelected)
                {
                    dgvCtrl.dgv.Rows[graph3dHoverPointPrev.RowIndex].Cells[graph3dHoverPointPrev.ColIndex].Selected = false;
                    graph3dCtrl.surfaceData.GetPointAt(graph3dHoverPointPrev.ColIndex, graph3dHoverPointPrev.RowIndex).Selected = false;
                    graph3dHoverPointPrev.HoverSelected = false;
                }
            }
        }

        graph3dCtrl.Invalidate();

        if (DebugAll)
            Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dToDgv_SelectHoverPoint() end");
    }

    // Clears all hover-based selections in both directions and redraws the graph.
    public void ClearHoverPoints()
    {
        // DGV hover point â€” deselect matching graph point if we placed the selection.
        if (dgvHoverPoint.IsValid() && dgvHoverPoint.HoverSelected)
        {
            if (TransposeXY)
            {
                if (dgvCtrl.dgv.RowCount >= dgvHoverPoint.RowIndex && dgvCtrl.dgv.ColumnCount >= dgvHoverPoint.ColIndex)
                    dgvCtrl.dgv.Rows[dgvHoverPoint.RowIndex].Cells[dgvHoverPoint.ColIndex].Selected = false;
                graph3dCtrl.surfaceData.GetPointAt(dgvHoverPoint.RowIndex, dgvHoverPoint.ColIndex).Selected = false;
            }
            else
            {
                if (dgvCtrl.dgv.RowCount >= dgvHoverPoint.RowIndex && dgvCtrl.dgv.ColumnCount >= dgvHoverPoint.ColIndex)
                    dgvCtrl.dgv.Rows[dgvHoverPoint.RowIndex].Cells[dgvHoverPoint.ColIndex].Selected = false;
                graph3dCtrl.surfaceData.GetPointAt(dgvHoverPoint.ColIndex, dgvHoverPoint.RowIndex).Selected = false;
            }
        }

        // Graph3d hover point â€” deselect matching DGV cell if we placed the selection.
        if (graph3dHoverPoint.IsValid() && graph3dHoverPoint.HoverSelected)
        {
            if (TransposeXY)
            {
                dgvCtrl.dgv.Rows[graph3dHoverPoint.ColIndex].Cells[graph3dHoverPoint.RowIndex].Selected = false;
                graph3dCtrl.surfaceData.GetPointAt(graph3dHoverPoint.ColIndex, graph3dHoverPoint.RowIndex).Selected = false;
            }
            else
            {
                dgvCtrl.dgv.Rows[graph3dHoverPoint.RowIndex].Cells[graph3dHoverPoint.ColIndex].Selected = false;
                graph3dCtrl.surfaceData.GetPointAt(graph3dHoverPoint.ColIndex, graph3dHoverPoint.RowIndex).Selected = false;
            }
        }

        dgvHoverPoint.Invalidate();
        graph3dHoverPoint.Invalidate();
        dgvHoverPointPrev.Invalidate();
        graph3dHoverPointPrev.Invalidate();

        if (DebugAll || DebugHoverPoint)
            Console.WriteLine($"{InstanceName} - {ClassName} - Hover points invalidated");

        graph3dCtrl.Invalidate();
    }

    // ---------- Selection ----------

    // DGV selection changed â€” rebuild graph selections to match.
    private void MyEvents_Dgv_SelectionChanged(object sender, DgvEvents.SelectEventArgs e)
    {
        if (!MirrorPoints || !dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn)
            return;

        if (DebugAll || DebugSelection)
            Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_Dgv_SelectionChanged");

        // Rebuild from scratch so we never accumulate stale entries from previous selections.
        dgvSelections.Clear();
        graph3dSelections.Clear();

        graph3dCtrl.ClearGraphSelection();

        dgvSelections.Add(e.SelectedCellCollection);

        if (dgvSelections.Count > 0)
        {
            foreach (MyPoint pt in dgvSelections)
            {
                if (TransposeXY)
                    graph3dCtrl.surfaceData.GetPointAt(pt.RowIndex, pt.ColIndex).Selected = true;
                else
                    graph3dCtrl.surfaceData.GetPointAt(pt.ColIndex, pt.RowIndex).Selected = true;
            }
        }

        // If the previous hover point is now part of the user selection, promote it so we
        // don't accidentally deselect it during the next hover cycle.
        if (dgvHoverPoint.IsValid() && dgvSelections.Contains(dgvHoverPointPrev))
            dgvHoverPointPrev.UserSelected = true;

        graph3dCtrl.Invalidate();
    }

    // Alt-click on the graph toggles the hover point's user-selected state.
    private void Graph3dCtrl_AltClickSelection(object sender, cObject3D e)
    {
        graph3dHoverPoint.UserSelected = !graph3dHoverPoint.UserSelected;

        // Upgrading to user-selected means it is no longer just hover-selected.
        if (graph3dHoverPoint.UserSelected)
            graph3dHoverPoint.HoverSelected = false;

        if (DebugAll || DebugHoverPoint)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dCtrl_AltClickSelection()");
            Console.WriteLine($"{InstanceName} - {ClassName} - graph3dHoverPoint " + graph3dHoverPoint.ToString());
        }
    }

    // Mouse click on the graph â€” used in target-mode (PointSelectMode) to select/deselect.
    private void Graph3d_MouseClick(object sender, MouseEventArgs e)
    {
        if (!PointSelectMode)
            return;

        MouseArgs = e;

        MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(e);

        if (hoverPoint == null || !hoverPoint.Found)
            return;

        // Toggle the previous hover point's user-selected state â€” this naturally handles both
        // selecting and deselecting with a single click.
        graph3dHoverPointPrev.UserSelected = !graph3dHoverPointPrev.UserSelected;
    }

    // Mouse move â€” updates MouseArgs and triggers hover-point mirroring.
    private void Graph3d_MouseMove(object sender, MouseEventArgs e)
    {
        MouseArgs = e;

        if (PointMoveInProgress)
            return;

        // Pause DGV selection events while we temporarily select hover cells so those
        // programmatic selections are not mistaken for user selections.
        dgvCtrl.myEvents.Pause_SelectionFromGraph3d();
        Graph3dToDgv_SelectHoverPoint();
        dgvCtrl.myEvents.Resume_SelectionFromGraph3d();

        MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(MouseArgs);

        // Show crosshair cursor when the user can interact with a point under the mouse.
        if (((Control.ModifierKeys & Keys.Alt) == Keys.Alt || PointSelectMode) && hoverPoint.IsValid())
            graph3dCtrl.graph3d.Cursor = Cursors.Cross;
        else
            graph3dCtrl.graph3d.Cursor = Cursors.Default;
    }

    // Mouse hover (stationary) â€” refreshes the cursor in case the modifier key state changed.
    private void Graph3d_MouseHover(object sender, EventArgs e)
    {
        MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(MouseArgs);

        if (((Control.ModifierKeys & Keys.Alt) == Keys.Alt || PointSelectMode) && hoverPoint.IsValid())
            graph3dCtrl.graph3d.Cursor = Cursors.Cross;
        else
            graph3dCtrl.graph3d.Cursor = Cursors.Default;
    }

    // ---------- Helpers ----------

    // Constructs a MyPoint from a DGV cell event, reading Z safely to handle null/non-double values.
    private MyPoint BuildPointFromDgvCellEventArgs(DataGridViewCellEventArgs e)
    {
        var pt = new MyPoint();

        pt.ColIndex = e.ColumnIndex;
        pt.RowIndex = e.RowIndex;

        if (pt.ColIndex == -1 || pt.RowIndex == -1)
            return new MyPoint(); // returns a sentinel / invalid point

        // Guard against null or non-numeric cell values â€” the DGV can hold strings if the
        // column is not strongly typed, or null if the row was just added.
        var raw = dgvCtrl.dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        pt.Z = raw is double d ? d : (double.TryParse(raw?.ToString(), out var parsed) ? parsed : double.NaN);

        try
        {
            pt.XAxisTag = XAxisLabels[e.ColumnIndex];
            pt.YAxisTag = YAxisLabels[e.RowIndex];
        }
        catch
        {
            // Axis label array may be shorter than the DGV during a resize transition; ignore.
        }

        pt.HashCode = pt.GetHashCode();

        return pt;
    }
}
