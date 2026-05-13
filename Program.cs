using System;
using System.Windows.Forms;
using TableEditor.Forms;

namespace TableEditor;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new App());
    }
}
