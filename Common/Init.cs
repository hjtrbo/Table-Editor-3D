using System.Drawing;

namespace TableEditor.Common;

// Sizing constants that govern the initial and minimum dimensions of the control and
// its embedded 3D graph pane.  All values are in pixels.  Changing these constants is
// the single place to adjust layout geometry across the whole control.
public static class Init
{
    public static Size Graph3dMinimumSize    => new Size(350, 350);

    // Slightly larger than the minimum so the opening zoom level looks correct.
    public static Size Graph3dInitialSize    => new Size(400, 400);

    public static Size FormOpeningSize       => new Size(880, 600);

    // Adjust if the DGV or graph area clips at minimum size.
    public static Size FormMinimumSize       => new Size(376, 496);

    public static int ToolBarHeight          => 76;
    public static int SplitContainerSplitterWidth   => 8;
    public static int SplitContainerPanel1MinSize   => 125;
    public static int SplitContainerPanel2MinSize   => Graph3dMinimumSize.Width;

    // Distance is computed so the graph pane opens at its initial width.
    public static int SplitContainerSplitterDistance =>
        FormOpeningSize.Width - Graph3dMinimumSize.Width - SplitContainerSplitterWidth;
}
