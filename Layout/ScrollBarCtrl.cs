using System;
using System.Drawing;
using System.Windows.Forms;
using TableEditor.DataGrid;
using Timers;

namespace TableEditor.Layout;

// Manages the custom horizontal and vertical scroll bars that replace the built-in WinForms DGV scroll bars.
// Custom scroll bars are necessary because the built-in ones cannot be reliably positioned or sized when the
// DGV is panned programmatically (the DGV resets FirstDisplayedScrollingRowIndex on every move event).
//
// Lifecycle: construct once, call Initiate() after the host form handle is created, call
// ExternalUpdateReq() whenever the containing SplitContainer changes size.
public class ScrollBarCtrl
{
    // -------------------------Properties -----------------------------------------------------------------------------------------

    // Debug class identifier — useful when multiple instances share the same log stream.
    public string ClassName    { get; set; } = "ScrlBrCtrl";
    // Propagated from the parent TableEditor3D so log lines can be traced back to the right instance.
    public string InstanceName { get; private set; }

    // When false the scroll bars, blanking panel, and header DGVs are hidden entirely (e.g. the host
    // app wants plain built-in scroll bars instead).
    public bool UseMyScrollBars      { get; set; }
    public bool DebugPosition        { get; set; }
    public bool DebugScrollBars      { get; set; }
    public bool DebugExternalEvents  { get; set; }
    public bool DebugMouseWheel      { get; set; }

    // Computed pixel counts for how far the child DGV extends beyond the visible parent area.
    public Point OffCanvasPixelCountValue { get { return OffCanvasPixelCount(); } }

    public bool HScrollShown         { get { return hScroll.Visible; } }
    public bool VScrollShown         { get { return vScroll.Visible; } }
    public int  HScrollValue         { get { return hScroll.Value; } }
    public int  VScrollValue         { get { return vScroll.Value; } }
    public int  VScrollWidth         { get { return vScroll.Width; } }
    public int  HScrollHeight        { get { return hScroll.Height; } }

    // Preferred small-increment values supplied by the caller to match row/column pixel dimensions.
    // Defaults to int.MinValue (sentinel) until explicitly set.
    public int HScrollPreferredValue
    {
        get { return hScrollPreferredValue; }
        set { hScrollPreferredValue = value; }
    }
    public int VScrollPreferredValue
    {
        get { return vScrollPreferredValue; }
        set { vScrollPreferredValue = value; }
    }

    public bool ScrollBarsReadyForStart { get; private set; }
    public bool Initiated               { get; private set; }

    // Convenience wrappers so callers don't have to know the child field name.
    private Rectangle cRect { get { return new Rectangle(child.Location, child.Size); } }
    private Point     cLoc  { get { return child.Location; }  set { child.Location = value; } }

    // -------------------------Variables ------------------------------------------------------------------------------------------

    DgvCtrl       dgvCtrl;
    Control       parent;          // The panel / split-container pane that contains the main DGV.
    DataGridView  child;           // The main (editable) DGV being scrolled.
    VScrollBar    vScroll;
    HScrollBar    hScroll;
    SplitContainer splitContainer;

    // Sentinel so we can detect "not yet set" without nullable overhead.
    int hScrollPreferredValue = int.MinValue;
    int vScrollPreferredValue = int.MinValue;

    // Used internally by OffCanvasPixelCount to expose split top/bottom off-canvas distances if needed.
    int vPixels_Top, vPixels_Bottom;

    // Saved snapshots for detecting whether a position correction actually moved anything.
    Rectangle pRectLast      = Rectangle.Empty;
    Rectangle cRectLast      = Rectangle.Empty;
    Point     offCanvasPixelsLast = Point.Empty;

    // Off-delay timer: fires after the DGV stops moving so we can do a final cleanup pass without
    // hammering the layout logic on every pixel of a drag.
    public TimerOffDelay scrlBrDgvMvTmr;

    // Raised after the scroll bars process a scroll event so the host can react (e.g. sync graph view).
    public event EventHandler<ScrollEventArgs> hScroll_Scrolled;
    public event EventHandler<ScrollEventArgs> vScroll_Scrolled;

