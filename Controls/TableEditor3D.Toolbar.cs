using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using TableEditor.Clipboard;
using TableEditor.Forms;
using TableEditor.DataGrid;
using TableEditor.Math;

namespace TableEditor;

// All toolbar button click handlers and context-menu handlers for TableEditor3D.
// The Main_Timer polling loop lives here because it drives toolbar button state.
partial class TableEditor3D
{
    // ----- Main polling timer -----

    // Runs every 200 ms and synchronises toolbar button states with the current runtime state.
    // The timerBusy flag prevents a slow tick from being re-entered if the UI thread is
    // saturated (e.g. large data refresh).
    private void Main_Timer_Tick(object sender, EventArgs e)
    {
        if (timerBusy)
            return;

        timerBusy = true;

        try
        {
            // Transpose button icon tracks the current transpose state of the graph.
            if (Graph3dEnabled && !Graph3dCtrl.TransposeXY)
                btn_Plot3d_TransposeXY.ImageIndex = 4;
            else
                btn_Plot3d_TransposeXY.ImageIndex = 5;

            // Average tool requires axis headers to be meaningful.
            btn_AverageTool.Enabled = AverageEnabled && DgvCtrl.DgvHasData;

            // Point-move mode button is only meaningful when there are selected graph points.
            if (Graph3dEnabled && UseToolBar && DgvToGraph3d.dgvSelections.Count() > 0)
            {
                btn_Graph3d_PointMoveMode.Enabled = true;
            }
            else
            {
                btn_Graph3d_PointMoveMode.Enabled = false;

                // Also switch off point-move mode so the graph stops tracking cursor movement.
                if (Graph3dEnabled)
                    Graph3dPointMoveMode = false;
            }

            // Hide layout controls that were not set up for this instance.
            if (!UseMyScrollBars)
            {
                rowHeader.Visible     = false;
                colHeader.Visible     = false;
                blankingPanel.Visible = false;
                hScrollBar.Visible    = false;
                vScrollBar.Visible    = false;
            }

            // Disable graph3D-specific buttons when graph is not in use.
            if (!Graph3dEnabled || !UseToolBar)
            {
                btn_Graph3D_Instructions.Enabled  = false;
                btn_Graph3d_PointMoveMode.Enabled = false;
                btn_Graph3d_PointSelectMode.Enabled = false;
                btn_Graph3D_ResetView.Enabled     = false;
                btn_Plot3d_TransposeXY.Enabled    = false;
                btn_RotateGraphDockedLocation.Enabled = false;
            }

            if (!UndoEnabled || !UseToolBar)
            {
                btn_Undo.Enabled = false;
                btn_Redo.Enabled = false;
            }

            if (!CopyPasteEnabled || !UseToolBar)
            {
                btn_Undo.Enabled  = false;
                btn_Paste.Enabled = false;
            }
        }
        finally
        {
            timerBusy = false;
        }
    }

    // ----- Context menu -----

    private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
    {
        if (DebugMouse)
            Console.WriteLine($"{InstanceName} - contextMenuStrip_Opening()");

        // Reset all items; the block below re-enables only what is appropriate.
        ToolStrip_CopyWithAxis.Enabled     = false;
        ToolStrip_Copy.Enabled             = false;
        ToolStrip_Paste.Enabled            = false;
        ToolStrip_PasteWithXYAxis.Enabled  = false;
        ToolStrip_PasteSpecial.Enabled     = false;
        ToolStrip_AddtnlPasteFcns.Enabled  = false;

        if (CopyPasteEnabled)
        {
            if (DgvCtrl.DgvHasData)
            {
                if (copyPasteMode == CopyPasteMode.All)
                {
                    ToolStrip_CopyWithAxis.Enabled    = true;
                    ToolStrip_Copy.Enabled            = true;
                    ToolStrip_PasteSpecial.Enabled    = true;
                    ToolStrip_PasteWithXYAxis.Enabled = true;
                    ToolStrip_Paste.Enabled           = true;
                    ToolStrip_AddtnlPasteFcns.Enabled = true;

                    ToolStrip_PasteXAxisPCMTEC.Enabled = true;
                    ToolStrip_PasteXAxis.Enabled       = true;
                    ToolStrip_PasteYAxis.Enabled       = true;
                    ToolStrip_ClearTable.Enabled       = true;
                }
                else if (copyPasteMode == CopyPasteMode.Copy)
                {
                    ToolStrip_CopyWithAxis.Enabled = true;
                    ToolStrip_Copy.Enabled         = true;
                }
            }
            else
            {
                // DGV is empty: only operations that can build a new table are allowed.
                ToolStrip_PasteWithXYAxis.Enabled         = true;
                ToolStrip_AddtnlPasteFcns.Enabled         = true;

                ToolStrip_PasteXAxisPCMTEC.Enabled        = false;
                ToolStrip_PasteXAxis.Enabled              = false;
                ToolStrip_PasteYAxis.Enabled              = false;
                ToolStrip_PasteTable.Enabled              = true;
                ToolStrip_PasteTableWithRowAxis.Enabled   = true;
                ToolStrip_PasteTableWithColAxis.Enabled   = true;
                ToolStrip_ClearTable.Enabled              = false;
            }
        }
    }

