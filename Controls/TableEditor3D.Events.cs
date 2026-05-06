using System;
using System.Windows.Forms;

namespace TableEditor;

// DGV and Graph3D event wiring and cross-panel routing handlers for TableEditor3D.
// Methods here translate DGV events into Graph3D updates and vice-versa so that neither
// DgvCtrl nor Graph3dCtrl need to reference each other directly.
partial class TableEditor3D
{
    // ----- DGV size / data change events -----

    // Fired (debounced) after the DGV data changes.  Pushes a fresh data set to the graph
    // so the 3D surface stays in sync without re-drawing on every individual cell edit.
    private void MyEvents_Dgv_NDR_Debounced(object sender, EventArgs e)
    {
        if (!Graph3dEnabled)
            return;

        DgvToGraph3d.XAxisLabels = ReadFromDgv_ColLabels();
        DgvToGraph3d.YAxisLabels = ReadFromDgv_RowLabels();
        DgvToGraph3d.ZValues     = ReadFromDgv_TableData();

        Graph3dCtrl.XAxisLabels = ReadFromDgv_ColLabels();
        Graph3dCtrl.YAxisLabels = ReadFromDgv_RowLabels();
        Graph3dCtrl.ZValues     = ReadFromDgv_TableData();

        Graph3dCtrl.DrawPlot();
    }

    // Fired when DGV data changes and the header values themselves have been modified (e.g.
    // after a paste-with-axis operation).  The axis label arrays must be reloaded from the
    // DGV before pushing them to the interface.
    private void MyEvents_DgvDataChangedToHeaders(object sender, EventArgs e)
    {
        if (!Graph3dEnabled)
            return;

        DgvToGraph3d.XAxisLabels = ReadFromDgv_ColLabels();
        DgvToGraph3d.YAxisLabels = ReadFromDgv_RowLabels();
    }

    // Recalculates the splitter position whenever Panel1 is resized (e.g. the user drags the
    // splitter or the host form is resized).  Keeps the DGV width preference intact.
    private void Dgv_SplitContainer_SizeChanged(object sender, EventArgs e)
    {
        if (DebugSplitContainer)
            Console.WriteLine($"{InstanceName} - Dgv_SplitContainer_SizeChanged()");

        if (DgvCtrl.DgvHasData)
            splitContainer1.SplitterDistance = CalcSplitterDistance();
    }

    // ----- Undo / paste completion callbacks -----

    // Called after an undo operation completes.  Refreshes the graph to reflect the reverted
    // data so the 3D view does not show stale values.
    private void Undo_Completed_NDR(object sender, EventArgs e)
    {
        if (!Graph3dEnabled)
            return;

        DgvToGraph3d.ZValues = ReadFromDgv_TableData();
        Graph3dCtrl.ZValues  = ReadFromDgv_TableData();
        Graph3dCtrl.DrawPlot();
    }

    // Called after a paste operation completes when the paste targets data cells only (not
    // axis headers).  Refreshes the 3D surface to reflect the pasted values.
    private void Paste_Completed_NDR(object sender, EventArgs e)
    {
        if (!Graph3dEnabled)
            return;

        DgvToGraph3d.ZValues = ReadFromDgv_TableData();
        Graph3dCtrl.ZValues  = ReadFromDgv_TableData();
        Graph3dCtrl.DrawPlot();
    }

    // ----- Average tool event handlers -----

    // Called when the Average Tool form writes averaged values back to the DGV.
    // The graph must be refreshed because cell values have been modified externally.
    private void AverageTool_DataWritten(object sender, EventArgs e)
    {
        if (!Graph3dEnabled)
            return;

        DgvToGraph3d.ZValues = ReadFromDgv_TableData();
        Graph3dCtrl.ZValues  = ReadFromDgv_TableData();
        Graph3dCtrl.DrawPlot();
    }

    // Called when the Average Tool form closes (or is dismissed).  Gives the DGV focus back
    // so keyboard shortcuts work immediately without requiring a mouse click.
    private void AverageTool_FormClosed(object sender, FormClosedEventArgs e)
    {
        DgvCtrl.dgv.Focus();
    }
}
