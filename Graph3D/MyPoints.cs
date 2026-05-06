﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

// Plot 3D type aliases — keep these so the rest of the file can use the short names.
using cObject3D    = Plot3D.Editor3D.cObject3D;
using cPoint3D     = Plot3D.Editor3D.cPoint3D;

namespace TableEditor.Graph3D;

// A deduplicated list of MyPoint objects keyed on grid position (RowIndex, ColIndex).
// Overrides Add to enforce uniqueness so the same cell can never appear twice in a selection set.
public class MyPoints : List<MyPoint>, ICloneable
{
    // Axis label arrays — set by the owner before calling any ConvertToMyPoint overload.
    public double[] XAxisLabels { get; set; }
    public double[] YAxisLabels { get; set; }

    // Returns true if the collection already contains a point at the same grid position.
    internal bool Exists(MyPoint point) => Contains(point);

    // Adds a point only if it is not already present (uniqueness by position).
    // Returns true when actually added, false when rejected as a duplicate.
    public new bool Add(MyPoint newPoint)
    {
        if (!Contains(newPoint))
        {
            base.Add(newPoint);
            return true;
        }
        return false;
    }

    // Converts a 3D graph object to a MyPoint then adds it if unique.
    public bool Add(cObject3D obj)
    {
        var newPoint = ConvertToMyPoint(obj);
        if (!Contains(newPoint))
        {
            base.Add(newPoint);
            return true;
        }
        return false;
    }

    // Converts a DGV cell to a MyPoint then adds it if unique.
    public bool Add(DataGridViewCell cell)
    {
        var newPoint = ConvertToMyPoint(cell);
        if (!Contains(newPoint))
        {
            base.Add(newPoint);
            return true;
        }
        return false;
    }

    // Adds all cells from a DGV selected-cell collection, skipping duplicates.
    public void Add(DataGridViewSelectedCellCollection cells)
    {
        foreach (DataGridViewCell c in cells)
        {
            var newPoint = ConvertToMyPoint(c);
            if (!Contains(newPoint))
                base.Add(newPoint);
        }
    }

    public override string ToString()
    {
        var parts = this.Select(p => p.ToString()).ToArray();

        if (parts.Length > 1)
            return string.Join("\r", parts);

        return parts.Length == 1 ? parts[0] : string.Empty;
    }

    // Deep-copies the list, cloning each contained point so mutations in the clone do not
    // affect the original.
    public object Clone()
    {
        var clone = new MyPoints();
        foreach (var point in this)
        {
            if (point == null) break;
            clone.Add((MyPoint)point.Clone());
        }
        return clone;
    }

    // Builds a MyPoint from a DGV cell using the axis label arrays stored on this collection.
    public MyPoint ConvertToMyPoint(DataGridViewCell cell)
    {
        var pt = new MyPoint();

        pt.ColIndex = cell.ColumnIndex;
        pt.RowIndex = cell.RowIndex;

        // Safely parse the cell value — the DGV can hold string, double, or null depending on
        // how the column was configured.
        pt.Z = double.Parse(cell.Value.ToString());

        pt.UserSelected = cell.Selected;

        pt.XAxisTag = XAxisLabels[pt.ColIndex];
        pt.YAxisTag = YAxisLabels[pt.RowIndex];

        pt.HashCode = pt.GetHashCode();

        return pt;
    }

    // Overload that iterates a selected-cell collection and returns only the last point built.
    // Kept for backward compatibility; callers that need all cells should use Add() instead.
    public MyPoint ConvertToMyPoint(DataGridViewSelectedCellCollection cells)
    {
        var pt = new MyPoint();

        foreach (DataGridViewCell cell in cells)
        {
            pt.ColIndex = cell.ColumnIndex;
            pt.RowIndex = cell.RowIndex;
            pt.Z        = double.Parse(cell.Value.ToString());

            pt.HoverSelected = cell.Selected;

            pt.XAxisTag = XAxisLabels[pt.ColIndex];
            pt.YAxisTag = YAxisLabels[pt.RowIndex];

            pt.HashCode = pt.GetHashCode();
        }

        return pt;
    }

    // Converts a Plot3D graph object to a MyPoint. Uses tolerance-based axis lookup so
    // floating-point tags compare safely even after round-trips through the renderer.
    public MyPoint ConvertToMyPoint(cObject3D obj)
    {
        var pt = new MyPoint();

        pt.XAxisTag    = obj.Points[0].X;
        pt.YAxisTag    = obj.Points[0].Y;
        pt.Z           = obj.Points[0].Z;
        pt.HoverSelected = obj.Points[0].Selected;

        pt.ColIndex = Array.FindIndex(XAxisLabels, v => System.Math.Abs(v - pt.XAxisTag) < 1e-9);
        pt.RowIndex = Array.FindIndex(YAxisLabels, v => System.Math.Abs(v - pt.YAxisTag) < 1e-9);

        pt.HashCode = pt.GetHashCode();

        return pt;
    }

    // Converts a raw Plot3D point to a MyPoint using tolerance-based axis lookup.
    public MyPoint ConvertToMyPoint(cPoint3D obj)
    {
        var pt = new MyPoint();

        pt.XAxisTag    = obj.X;
        pt.YAxisTag    = obj.Y;
        pt.Z           = obj.Z;
        pt.HoverSelected = obj.Selected;

        pt.ColIndex = Array.FindIndex(XAxisLabels, v => System.Math.Abs(v - pt.XAxisTag) < 1e-9);
        pt.RowIndex = Array.FindIndex(YAxisLabels, v => System.Math.Abs(v - pt.YAxisTag) < 1e-9);

        pt.HashCode = pt.GetHashCode();

        return pt;
    }

    public MyPoints() { }

    // Convenience constructor that pre-populates from the DGV selection.
    public MyPoints(DataGridViewSelectedCellCollection selectedCells)
    {
        foreach (DataGridViewCell cell in selectedCells)
            base.Add(ConvertToMyPoint(cell));
    }
}
