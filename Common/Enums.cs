namespace TableEditor;

// Controls whether copy, paste, or both clipboard operations are available on a table instance.
public enum CopyPasteMode
{
    All,    // Both copy and paste are allowed
    Copy,   // Copy only — the table is read-only for paste operations
    None    // No clipboard operations
}

// Drives how much of the control is repainted after a data or style change.
// Coarser modes are cheaper; finer modes target only what changed.
public enum RefreshMode
{
    All,
    ExternallySetNumberFormat,
    Partial,
    WidthColour,
    ColourOnly,
    DpAdjust,
    StyleWidthSize,
    AverageTool
}

// Selects which part of the table receives a formatting operation.
public enum FormatTarget
{
    RowHeaders,
    ColHeaders,
    AllHeaders,
    Cells,
    All,
    None
}

// Direction for incrementing or decrementing decimal-place count.
public enum DpDirection
{
    Decrement,
    Increment
}

// Heat-map colour scheme applied to cell backgrounds.
public enum ColourScheme
{
    None,      // No heat map
    HpNormal,  // HP Tuners default gradient
    HpEdited,  // Highlights edited cells
    HpScanner  // Scanner-style colouring
}
