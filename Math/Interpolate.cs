using System;
using System.Windows.Forms;
using SysMath = System.Math;

namespace TableEditor.Math;

// Provides linear interpolation over a DataGridView selection and bilinear lookup helpers
// used when importing data from an external source table.
public static class Interpolate
{
    // Performs a bilinear lookup across an entire grid of target (x, y) points against a
    // source table defined by x_Axis / y_Axis / z_Data. The result has the same dimensions
    // as the (y × x) target grid.
    public static double[,] LookUp_2D(double[] x, double[] y, double[] xAxis, double[] yAxis, double[,] zData)
    {
        double[,] result = new double[y.Length, x.Length];

        for (int i = 0; i < y.Length; i++)
        {
            for (int j = 0; j < x.Length; j++)
            {
                result[i, j] = LookUp_1D(x[j], y[i], xAxis, yAxis, zData);
            }
        }

        return result;
    }

    // Bilinear interpolation for a single (x, y) point. Clamps to the axis boundaries
    // (no extrapolation). Returns double.NaN when the four surrounding z-values collapse
    // to a degenerate rectangle (denominator ≈ zero), instead of throwing.
    public static double LookUp_1D(double x, double y, double[] xAxis, double[] yAxis, double[,] zData)
    {
        // Clamp to min axis boundaries — no extrapolation below the range
        if (x < xAxis[0]) x = xAxis[0];
        if (y < yAxis[0]) y = yAxis[0];

        // Clamp to max axis boundaries — no extrapolation above the range
        if (x > xAxis[xAxis.Length - 1]) x = xAxis[xAxis.Length - 1];
        if (y > yAxis[yAxis.Length - 1]) y = yAxis[yAxis.Length - 1];

        int x1Idx = LowerBoundIndex(x, xAxis);
        int x2Idx = UpperBoundIndex(x, xAxis);
        int y1Idx = LowerBoundIndex(y, yAxis);
        int y2Idx = UpperBoundIndex(y, yAxis);

        double x1 = LowerBoundValue(x, xAxis);
        double x2 = UpperBoundValue(x, xAxis);
        double y1 = LowerBoundValue(y, yAxis);
        double y2 = UpperBoundValue(y, yAxis);

        double z11 = zData[y1Idx, x1Idx];
        double z12 = zData[y2Idx, x1Idx];
        double z21 = zData[y1Idx, x2Idx];
        double z22 = zData[y2Idx, x2Idx];

        // Guard against a degenerate rectangle (both axis intervals ≈ 0) which would
        // produce a division-by-zero in the standard bilinear formula.
        double denom = (x2 - x1) * (y2 - y1);
        if (SysMath.Abs(denom) < double.Epsilon)
            return double.NaN;

        return (1.0 / denom) *
               (z11 * (x2 - x) * (y2 - y) +
                z21 * (x - x1) * (y2 - y) +
                z12 * (x2 - x) * (y - y1) +
                z22 * (x - x1) * (y - y1));
    }

