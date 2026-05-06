using System.Windows.Forms;

namespace TableEditor.Forms;

// Read-only help window for 3D graph controls. Thin wrapper around a Designer-managed TextBox
// that is populated from the embedded resource string at design time.
public partial class Graph3DInstructionsDialog : Form
{
    // Caller sets this to false after showing the form to scroll the text box back to the top,
    // because WinForms auto-selects all text when a TextBox receives focus, which scrolls it down.
    public bool DeselectAllText
    {
        set { textBox1.DeselectAll(); }
    }

    public Graph3DInstructionsDialog()
    {
        InitializeComponent();
    }
}
