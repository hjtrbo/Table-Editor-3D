using System;
using System.Windows.Forms;

namespace TableEditor
{
    public partial class PasteWithXAxisPcmtecDialog : Form
    {
        public string InstanceName { get; set; } = "Pcmtec_X_Axis";
        public double[] X_Axis { get { return x_Axis; } }
        public string NumberFormat { get; private set; }


        private DgvCtrl dgvCtrl;
        private double[] x_Axis;


        public PasteWithXAxisPcmtecDialog(DgvCtrl dgvCtrl)
        {
            this.dgvCtrl = dgvCtrl;

            InitializeComponent();
        }

        private void ParseTextInput()
        {
            // Quick, loose and dirty
            try
            {
                string[] sArray1D = textBox_PCMTEC_X_Axis.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                string[,] sArray2D = new string[sArray1D.Length, 2];

                int k = 0;
                foreach (string s in sArray1D)
                {
                    string[] ss = s.Split(new[] { "\t" }, StringSplitOptions.None);

                    sArray2D[k, 0] = ss[1]; // cell number
                    sArray2D[k, 1] = ss[0]; // rpm

                    k++;
                }

                double[,] d = new double[sArray2D.GetLength(0), sArray2D.GetLength(1)];

                for (int i = 0; i < sArray2D.GetLength(0); i++)
                {
                    d[i, 0] = double.Parse(sArray2D[i, 0]);
                    d[i, 1] = double.Parse(sArray2D[i, 1]);
                }

                x_Axis = new double[sArray2D.GetLength(0)];

                int numberOfBlanksToFillCounter = 0;
                double previouslyLoadedRPM = 0;
                int pointerToInputArray = 0;
                int pointerToInputArray_Prev = 0;

                for (int i = 0; i < sArray2D.GetLength(0); i++)
                {
                    if (i == d[pointerToInputArray, 0]) // match found
                    {
                        x_Axis[i] = d[pointerToInputArray, 1]; // put rpm value into array at position i

                        if (numberOfBlanksToFillCounter > 0) // go back and fill gaps in array
                        {
                            double delta = (x_Axis[i] - previouslyLoadedRPM) / (numberOfBlanksToFillCounter + 1); // increment value
                            int multiplier = 1;

                            for (int j = i - numberOfBlanksToFillCounter; j < i; j++)
                            {
                                x_Axis[j] = previouslyLoadedRPM + delta * multiplier;
                                multiplier++;
                            }
                            numberOfBlanksToFillCounter = 0;
                        }

                        previouslyLoadedRPM = d[pointerToInputArray, 1]; // save previous index and rpm values

                        // Look ahead to handle consecutive input array indexes. When done the result is our next unique entry index
                        if (i < sArray2D.GetLength(0) - 2)
                            while (d[pointerToInputArray + 1, 0] == d[pointerToInputArray, 0])
                                pointerToInputArray++;

                        pointerToInputArray_Prev = pointerToInputArray;
                        pointerToInputArray++;
                    }
                    else // count number of times around without a match
                        numberOfBlanksToFillCounter++;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"{InstanceName} PasteWithXAxisPcmtecDialog() {ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}");
#endif

                DialogResult result = MessageBox.Show("Could not parse input. Please check correct format and length.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                if (result == DialogResult.Cancel)
                {
                    Close();
                }
            }
        }

        private void btn_Done_Click(object sender, EventArgs e)
        {
            ParseTextInput();

            if (x_Axis != null)
                if (x_Axis.Length != dgvCtrl.dgv.ColumnCount)
                    x_Axis = null;

            if (x_Axis == null)
            {
                DialogResult result = MessageBox.Show("Could not parse input. Please check correct format and length.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                if (result == DialogResult.Cancel)
                {
                    Close();
                }
            }
            else // all good
            {
                // Get number format
                NumberFormat = Utils.FormatDouble(x_Axis);

                Close();
            }
        }
    }
}