    // Fills missing (zero) cells with the average of their valid immediate neighbours.
    // Cells that remain NaN after the neighbour search are reset to zero.
    public static double[,] MissingNeighbour(double[,] inData)
    {
        int numRows = inData.GetLength(0);
        int numCols = inData.GetLength(1);
        double[,] outData = new double[numRows, numCols];

        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numCols; j++)
            {
                if (inData[i, j] == 0)
                {
                    outData[i, j] = NearestNeighbourLinearInterpolation(inData, i, j);

                    if (double.IsNaN(outData[i, j]))
                        outData[i, j] = 0;
                }
                else
                {
                    outData[i, j] = inData[i, j];
                }
            }
        }

        return outData;
    }

    // Fills every zero cell by linear interpolation along columns then rows, recursing
    // until no further changes are possible. Vertical direction takes priority; horizontal
    // is used as a fallback when vertical neighbours are unavailable.
    public static double[,] AutoInterpolate(double[,] data)
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        double[,] result = new double[rows, cols];
        bool changed = false;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (data[i, j] == 0)
                {
                    double vertical = InterpolateVertically(data, i, j);
                    double horizontal = InterpolateHorizontally(data, i, j);

                    if (vertical != 0 || horizontal != 0)
                    {
                        changed = true;
                        result[i, j] = vertical != 0 ? vertical : horizontal;
                    }
                }
                else
                {
                    result[i, j] = data[i, j];
                }
            }
        }

        // Keep iterating until the data stabilises — each pass may unblock further cells
        return changed ? AutoInterpolate(result) : result;
    }

    // Linearly interpolates the selected DataGridView cells along the axis specified by mode.
    // The interpolated values are proportional to the axis header spacing between the
    // outermost selected cells in each column (Vertical) or row (Horizontal), producing
    // a true axis-aware linear distribution rather than a naive index-based one.
    // Calls itself twice with Vertical then Horizontal when mode == All.
    public static void InterpolateSelection(
        DataGridView dgv,
        DataGridViewSelectedCellCollection selectedCells,
        WalkMode mode)
    {
        if (mode == WalkMode.All)
        {
            InterpolateSelection(dgv, selectedCells, WalkMode.Vertical);
            InterpolateSelection(dgv, selectedCells, WalkMode.Horizontal);
            return;
        }

        SelectionWalker.Walk(dgv, selectedCells, mode, (dt, primary, minSecondary, maxSecondary) =>
        {
            // Bounds check before any data access
            if (mode == WalkMode.Vertical)
            {
                if (minSecondary >= dgv.Rows.Count || maxSecondary >= dgv.Rows.Count || primary >= dgv.Columns.Count)
                    return;
            }
            else
            {
                if (minSecondary >= dgv.Columns.Count || maxSecondary >= dgv.Columns.Count || primary >= dgv.Rows.Count)
                    return;
            }

            // Read the anchor cell values at the selection endpoints
            double minValue = mode == WalkMode.Vertical
                ? (double)dt.Rows[minSecondary][primary]
                : (double)dt.Rows[primary][minSecondary];

            double maxValue = mode == WalkMode.Vertical
                ? (double)dt.Rows[maxSecondary][primary]
                : (double)dt.Rows[primary][maxSecondary];

            // Build the axis header array up to and including the max secondary index so
            // we can measure the proportional spacing between each pair of adjacent headers.
            double[] axisList = new double[maxSecondary + 1];

            for (int i = 0; i < maxSecondary + 1; i++)
            {
                string headerText = mode == WalkMode.Vertical
                    ? dgv.Rows[i].HeaderCell.Value.ToString()
                    : dgv.Columns[i].HeaderCell.Value.ToString();

                if (!double.TryParse(headerText, out axisList[i]))
                    return;
            }

            int distance = maxSecondary - minSecondary;
            double[] cellValues = new double[distance + 1];
            cellValues[0] = minValue;
            cellValues[distance] = maxValue;

            double overallAxisSpan = axisList[maxSecondary] - axisList[minSecondary];

            for (int i = 1; i < cellValues.Length - 1; i++)
            {
                // Proportional step: how much of the total axis span does this interval occupy
                double stepSpan = axisList[minSecondary + i] - axisList[minSecondary + i - 1];
                double proportion = stepSpan / overallAxisSpan;

                double absoluteDiff = SysMath.Max(maxValue, minValue) - SysMath.Min(maxValue, minValue);
                double increment = maxValue > minValue
                    ? absoluteDiff * proportion
                    : absoluteDiff * -proportion;

                // Round to a sensible precision to avoid floating-point noise accumulation
                increment = RoundToSignificantDecimalPlaces(increment);

                cellValues[i] = cellValues[i - 1] + increment;
            }

            // Write the interpolated values back to the DataTable
            for (int i = 0; i < cellValues.Length; i++)
            {
                if (mode == WalkMode.Vertical)
                    dt.Rows[minSecondary + i][primary] = cellValues[i];
                else
                    dt.Rows[primary][minSecondary + i] = cellValues[i];
            }
        });
    }

    // Rounds small increments to a number of decimal places proportional to their magnitude,
    // preventing floating-point noise from accumulating across many interpolated cells.
    private static double RoundToSignificantDecimalPlaces(double value)
    {
        double abs = SysMath.Abs(value);

        if (abs > 1)      return SysMath.Round(value, 2);
        if (abs > 0.1)    return SysMath.Round(value, 3);
        if (abs > 0.01)   return SysMath.Round(value, 4);
        if (abs > 0.001)  return SysMath.Round(value, 5);
        if (abs > 0.0001) return SysMath.Round(value, 6);
        if (abs > 0.00001) return SysMath.Round(value, 7);
        return SysMath.Round(value, 8);
    }

    private static double NearestNeighbourLinearInterpolation(double[,] data, int row, int col)
    {
        int numRows = data.GetLength(0);
        int numCols = data.GetLength(1);

        double sum = 0;
        int count = 0;

        for (int i = System.Math.Max(0, row - 1); i <= System.Math.Min(numRows - 1, row + 1); i++)
        {
            for (int j = System.Math.Max(0, col - 1); j <= System.Math.Min(numCols - 1, col + 1); j++)
            {
                if (data[i, j] != 0)
                {
                    sum += data[i, j];
                    count++;
                }
            }
        }

        return count > 0 ? sum / count : double.NaN;
    }

    private static double InterpolateHorizontally(double[,] data, int row, int col)
    {
        double leftValue = 0;
        double rightValue = 0;
        int leftIndex = -1;
        int rightIndex = -1;

        for (int k = col - 1; k >= 0; k--)
        {
            if (data[row, k] != 0) { leftValue = data[row, k]; leftIndex = k; break; }
        }

        for (int k = col + 1; k < data.GetLength(1); k++)
        {
            if (data[row, k] != 0) { rightValue = data[row, k]; rightIndex = k; break; }
        }

        if (leftIndex == -1 || rightIndex == -1)
            return 0;

        return leftValue + (rightValue - leftValue) * (col - leftIndex) / (rightIndex - leftIndex);
    }

    private static double InterpolateVertically(double[,] data, int row, int col)
    {
        double topValue = 0;
        double bottomValue = 0;
        int topIndex = -1;
        int bottomIndex = -1;

        for (int k = row - 1; k >= 0; k--)
        {
            if (data[k, col] != 0 && !double.IsNaN(data[k, col])) { topValue = data[k, col]; topIndex = k; break; }
        }

        for (int k = row + 1; k < data.GetLength(0); k++)
        {
            if (data[k, col] != 0 && !double.IsNaN(data[k, col])) { bottomValue = data[k, col]; bottomIndex = k; break; }
        }

        if (topIndex == -1 || bottomIndex == -1)
            return 0;

        return topValue + (bottomValue - topValue) * (row - topIndex) / (bottomIndex - topIndex);
    }

    // Binary search helpers — return the axis value or index that bounds x from below or above.
    // When x lands exactly on an axis point, LowerBound == UpperBound for that dimension,
    // so the bilinear formula degenerates correctly to the point value.

    private static double LowerBoundValue(double target, double[] arr)
    {
        int idx = Array.BinarySearch(arr, target);
        if (idx >= 0)         return arr[idx];
        if (~idx > 0)         return arr[~idx - 1];
        return arr[~idx];
    }

    private static double UpperBoundValue(double target, double[] arr)
    {
        int idx = Array.BinarySearch(arr, target);
        if (idx >= 0)
            return idx < arr.Length - 1 ? arr[idx + 1] : 0;
        if (~idx < arr.Length) return arr[~idx];
        return 0;
    }

    private static int LowerBoundIndex(double target, double[] arr)
    {
        int idx = Array.BinarySearch(arr, target);
        if (idx >= 0)  return idx;
        if (~idx > 0)  return ~idx - 1;
        return ~idx;
    }

    private static int UpperBoundIndex(double target, double[] arr)
    {
        int idx = Array.BinarySearch(arr, target);
        if (idx >= 0)
            return idx < arr.Length - 1 ? idx + 1 : 0;
        if (~idx < arr.Length) return ~idx;
        return 0;
    }
}
