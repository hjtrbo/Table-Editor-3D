using System;
using System.Windows.Forms;
using System.Collections.Generic;
using TableEditor.DataGrid;

namespace TableEditor.Math;

// Determines which axis direction SelectionWalker groups selected cells along.
// Vertical groups by column (interpolate/smooth down each column independently).
// Horizontal groups by row (interpolate/smooth across each row independently).
// All runs both passes in sequence.
public enum WalkMode
{
    Vertical,
    Horizontal,
    All
}
