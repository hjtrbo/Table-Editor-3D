using System.Windows.Forms;

namespace TableEditor.Layout;

// Packages the scroll bars, header DataGridViews, blanking panel, and split container into a single
// transferable bundle. Originally existed as two identical DTOs (ScrollBarCntrls and DgvHeaderCntrls);
// merged here to eliminate duplication — both consumers need the same set of references.
public class LayoutControls
{
    public DataGridView   RowHeader       { get; set; }
    public DataGridView   ColHeader       { get; set; }
    public Panel          BlankingPanel   { get; set; }
    public HScrollBar     HScrollBar      { get; set; }
    public VScrollBar     VScrollBar      { get; set; }
    public SplitContainer SplitContainer  { get; set; }
    public string         InstanceName    { get; set; }
}
