﻿using System;
using System.Windows.Forms;

namespace TableEditor.Graph3D;

// Represents a single data point that can be tracked across both the DGV and the 3D graph.
// Identity is defined by grid position (RowIndex/ColIndex) only — Z is mutable and must never
// participate in equality or hashing, otherwise selections silently break when a cell is edited.
public class MyPoint : IEquatable<MyPoint>, ICloneable
{
    // Whether a nearest-point search found this point
    public bool Found { get; set; }

    // Column index in the DGV — maps to the X axis
    public int ColIndex { get; set; }

    // Row index in the DGV — maps to the Y axis
    public int RowIndex { get; set; }

    // Z (value) axis — mutable, intentionally excluded from GetHashCode / Equals
    public double Z { get; set; }

    // Axis value tag that corresponds to ColIndex in the axis label array
    public double XAxisTag { get; set; }

    // Axis value tag that corresponds to RowIndex in the axis label array
    public double YAxisTag { get; set; }

    // Axis label arrays stored on the point for convenience during conversions
    public double[] XAxisLabels { get; set; }
    public double[] YAxisLabels { get; set; }

    // Cached hash — kept as a public property so callers can store a snapshot, but the
    // authoritative value is always computed from (RowIndex, ColIndex) only.
    public int HashCode { get; set; }

    // UserSelected and HoverSelected are mutually exclusive: setting one clears the other.
    public bool UserSelected
    {
        get => userSelected;
        set
        {
            if (hoverSelected) hoverSelected = false;
            userSelected = value;
        }
    }

    public bool HoverSelected
    {
        get => hoverSelected;
        set
        {
            if (userSelected) userSelected = false;
            hoverSelected = value;
        }
    }

    bool userSelected;
    bool hoverSelected;

    // Hash is based on position only so the point's identity is stable even after Z changes.
    public override int GetHashCode() => (RowIndex * 397) ^ ColIndex;

    public override bool Equals(object obj) => obj is MyPoint other && Equals(other);

    // Two points are the same grid cell if their row and column indexes match.
    public bool Equals(MyPoint other) => other != null && RowIndex == other.RowIndex && ColIndex == other.ColIndex;

    public override string ToString() =>
        string.Format("(Col={0}, Row={1}, Z={2}, UserSel={3}, HoverSel={4})",
            ColIndex, RowIndex, Z, UserSelected, HoverSelected);

    public object Clone()
    {
        var clone = new MyPoint();

        clone.Found      = Found;
        clone.ColIndex   = ColIndex;
        clone.RowIndex   = RowIndex;
        clone.Z          = Z;
        clone.XAxisTag   = XAxisTag;
        clone.YAxisTag   = YAxisTag;
        clone.XAxisLabels = XAxisLabels;
        clone.YAxisLabels = YAxisLabels;
        clone.HashCode   = HashCode;

        // Assign via backing fields to avoid the mutual-exclusion side-effects in the setters
        // when restoring both flags from a snapshot (only one can be true at a time anyway).
        clone.userSelected  = userSelected;
        clone.hoverSelected = hoverSelected;

        return clone;
    }

    // Returns true when the point refers to a real grid position (i.e. not in the invalidated state).
    public bool IsValid() => ColIndex != -1 && RowIndex != -1;

    // Resets the point to a sentinel "no point" state. Called on construction and when the
    // underlying data changes so that stale references do not accidentally match real points.
    public void Invalidate()
    {
        Found  = false;
        ColIndex = -1;
        RowIndex = -1;
        Z      = -1;

        XAxisTag = -1;
        YAxisTag = -1;

        XAxisLabels = new double[0];
        YAxisLabels = new double[0];

        userSelected  = false;
        hoverSelected = false;

        HashCode = 0;
    }

    // Builds a fully defined MyPoint from a DGV cell. XAxisLabels / YAxisLabels must be set on
    // this instance before calling this helper.
    public MyPoint ConvertToMyPoint(DataGridViewCell cell)
    {
        var pt = new MyPoint();

        pt.ColIndex = cell.ColumnIndex;
        pt.RowIndex = cell.RowIndex;
        pt.Z = double.Parse(cell.Value.ToString());

        pt.XAxisTag = XAxisLabels[pt.ColIndex];
        pt.YAxisTag = YAxisLabels[pt.RowIndex];

        pt.HashCode = pt.GetHashCode();

        return pt;
    }

    // Default constructor — places the point in an invalid/sentinel state.
    public MyPoint()
    {
        Invalidate();
    }

    // Constructs from a DGV cell. Requires XAxisLabels / YAxisLabels to already be set.
    public MyPoint(DataGridViewCell cell)
    {
        Invalidate();
        var built = ConvertToMyPoint(cell);

        ColIndex  = built.ColIndex;
        RowIndex  = built.RowIndex;
        Z         = built.Z;
        XAxisTag  = built.XAxisTag;
        YAxisTag  = built.YAxisTag;
        HashCode  = built.HashCode;
    }

    // Constructs a fully defined point by resolving the axis tag to an index via the label arrays.
    // Uses tolerance-based lookup so floating-point labels compare safely.
    public MyPoint(double xAxisLabel, double yAxisLabel, double[] xAxisLabels, double[] yAxisLabels,
                   double z, bool isUserSelected = false, bool isHoverSelected = false)
    {
        ColIndex = Array.FindIndex(xAxisLabels, x => System.Math.Abs(x - xAxisLabel) < 1e-9);
        RowIndex = Array.FindIndex(yAxisLabels, y => System.Math.Abs(y - yAxisLabel) < 1e-9);
        Z        = z;

        HoverSelected = isHoverSelected;
        UserSelected  = isUserSelected;

        HashCode = GetHashCode();
    }
}