    // -------------------------Constructor ----------------------------------------------------------------------------------------

    public ScrollBarCtrl(DgvCtrl dgvCtrl, string instanceName)
    {
        // Accept the owning DgvCtrl rather than individual references so this class stays in sync when
        // the DgvCtrl rebuilds its controls (LayoutControls is re-assigned, not individual fields).

        this.dgvCtrl       = dgvCtrl;
        this.parent        = dgvCtrl.dgv.Parent;                    // Parent panel — provides the visible viewport.
        this.child         = dgvCtrl.dgv;
        this.vScroll       = dgvCtrl.ScrollBarCntrls.VScrollBar;
        this.hScroll       = dgvCtrl.ScrollBarCntrls.HScrollBar;
        this.splitContainer = dgvCtrl.ScrollBarCntrls.SplitContainer;

        InstanceName = instanceName;

        // Debounced timer: 50 ms after the last DGV move event we run a final cleanup tick.
        scrlBrDgvMvTmr = new TimerOffDelay
        {
            Preset          = 50,
            UiControl       = dgvCtrl.dgv,
            OnTimingDone    = DgvMove_TimerOff_Tick,
            DebugTimerName  = "scrlBrDgvMvTmr",
            Debug           = DebugPosition
        };
    }

    // -------------------------Functions ------------------------------------------------------------------------------------------

    // Call once after the host form's handle has been created. Calling again is safe but resets all
    // event subscriptions, which means duplicate handlers if callers are not careful.
    public void Initiate()
    {
        if (DebugPosition)
            Console.WriteLine($"{InstanceName} - {ClassName} - Initiate()");

        // Custom scroll bars require the DGV to be free-floating (Dock = None, AutoSize = false).
        // Docked or auto-sized children reposition themselves on every layout pass, defeating the manual
        // positioning that drives our scroll logic.
        if (UseMyScrollBars && child.Dock != DockStyle.None)
            throw new Exception("Cannot use MyScrollBars with a docked child (dgv), please undock");

        if (UseMyScrollBars && child.AutoSize)
            throw new Exception("Cannot use MyScrollBars with (dgv) AutoSize == true");

        LoadEvents();

        // Run an initial pass so scroll bars reflect the data already loaded at startup.
        if (dgvCtrl.DgvHasData)
            UpdateScrollBarVisibilityAndValues();

        Initiated = true;
    }

