using System.Windows.Forms;

namespace TableEditor
{
    public partial class Graph3D_Instructions : Form
    {
        public bool DeselectAllText
        {
            set { textBox1.DeselectAll(); }
        }


        public Graph3D_Instructions()
        {
            InitializeComponent();
        }


    }
}
