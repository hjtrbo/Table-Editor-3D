using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Data;
using TableEditor.DataGrid;

namespace TableEditor.Math;

// Applies a weighted moving-average smoothing pass to a DataGridView selection.
// Endpoints of each selected run are pinned so the smoothing blends only the interior values.
public static class Smooth
{
    // Fraction of the delta between each cell's value and its local moving average that is
    // removed per pass. 0 = no smoothing, 1 = full averaging. Default 0.5.
    public static double Weight { get; set; } = 0.5;

    // Alternates which direction runs first in All mode so repeated calls converge uniformly
    // rather than always favouring one axis.
    private static bool verticalFirst = false;

    // Smooths the selected cells along Vertical columns, Horizontal rows, or both.
    // In All mode the two directions alternate who goes first across successive calls
    // so repeated presses converge evenly on both axes.
    public static void SmoothSelection(
        DataGridView dgv,
        DataGridViewSelectedCellCollection selectedCells,
        WalkMode mode)
    {
        if (mode == WalkMode.All)
        {
            if (verticalFirst)
            {
                SmoothSelection(dgv, selectedCells, WalkMode.Vertical);
                SmoothSelection(dgv, selectedCells, WalkMode.Horizontal);
            }
            else
            {
                SmoothSelection(dgv, selectedCells, WalkMode.Horizontal);
                SmoothSelection(dgv, selectedCells, WalkMode.Vertical);
            }
            verticalFirst = !verticalFirst;
            return;
        }

        SelectionWalker.Walk(dgv, selectedCells, mode, (dt, primary, minSecondary, maxSecondary) =>
        {
            int distance = maxSecondary - minSecondary;
            double[] values = new double[distance + 1];

            // Read the current cell values for this column (Vertical) or row (Horizontal) slice
            for (int i = minSecondary; i <= maxSecondary; i++)
            {
                values[i - minSecondary] = mode == WalkMode.Vertical
                    ? (double)dt.Rows[i][primary]
                    : (double)dt.Rows[primary][i];
            }

            // Apply the moving-average smoothing kernel to the interior points
            values = MovingAvg(values);

            // Write the smoothed values back; endpoints are unchanged by MovingAvg
            for (int i = minSecondary; i <= maxSecondary; i++)
            {
                if (mode == WalkMode.Vertical)
                    dt.Rows[i][primary] = values[i - minSecondary];
                else
                    dt.Rows[primary][i] = values[i - minSecondary];
            }
        });
    }

    // Weighted moving-average over an array. The first and last elements are pinned
    // (unchanged) so the selection endpoints act as anchors. Interior points are nudged
    // toward their local neighbourhood average by the current Weight factor — a value
    // of 1 fully replaces with the average; 0 leaves the data unchanged.
    private static double[] MovingAvg(double[] data, int windowSize = 2)
    {
        double[] result = new double[data.Length];

        // Pin the first point
        result[0] = data[0];

        for (int i = 1; i < data.Length - 1; i++)
        {
            int startIdx = System.Math.Max(0, i - windowSize / 2);
            int endIdx   = System.Math.Min(data.Length - 1, i + windowSize / 2);

            double sum = 0;
            for (int j = startIdx; j <= endIdx; j++)
                sum += data[j];

            double average = sum / (endIdx - startIdx + 1);
            double delta = data[i] - average;

            // Pull the value toward the local average by the configured weight fraction
            result[i] = data[i] - delta * Weight;
        }

        // Pin the last point
        result[data.Length - 1] = data[data.Length - 1];

        return result;
    }

    // List<double> overload — used internally when chaining multiple smoothing passes
    // during iterative convergence experiments (e.g. MovingAvgIterations_Example).
    private static List<double> MovingAvg(List<double> data, int windowSize = 2)
    {
        var result = new List<double>(data.Count);

        result.Add(data[0]);

        for (int i = 1; i < data.Count - 1; i++)
        {
            int startIdx = System.Math.Max(0, i - windowSize / 2);
            int endIdx   = System.Math.Min(data.Count - 1, i + windowSize / 2);

            double sum = 0;
            for (int j = startIdx; j <= endIdx; j++)
                sum += data[j];

            double average = sum / (endIdx - startIdx + 1);
            double delta = data[i] - average;

            result.Add(data[i] - delta * Weight);
        }

        result.Add(data[data.Count - 1]);

        return result;
    }
}