    // Called when the SplitContainer is resized or when the host explicitly requests a layout refresh.
    // Corrects the child DGV position so there is never blank space inside the viewport.
    public void ExternalUpdateReq()
    {
        if (DebugPosition)
            Console.WriteLine($"{InstanceName} - {ClassName} - ExternalUpdateReq()");

        // 1. Parent larger than child → snap child to origin.
        // 2. Child has drifted positive (white space at top/left) → snap to origin.
        // 3. White space at bottom → shift child up to close the gap.
        // 4. White space at right → shift child left to close the gap.

        // --- Step 1: parent viewport is larger than child — just snap to origin ---
        if (pRect().Height >= cRect.Height || pRect().Width >= cRect.Width)
        {
            cRectLast = cRect;

            if (pRect().Height >= cRect.Height)
                cLoc = new Point(cLoc.X, 0);

            if (pRect().Width >= cRect.Width)
            {
                cLoc = new Point(0, cLoc.Y);
                dgvCtrl.dgvHeaders.ResetColHeaderPosition();
            }

            if (DebugPosition)
                Console.WriteLine($"{InstanceName} - {ClassName} - 1");

            goto CheckWhiteSpace;
        }

        // --- Step 2: child has positive XY (white space at top or left) → reset to origin ---
        if (cLoc.Y > 0 || cLoc.X > 0)
        {
            cRectLast = cRect;

            if (cLoc.Y > 0)
            {
                cLoc = new Point(cLoc.X, 0);
                dgvCtrl.dgvHeaders.ResetRowHeaderPosition();
            }

            if (cLoc.X > 0)
            {
                cLoc = new Point(0, cLoc.Y);
                dgvCtrl.dgvHeaders.ResetColHeaderPosition();
            }

            if (DebugPosition)
                Console.WriteLine($"{InstanceName} - {ClassName} - 2");

            goto CheckWhiteSpace;
        }

    CheckWhiteSpace:
        // --- Step 3: vertical white space at the bottom ---
        int whiteSpaceDistance = 0;
        int shiftAmount        = 0;

        if (cLoc.Y != 0)
        {
            cRectLast = cRect;
            whiteSpaceDistance = pRect().Height - (cRect.Height + cLoc.Y);

            // A positive value means the bottom of the child doesn't reach the bottom of the parent.
            if (whiteSpaceDistance > 0)
            {
                // Shift up by whichever is smaller: the amount the child is off-screen, or the gap size.
                shiftAmount = System.Math.Min(-cLoc.Y, whiteSpaceDistance);
            }

            if (shiftAmount != 0)
            {
                cLoc = new Point(cLoc.X, cLoc.Y + shiftAmount);
                dgvCtrl.dgvHeaders.PushNewHeaderLocation(true, shiftAmount);
            }

            if (DebugPosition)
                Console.WriteLine($"{InstanceName} - {ClassName} - 3");
        }

        // --- Step 4: horizontal white space at the right ---
        whiteSpaceDistance = 0;
        shiftAmount        = 0;

        if (cLoc.X != 0)
        {
            cRectLast = cRect;
            whiteSpaceDistance = pRect().Width - (cRect.Width + cLoc.X);

            if (whiteSpaceDistance > 0)
            {
                shiftAmount = System.Math.Min(System.Math.Abs(cLoc.X), whiteSpaceDistance);
            }

            if (shiftAmount > 0)
            {
                cLoc = new Point(cLoc.X + shiftAmount, cLoc.Y);
                dgvCtrl.dgvHeaders.PushNewHeaderLocation(false, shiftAmount);
            }

            if (DebugPosition)
                Console.WriteLine($"{InstanceName} - {ClassName} - 4");
        }

        UpdateScrollBarVisibilityAndValues();
    }

    // Fired by the off-delay timer after the DGV settles. Currently a no-op placeholder; logic that
    // used to run here was found to be unnecessary after moving the resize-hold approach to scroll events.
    private void DgvMove_TimerOff_Tick()
    {
        // Intentionally empty — kept as a hook point for future post-scroll cleanup if needed.
    }

    // -------------------------Event handlers -------------------------------------------------------------------------------------

    private void Dgv_NDR_Debounced(object sender, DgvData e)
    {
        if (DebugExternalEvents)
            Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_NDR_Debounced()");

        // New data arrived — recalculate whether scroll bars should appear.
        UpdateScrollBarVisibilityAndValues();
    }

    private void Parent_SizeChanged(object sender, EventArgs e)
    {
        ExternalUpdateReq();
    }

    // Lightweight move-event listener. Used for debug tracing only; heavy logic was moved out.
    private void Child_Move(object sender, EventArgs e)
    {
        if (DebugPosition || DebugScrollBars)
            Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_Move()");
    }

    // Kept for reference in case the "freeze DGV during parent resize" approach needs to be revisited.
    // The technique worked but caused subtle scroll desync and was replaced by the ExternalUpdateReq path.
    [Obsolete("Superseded by Child_Move; kept for reference until proven unused")]
    private void Child_Move_Backup(object sender, EventArgs e)
    {
        // If re-enabling, re-wire the event in LoadEvents() and restore the body below.
        //
        // When the form moves, a DGV automatically resets FirstDisplayedScrollingRowIndex to 0,0.
        // The original approach here was to re-apply the saved child.Location on every Move event
        // and use scrlBrDgvMvTmr to detect when the move had finished, then release the lock.
        //
        // Outline of the old body (variables no longer declared):
        //   if (!holdDgvStillDuringResize) return;
        //   if (!dgvResizeStartFlag) { childLocationPrev = dgv.Location; dgvResizeStartFlag = true; }
        //   scrlBrDgvMvTmr.Start();
        //   dgv.Location = childLocationPrev;
    }