    private void CopyWithAxis_Click(object sender, EventArgs e)
    {
        Copy.CopyClipboard(DgvCtrl, Copy.SelectMode.SelectedCells, Copy.Headers.Include, UseMyScrollBars);
    }

    private void CopyWithNoAxis_Click(object sender, EventArgs e)
    {
        Copy.CopyClipboard(DgvCtrl, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, UseMyScrollBars);
    }

    private void PasteTableWithXYAxis_Click(object sender, EventArgs e)
    {
        DgvNumberFormat.CelLckOut = false;
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteTableWithXYAxis);
    }

    private void PasteTableWithXAxis_Click(object sender, EventArgs e)
    {
        DgvNumberFormat.CelLckOut = false;
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteTableWithXAxis);
    }

    private void PasteTableWithYAxis_Click(object sender, EventArgs e)
    {
        DgvNumberFormat.CelLckOut = false;
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteTableWithYAxis);
    }

    private void PasteTableWithNoAxis_Click(object sender, EventArgs e)
    {
        DgvNumberFormat.CelLckOut = false;
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteTableWithNoAxis);
        DgvCtrl.Refresh(RefreshMode.All);
    }

    private void Paste_Click(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteToCurrentCell);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void PasteXAxis_PCMTEC_Click(object sender, EventArgs e)
    {
        PasteWithXAxisPcmtecDialog dialog = new PasteWithXAxisPcmtecDialog(DgvCtrl);

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        dialog.ShowDialog();

        if (dialog.X_Axis != null)
            DgvCtrl.WriteColHeaderLabels(dialog.X_Axis, dialog.NumberFormat);

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
    }

    private void PasteXAxis_Click(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteXAxis);
        DgvCtrl.Refresh(RefreshMode.All);
    }

    private void PasteYAxis_Click(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteYAxis);
        DgvCtrl.Refresh(RefreshMode.All);
    }

    private void Paste_MultiplyByPercent(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteSpecial_MultiplyByPercent);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void Paste_MultiplyByPercentHalf(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteSpecial_MultiplyByPercentHalf);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void Paste_DivideByPercent(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteSpecial_DivideByPercent);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void Paste_DivideByPercentHalf(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteSpecial_DivideByPercentHalf);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void Paste_Add(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteSpecial_Add);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void Paste_Subtract(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteSpecial_Subtract);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    private void ClearTable_Click(object sender, EventArgs e)
    {
        DgvNumberFormat.CelLckOut = false;
        DgvCtrl.myEvents.Pause_SelectionToGraph3d();
        DgvCtrl.ResetDataTable();
        DgvCtrl.StyleOverrides(DgvCtrl.dgv);
        Graph3dCtrl.Reset();
        DgvCtrl.myEvents.Resume_SelectionToGraph3d();
    }

    private void btn_Copy_Click(object sender, EventArgs e)
    {
        Copy.CopyClipboard(DgvCtrl, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, UseMyScrollBars);
    }

    private void btn_Paste_Click(object sender, EventArgs e)
    {
        DgvCtrl.paste.ParseClipboardToDgv(DgvCtrl, Paste.eMode.PasteToCurrentCell);
        DgvCtrl.Refresh(RefreshMode.StyleWidthSize);
    }

    // ----- Sample data buttons -----

    private void ShowSampleButtons()
    {
        if (ShowSamples)
        {
            btn_LoadSample1.Show();
            btn_LoadSample2.Show();
            btn_LoadSample3.Show();
            btn_LoadSample4.Show();
        }
        else
        {
            btn_LoadSample1.Hide();
            btn_LoadSample2.Hide();
            btn_LoadSample3.Hide();
            btn_LoadSample4.Hide();
        }
    }

    // Optional default args allow the Demo form to call these directly without a sender/event.
    public void btn_LoadSample1_Click(object sender = null, EventArgs e = null)
    {
        DgvCtrl.WriteToDataGridView(
            SampleData.RowHeaders1, SampleData.ColHeaders1, SampleData.TableData1,
            RefreshMode.All);

        if (UseMyScrollBars)
        {
            DgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(DgvData.ConvertNumericHeadersToText(SampleData.RowHeaders1));
            DgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(SampleData.ColHeaders1));
        }

        DgvCtrl.SetCellWidths();
    }

    public void btn_LoadSample2_Click(object sender = null, EventArgs e = null)
    {
        DgvCtrl.WriteToDataGridView(
            SampleData.RowHeaders2, SampleData.ColHeaders2, SampleData.TableData2,
            RefreshMode.All);

        if (UseMyScrollBars)
        {
            DgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(DgvData.ConvertNumericHeadersToText(SampleData.RowHeaders2));
            DgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(SampleData.ColHeaders2));
        }

        DgvCtrl.SetCellWidths();
    }

    public void btn_LoadSample3_Click(object sender = null, EventArgs e = null)
    {
        DgvCtrl.WriteToDataGridView(
            SampleData.RowHeaders3, SampleData.ColHeaders3, SampleData.TableData3,
            RefreshMode.All);

        if (UseMyScrollBars)
        {
            DgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(DgvData.ConvertNumericHeadersToText(SampleData.RowHeaders3));
            DgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(SampleData.ColHeaders3));
        }

        DgvCtrl.SetCellWidths();
    }

    public void btn_LoadSample4_Click(object sender = null, EventArgs e = null)
    {
        DgvCtrl.WriteToDataGridView(
            SampleData.RowHeaders4, SampleData.ColHeaders4, SampleData.TableData4,
            RefreshMode.All);

        DgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(SampleData.RowHeaders4Text);
        DgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(SampleData.ColHeaders4));

        DgvCtrl.SetCellWidths();
    }

    // ----- Interpolation buttons -----

    private void btn_FillMissingDataGaps_Click(object sender, EventArgs e)
    {
        if (DgvCtrl.DgvHasData)
        {
            DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
            DgvCtrl.WriteToDataTable(Interpolate.AutoInterpolate(DgvCtrl.ReadDataTable()));
            DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
            DgvCtrl.Refresh(RefreshMode.ColourOnly);
        }
    }

    private void btn_MissingNeighbourFill_Click(object sender, EventArgs e)
    {
        if (DgvCtrl.DgvHasData)
        {
            DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
            DgvCtrl.WriteToDataTable(Interpolate.MissingNeighbour(DgvCtrl.ReadDataTable()));
            DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
            DgvCtrl.Refresh(RefreshMode.ColourOnly);
        }
    }

    private void btn_All_Smooth_Click(object sender, EventArgs e)
    {
        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
        Smooth.SmoothSelection(DgvCtrl.dgv, DgvCtrl.SelectedCellCollection, WalkMode.All);
        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        DgvCtrl.Refresh(RefreshMode.ColourOnly);
    }

    private void btn_H_Smooth_Click(object sender, EventArgs e)
    {
        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
        Smooth.SmoothSelection(DgvCtrl.dgv, DgvCtrl.SelectedCellCollection, WalkMode.Horizontal);
        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        DgvCtrl.Refresh(RefreshMode.ColourOnly);
    }

    private void btn_V_Smooth_Click(object sender, EventArgs e)
    {
        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
        Smooth.SmoothSelection(DgvCtrl.dgv, DgvCtrl.SelectedCellCollection, WalkMode.Vertical);
        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        DgvCtrl.Refresh(RefreshMode.ColourOnly);
    }

    private void btn_H_Interpolate_Click(object sender, EventArgs e)
    {
        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
        Interpolate.InterpolateSelection(DgvCtrl.dgv, DgvCtrl.SelectedCellCollection, WalkMode.Horizontal);
        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        DgvCtrl.Refresh(RefreshMode.ColourOnly);
    }

    private void btn_V_Interpolate_Click(object sender, EventArgs e)
    {
        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
        Interpolate.InterpolateSelection(DgvCtrl.dgv, DgvCtrl.SelectedCellCollection, WalkMode.Vertical);
        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        DgvCtrl.Refresh(RefreshMode.ColourOnly);
    }

    private void btn_All_Interpolate_Click(object sender, EventArgs e)
    {
        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();
        Interpolate.InterpolateSelection(DgvCtrl.dgv, DgvCtrl.SelectedCellCollection, WalkMode.All);
        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        DgvCtrl.Refresh(RefreshMode.ColourOnly);
    }

    // ----- Math / adjust buttons -----

    private void btn_SetSelectedCellsValue_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, adjustValue);

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.Partial);
    }

    private void btn_Multiply_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
        {
            double.TryParse(cell.Value.ToString(), out double cellValue);
            cellValue *= adjustValue;
            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
        }

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.WidthColour);
    }

    private void btn_Divide_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        // Guard against divide-by-zero before touching any cells.
        if (adjustValue == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
        {
            double.TryParse(cell.Value.ToString(), out double cellValue);
            cellValue /= adjustValue;
            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
        }

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.WidthColour);
    }

    private void btn_Add_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
        {
            double.TryParse(cell.Value.ToString(), out double cellValue);
            cellValue += adjustValue;
            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
        }

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.WidthColour);
    }

    private void btn_Subtract_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
        {
            double.TryParse(cell.Value.ToString(), out double cellValue);
            cellValue -= adjustValue;
            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
        }

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.WidthColour);
    }

    private void btn_ClipMax_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
        {
            double value = (double)DgvCtrl.ReadDt(cell.RowIndex, cell.ColumnIndex);

            if (value > adjustValue)
                value = adjustValue;

            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, value);
        }

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.WidthColour);
    }

    private void btn_ClipMin_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || DgvCtrl.SelectedCellCollection.Count == 0 || textBox_Adjust.Text.Length == 0)
            return;

        DgvCtrl.Events_CellDataAndSelectionChanged_Pause();

        double.TryParse(textBox_Adjust.Text, out double adjustValue);

        foreach (DataGridViewCell cell in DgvCtrl.SelectedCellCollection)
        {
            double value = (double)DgvCtrl.ReadDt(cell.RowIndex, cell.ColumnIndex);

            if (value < adjustValue)
                value = adjustValue;

            DgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, value);
        }

        DgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

        DgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;
        DgvCtrl.Refresh(RefreshMode.WidthColour);
    }

    private void textBox_Adjust_KeyPress(object sender, KeyPressEventArgs e)
    {
        // Only allow digits, sign characters, decimal point, and control keys (e.g. backspace).
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
            e.KeyChar != '.' && e.KeyChar != '+' && e.KeyChar != '-')
        {
            e.Handled = true;
        }

        // Prevent a second decimal point from being entered.
        if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
            e.Handled = true;
    }

    private void btn_AverageTool_Click(object sender = null, EventArgs e = null)
    {
        if (!AverageEnabled)
            return;

        if (averageTool == null)
            averageTool = new AverageTool();

        // Reload the table if dimensions have changed (or on first open).
        if (DgvCtrl.ReadRowHeaders().Length != averageTool.RowHeaders?.Length ||
            averageTool.RowHeaders == null ||
            DgvCtrl.ReadColHeaders().Length != averageTool.ColHeaders?.Length ||
            averageTool.ColHeaders == null)
        {
            averageTool.LoadTable(DgvCtrl.ReadRowHeaders(), DgvCtrl.ReadColHeaders(), DgvNumberFormat, true);
        }

        // Reload if axis values have changed even though the dimension is the same.
        if (!DgvCtrl.ReadRowHeaders().SequenceEqual(averageTool.RowHeaders) ||
            !DgvCtrl.ReadColHeaders().SequenceEqual(averageTool.ColHeaders))
        {
            averageTool.LoadTable(DgvCtrl.ReadRowHeaders(), DgvCtrl.ReadColHeaders(), DgvNumberFormat, false);
        }

        averageTool.Show();
    }

    // ----- Decimal-places buttons -----

    private void btn_IncDp_Click(object sender, EventArgs e)
    {
        DgvCtrl.AdjustDecimalPlaces(DpDirection.Increment);
    }

    private void btn_DecDp_Click(object sender, EventArgs e)
    {
        DgvCtrl.AdjustDecimalPlaces(DpDirection.Decrement);
    }

    // ----- Graph3D mode buttons -----

    private void btn_Graph3d_PointSelectMode_Click(object sender, EventArgs e)
    {
        Graph3dPointSelectMode = !Graph3dPointSelectMode;

        btn_Graph3d_PointSelectMode.ImageIndex = Graph3dPointSelectMode ? 7 : 6;

        // Point-move requires at least one selected point AND point-select to be active.
        if (Graph3dPointSelectMode && DgvToGraph3d.dgvSelections.Count > 0)
            btn_Graph3d_PointMoveMode.Enabled = true;
        else
        {
            btn_Graph3d_PointMoveMode.Enabled = false;
            Graph3dPointMoveMode = false;
        }
    }

    private void btn_Graph3d_PointMoveMode_Click(object sender, EventArgs e)
    {
        Graph3dPointMoveMode = !Graph3dPointMoveMode;
    }

    private void btn_Graph3D_ResetView_Click(object sender, EventArgs e)
    {
        Graph3dCtrl.ResetViewPosition();
        Graph3dCtrl.DrawPlot();
    }

    private void btn_Graph3D_Instructions_Click(object sender, EventArgs e)
    {
        // Bring an existing instructions window to the front rather than opening a second one.
        foreach (Form openForm in Application.OpenForms)
        {
            if (openForm is Graph3DInstructionsDialog)
            {
                openForm.Focus();
                return;
            }
        }

        Graph3DInstructionsDialog graph3DInstructions = new Graph3DInstructionsDialog();
        graph3DInstructions.Show();
        graph3DInstructions.DeselectAllText = true;
    }

    private void btn_Plot3d_Transpose_Click(object sender, EventArgs e)
    {
        if (!DgvCtrl.DgvHasData || !Graph3dCtrl.IsDrawn)
            return;

        transposeXY = !transposeXY;

        DgvToGraph3d.ClearHoverPoints();
        DgvClearSelection();
        Graph3dCtrl.ClearGraphSelection();

        // Refresh all axis and value references so the transposed graph draws correctly.
        DgvToGraph3d.XAxisLabels = ReadFromDgv_ColLabels();
        DgvToGraph3d.YAxisLabels = ReadFromDgv_RowLabels();
        DgvToGraph3d.ZValues     = ReadFromDgv_TableData();

        Graph3dCtrl.XAxisLabels = ReadFromDgv_ColLabels();
        Graph3dCtrl.YAxisLabels = ReadFromDgv_RowLabels();
        Graph3dCtrl.ZValues     = ReadFromDgv_TableData();

        DgvCtrl.TransposeXY     = transposeXY;
        Graph3dCtrl.TransposeXY = transposeXY;
        DgvToGraph3d.TransposeXY = transposeXY;
    }

    private void btn_Options_Click(object sender, EventArgs e)
    {
        // Open the settings dialog using the service-managed instance so the singleton
        // settings state is updated and the SettingsChanged event fires on save.
        UserSettingsDialog settingsDialog = new UserSettingsDialog();
        settingsDialog.StartPosition = FormStartPosition.CenterScreen;
        settingsDialog.ShowDialog();
    }

    private void btn_RotateGraphDockedLocation_Click(object sender, EventArgs e)
    {
        // If no data has been drawn yet, treat the display as hidden so the button cycles
        // from Hide → Right on the first press.
        if (!Graph3dCtrl.IsDrawn)
            graphDisplay = GraphDisplay.Hide;

        if (DebugSplitContainer)
            Console.WriteLine($"{InstanceName} - GraphDisplay {graphDisplay}");

        if (graphDisplay == GraphDisplay.Hide)
            graphDisplay = GraphDisplay.Right;
        else
            graphDisplay++;

        switch (graphDisplay)
        {
            case GraphDisplay.Right:
                // Restore a saved splitter distance if available; otherwise snap to the DGV edge.
                splitContainer1.SplitterDistance = lastSplitterDistance != 0
                    ? lastSplitterDistance
                    : CalcSplitterDistance();
                splitContainer1.Panel2Collapsed = false;
                break;

            case GraphDisplay.Fill:
                // Save before hiding Panel1 so we can restore it when cycling back.
                lastSplitterDistance = splitContainer1.SplitterDistance;
                splitContainer1.Panel1Collapsed = true;
                break;

            case GraphDisplay.Hide:
                splitContainer1.Panel2Collapsed = true;
                splitContainer1.Panel1Collapsed = false;
                break;
        }
    }

    // ----- DGV editing buttons -----

    private void btn_ClearAllSelections_Click(object sender, EventArgs e)
    {
        DgvCtrl.ClearSelection();

        if (Graph3dEnabled)
        {
            Graph3dCtrl.ClearGraphSelection();
            DgvToGraph3d.ClearHoverPoints();
        }
    }

    private void btn_Undo_Click(object sender, EventArgs e)
    {
        DgvCtrl.Undo_Get();
    }

    private void btn_Redo_Click(object sender, EventArgs e)
    {
        DgvCtrl.Redo_Get();
    }

    private void btn_TableEditMode_Click(object sender, EventArgs e)
    {
        ColourScheme colourScheme = DgvCtrl.ColourTheme;

        // Toggle between normal-edit and scanner (error-highlight) colour themes.
        if (colourScheme == ColourScheme.HpNormal || colourScheme == ColourScheme.HpEdited)
            colourScheme = ColourScheme.HpScanner;
        else
            colourScheme = ColourScheme.HpNormal;

        DgvCtrl.SetCellColour(colourScheme);
    }
}
