﻿using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using TableEditor;


namespace TableEditor.DataGrid;

// Applies heat-map background colours to DataGridView cells based on the active
// ColourScheme. Extracted from DgvCtrl to keep colouring logic self-contained.
public static class CellColorizer
{
    // Named constants replacing magic numbers in the colour-blend logic
    private const double HpNormalLowerBand = 0.66;
    private const double HpNormalMidBand   = 0.41;
    private const double HpEditedMidBand   = 0.5;
    private const double ScannerRangeMin   = -25.0;
    private const double ScannerRangeMax   =  25.0;

    public static void SetCellColour(DataGridView dgv, DataTable dt, ColourScheme colourScheme)
    {
        if (dgv.Rows.Count == 0 || dgv.Columns.Count == 0)
            return;

        // HP Editor green / orange colour scheme
        Color grnOrg_MaxPos = Color.FromArgb(255, 165, 0); // Orange
        Color grnOrg_MinPos = Color.FromArgb(255, 255, 0);
        Color grnOrg_MinNeg = Color.FromArgb(255, 255, 0);
        Color grnOrg_MaxNeg = Color.FromArgb(0, 255, 0);   // Green

        // HP Editor purple / pink colour scheme
        Color prplPnk_MaxPos = Color.FromArgb(255, 160, 160); // Pink
        Color prplPnk_MinPos = Color.FromArgb(255, 218, 218);
        Color prplPnk_MinNeg = Color.FromArgb(219, 219, 255);
        Color prplPnk_MaxNeg = Color.FromArgb(179, 179, 255); // Purple

        // HP Scanner red / green colour scheme
        Color redGrn_MaxPos = Color.FromArgb(255, 0, 0);   // Red
        Color redGrn_MinPos = Color.FromArgb(255, 255, 255); // White
        Color redGrn_MinNeg = Color.FromArgb(255, 255, 255);
        Color redGrn_MaxNeg = Color.FromArgb(0, 255, 0);   // Green

        double tableMinValue;
        double tableMaxValue;

        switch (colourScheme)
        {
            case ColourScheme.HpNormal:
            case ColourScheme.HpEdited:
                (tableMinValue, tableMaxValue) = GetTableMinMaxValues(dt);
                break;
            case ColourScheme.HpScanner:
                tableMinValue = ScannerRangeMin;
                tableMaxValue = ScannerRangeMax;
                break;
            default:
                return;
        }

        for (int rowIndex = 0; rowIndex < dgv.Rows.Count; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < dgv.Columns.Count; columnIndex++)
            {
                DataGridViewCell cell = dgv.Rows[rowIndex].Cells[columnIndex];

                double.TryParse(cell.Value.ToString(), out double tableCellValue);

                double ratio = (tableMaxValue - tableMinValue) != 0
                    ? (tableCellValue - tableMinValue) / (tableMaxValue - tableMinValue)
                    : 0;

                Color cellColour;

                switch (colourScheme)
                {
                    case ColourScheme.HpNormal:
                        cellColour = GetColourValue(grnOrg_MaxPos, grnOrg_MinPos, grnOrg_MinNeg, grnOrg_MaxNeg, HpNormalLowerBand, ratio);
                        break;
                    case ColourScheme.HpEdited:
                        cellColour = GetColourValue(prplPnk_MaxPos, prplPnk_MinPos, prplPnk_MinNeg, prplPnk_MaxNeg, HpNormalMidBand, ratio);
                        break;
                    case ColourScheme.HpScanner:
                        cellColour = GetColourValue(redGrn_MaxPos, redGrn_MinPos, redGrn_MinNeg, redGrn_MaxNeg, HpEditedMidBand, ratio);
                        break;
                    default:
                        continue;
                }

                cell.Style.BackColor = cellColour;
            }
        }
    }

    public static (double min, double max) GetTableMinMaxValues(DataTable dt)
    {
        double tableMinValue = 0;
        double tableMaxValue = 0;

        for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < dt.Columns.Count; columnIndex++)
            {
                double value = (double)dt.Rows[rowIndex][columnIndex];
                tableMinValue = System.Math.Min(tableMinValue, value);
                tableMaxValue = System.Math.Max(tableMaxValue, value);
            }
        }

        return (tableMinValue, tableMaxValue);
    }

    public static Color GetColourValue(Color maxPos, Color minPos, Color minNeg, Color maxNeg, double midPoint, double normRatio)
    {
        double colourRatio;
        int r = 0, g = 0, b = 0;

        if (normRatio > midPoint)
            colourRatio = (normRatio - midPoint) / (1 - midPoint);
        else
            colourRatio = normRatio / midPoint;

        if (normRatio > midPoint)
        {
            r = maxPos.R - minPos.R != 0 ? (int)(minPos.R + colourRatio * (maxPos.R - minPos.R)) : maxPos.R;
            g = maxPos.G - minPos.G != 0 ? (int)(minPos.G + colourRatio * (maxPos.G - minPos.G)) : maxPos.G;
            b = maxPos.B - minPos.B != 0 ? (int)(minPos.B + colourRatio * (maxPos.B - minPos.B)) : maxNeg.B;
        }
        else
        {
            r = maxNeg.R - minNeg.R != 0 ? (int)(maxNeg.R - colourRatio * (maxNeg.R - minNeg.R)) : maxNeg.R;
            g = maxNeg.G - minNeg.G != 0 ? (int)(maxNeg.G - colourRatio * (maxNeg.G - minNeg.G)) : maxNeg.G;
            b = maxNeg.B - minNeg.B != 0 ? (int)(maxNeg.B - colourRatio * (maxNeg.B - minNeg.B)) : maxNeg.B;
        }

        r = System.Math.Max(0, System.Math.Min(255, r));
        g = System.Math.Max(0, System.Math.Min(255, g));
        b = System.Math.Max(0, System.Math.Min(255, b));

        return Color.FromArgb(r, g, b);
    }
}