    private void Scroll_MouseEnter(object sender, EventArgs e)
    {
        // Reserved — could trigger a visibility refresh if we ever implement auto-hide scroll bars.
    }

    private void Scroll_MouseLeave(object sender, EventArgs e)
    {
        // Reserved — symmetric with Scroll_MouseEnter.
    }

    private void hScroll_Scroll(object sender, ScrollEventArgs e)
    {
        if (DebugScrollBars)
            Console.WriteLine($"{InstanceName} - {ClassName} - hScroll_Scroll()");

        // Translate the scroll-bar delta into a pixel offset on the child DGV.
        Point location = child.Location;
        location.X -= e.NewValue - e.OldValue;
        child.Location = location;

        // Notify external subscribers (e.g. graph view needs to pan in sync).
        hScroll_Scrolled?.Invoke(this, e);

        // Tell the header DGV controller to pan its column header to match.
        if (dgvCtrl.dgvHeaders != null)
        {
            dgvCtrl.dgvHeaders.ScrollEventArgs   = e;
            dgvCtrl.dgvHeaders.hScrollInProgress = true;
        }
    }

    private void vScroll_Scroll(object sender, ScrollEventArgs e)
    {
        if (DebugScrollBars)
            Console.WriteLine($"{InstanceName} - {ClassName} - vScroll_Scroll()");

        Point location = child.Location;
        location.Y -= e.NewValue - e.OldValue;
        child.Location = location;

        vScroll_Scrolled?.Invoke(this, e);

        if (dgvCtrl.dgvHeaders != null)
        {
            dgvCtrl.dgvHeaders.ScrollEventArgs   = e;
            dgvCtrl.dgvHeaders.vScrollInProgress = true;
        }
    }

    private void Dgv_MouseWheel(object sender, MouseEventArgs e)
    {
        // Touchpad / mouse-wheel scrolling is handled via the custom scroll bars. This handler is wired
        // in case we need to intercept raw wheel events in future (e.g. horizontal touchpad gesture).
        // The commented block below shows the ratio-based approach that was tried and removed.
    }

    private void Parent_Scroll(object sender, ScrollEventArgs e)
    {
        // This handler existing at all means the parent has native scroll bars, which should not happen
        // in our layout. Throw so we catch the configuration mistake early in testing.
        throw new Exception("Ah huh, you did need this!");
    }

    private void Parent_MouseWheel(object sender, MouseEventArgs e)
    {
        // Native parent wheel events are suppressed in favour of vScroll_Scroll / hScroll_Scroll.
    }

    private void SplitContainer_Resize(object sender, EventArgs e)
    {
        ExternalUpdateReq();
    }

    private void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
    {
        ExternalUpdateReq();
    }

    // -------------------------Core layout helpers --------------------------------------------------------------------------------

    // Returns the usable area of the parent panel, shrunk by any visible scroll bar chrome so
    // off-canvas calculations remain consistent regardless of scroll bar visibility.
    private Rectangle pRect()
    {
        return new Rectangle(
            parent.Location.X,
            parent.Location.Y,
            parent.DisplayRectangle.Width  - (vScroll.Visible ? vScroll.Width  : 0),
            parent.DisplayRectangle.Height - (hScroll.Visible ? hScroll.Height : 0));
    }

    private void UpdateScrollBarVisibilityAndValues()
    {
        if (DebugScrollBars)
            Console.WriteLine($"{InstanceName} - {ClassName} - UpdateScrollBars()");

        OffCanvasPixelCount();

        if (DebugScrollBars)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
            Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
        }

        SetScrollBarVisibility();
        AssignScrollValues();
    }

    private void SetScrollBarVisibility()
    {
        Point offCanvas = OffCanvasPixelCount();

        // Show a scroll bar only when the child genuinely overflows in that axis.
        hScroll.Visible = offCanvas.X > 0;
        vScroll.Visible = offCanvas.Y > 0;

        // Edge case: child and parent are both at the origin and no overflow exists, but pRect() would
        // have appeared smaller because it subtracted scroll-bar widths that weren't actually needed.
        // Re-evaluate without that penalty.
        if (parent.Location.Equals(child.Location))
        {
            if (pRect().Width >= cRect.Width && pRect().Height >= cRect.Height)
            {
                hScroll.Visible = false;
                vScroll.Visible = false;
            }
        }

        if (DebugScrollBars)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - hScroll.Visible {hScroll.Visible}");
            Console.WriteLine($"{InstanceName} - {ClassName} - vScroll.Visible {vScroll.Visible}");
        }
    }

    private void AssignScrollValues()
    {
        // When the child is back at the origin there is nothing to scroll to.
        if (cRect.X == 0) hScroll.Value = 0;
        if (cRect.Y == 0) vScroll.Value = 0;

        Point offCanvas = OffCanvasPixelCount();

        if (offCanvas.X > 0)
        {
            hScroll.Minimum     = 0;
            // Use the caller-supplied preferred small change if available; fall back to a 10th of the range.
            hScroll.SmallChange = hScrollPreferredValue != int.MinValue ? hScrollPreferredValue : offCanvas.X / 10;
            hScroll.LargeChange = offCanvas.X;

            // Disable then re-enable to suppress the flicker that occurs when Maximum is reassigned
            // while Value is near the upper end of the old range.
            hScroll.Enabled = false;
            hScroll.Maximum = offCanvas.X + hScroll.LargeChange;
            hScroll.Enabled = true;
        }

        if (offCanvas.Y > 0)
        {
            vScroll.Minimum     = 0;
            vScroll.SmallChange = vScrollPreferredValue != int.MinValue ? vScrollPreferredValue : offCanvas.Y / 10;
            vScroll.LargeChange = offCanvas.Y;

            vScroll.Enabled = false;
            vScroll.Maximum = offCanvas.Y + vScroll.LargeChange;
            vScroll.Enabled = true;
        }

        if (DebugScrollBars)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - hScroll.Value = {hScroll.Value}");
            Console.WriteLine($"{InstanceName} - {ClassName} - vScroll.Value = {vScroll.Value}");
        }
    }

    // Calculates how many pixels of the child DGV lie outside the visible parent rectangle in each axis.
    // Returns a Point where X = horizontal overflow and Y = vertical overflow (both >= 0).
    private Point OffCanvasPixelCount()
    {
        int hPixels = 0, vPixels = 0;
        Rectangle parent = pRect();

        if (cRect.Left < parent.Left)
            hPixels = System.Math.Abs(cRect.Left);

        if (cRect.Top < parent.Top)
        {
            vPixels     = System.Math.Abs(cRect.Top);
            vPixels_Top = System.Math.Abs(cRect.Top);
        }

        if (cRect.Right > parent.Right)
            hPixels += System.Math.Abs(cRect.Right - parent.Right);

        if (cRect.Bottom > parent.Bottom)
        {
            vPixels        += System.Math.Abs(cRect.Bottom - parent.Bottom);
            vPixels_Bottom  = System.Math.Abs(cRect.Bottom - parent.Bottom);
        }

        return new Point(hPixels, vPixels);
    }

    private void LoadEvents()
    {
        // Data change: recalculate scroll bar range when table content changes.
        dgvCtrl.myEvents.DgvDataChanged_Debounced += Dgv_NDR_Debounced;

        // Scroll bar interaction.
        vScroll.MouseEnter += Scroll_MouseEnter;
        hScroll.MouseEnter += Scroll_MouseEnter;
        vScroll.MouseLeave += Scroll_MouseLeave;
        hScroll.MouseLeave += Scroll_MouseLeave;
        vScroll.Scroll     += vScroll_Scroll;
        hScroll.Scroll     += hScroll_Scroll;

        // Layout change: recalculate positions when the container is resized or the splitter is dragged.
        parent.SizeChanged           += Parent_SizeChanged;
        splitContainer.Resize        += SplitContainer_Resize;
        splitContainer.SplitterMoved += SplitContainer_SplitterMoved;
    }
}
