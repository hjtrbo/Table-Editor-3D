using System;
using System.Windows.Forms;
using TableEditor.Common;
using TableEditor.DataGrid;

namespace TableEditor.Forms;

// Dialog for pasting a PCMtec x-axis mapping (cell-number / RPM pairs) and converting it into
// a contiguous double[] axis suitable for use as column headers in the main table editor.
// The PCMtec export format is two columns separated by a tab: RPM value then cell index.
public partial class PasteWithXAxisPcmtecDialog : Form
{
    public string InstanceName { get; set; } = "Pcmtec_X_Axis";

    // The parsed axis array. Callers read this after the dialog closes with DialogResult.OK.
    public double[] X_Axis { get { return xAxis; } }

    // The detected number format string (e.g. "N0", "N1") derived from the parsed values.
    public string NumberFormat { get; private set; }

    // The DgvCtrl from the host editor — used only to validate that the parsed axis length
    // matches the table column count.
    private DgvCtrl dgvCtrl;
    private double[] xAxis;

    public PasteWithXAxisPcmtecDialog(DgvCtrl dgvCtrl)
    {
        this.dgvCtrl = dgvCtrl;
        InitializeComponent();
    }

    // ---- Parsing logic ---------------------------------------------------------------------

    private void ParseTextInput()
    {
        // Intentionally permissive: catches all exceptions and shows a retry/cancel prompt
        // rather than crashing, because the input is free-form clipboard text.
        try
        {
            // Split on both Windows and Unix line endings to handle clipboard variations.
            string[] rows = textBox_PCMTEC_X_Axis.Text.Split(
                new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string[,] pairs = new string[rows.Length, 2];

            int k = 0;
            foreach (string row in rows)
            {
                string[] cols = row.Split(new[] { "\t" }, StringSplitOptions.None);
                pairs[k, 0] = cols[1]; // cell index (column 1 in the PCMtec export)
                pairs[k, 1] = cols[0]; // RPM value  (column 0 in the PCMtec export)
                k++;
            }

            // Convert the string pairs to doubles; any parse failure throws immediately.
            double[,] d = new double[pairs.GetLength(0), pairs.GetLength(1)];
            try
            {
                for (int i = 0; i < pairs.GetLength(0); i++)
                {
                    d[i, 0] = double.Parse(pairs[i, 0]);
                    d[i, 1] = double.Parse(pairs[i, 1]);
                }
            }
            catch
            {
                throw new Exception();
            }

            xAxis = new double[pairs.GetLength(0)];

            int blanksCounter = 0;
            double previousRpm = 0;
            int inputPtr = 0;

            // Fill the output axis by linearly interpolating gaps between known cell-index
            // entries. PCMtec often omits intermediate cell numbers when they share an RPM.
            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                if (i == d[inputPtr, 0]) // current output index matches a known cell index
                {
                    xAxis[i] = d[inputPtr, 1];

                    if (blanksCounter > 0) // back-fill the skipped positions
                    {
                        double delta = (xAxis[i] - previousRpm) / (blanksCounter + 1);
                        int multiplier = 1;

                        for (int j = i - blanksCounter; j < i; j++)
                        {
                            xAxis[j] = previousRpm + delta * multiplier;
                            multiplier++;
                        }
                        blanksCounter = 0;
                    }

                    previousRpm = d[inputPtr, 1];

                    // Advance past any consecutive duplicate cell-index entries so the next
                    // unique index is used for the look-ahead comparison.
                    if (i < pairs.GetLength(0) - 2)
                        while (d[inputPtr + 1, 0] == d[inputPtr, 0])
                            inputPtr++;

                    inputPtr++;
                }
                else
                {
                    blanksCounter++;
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine(
                $"{InstanceName} ParseTextInput() {ex.Message} at line " +
                $"{ex.StackTrace?.Substring(ex.StackTrace.LastIndexOf(":line"))}");
#endif
            DialogResult retry = MessageBox.Show(
                "Could not parse input. Please check correct format.",
                "Error",
                MessageBoxButtons.RetryCancel,
                MessageBoxIcon.Error);

            if (retry == DialogResult.Cancel)
                Close();
        }
    }

    // ---- Button handlers -------------------------------------------------------------------

    private void btn_Done_Click(object sender, EventArgs e)
    {
        ParseTextInput();

        string message = "";
        bool trim = false;

        if (xAxis != null)
        {
            if (xAxis.Length != dgvCtrl.dgv.ColumnCount)
            {
                trim = true;
                message = "Length of the attempted x axis paste is not the same as the tables x axis length. ";
            }
        }
        else
        {
            message = "X axis result was null";
        }

        if (xAxis == null || trim)
        {
            if (xAxis == null)
            {
                // Fatal — cannot continue without a valid array.
                DialogResult result = MessageBox.Show(
                    "Could not parse input. " + message,
                    "Error",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Cancel)
                    Close();
                else
                    return; // retry — let the user edit the text box
            }

            if (trim)
            {
                // Non-fatal — the user can choose to write only as many columns as exist.
                DialogResult result = MessageBox.Show(
                    "Could not parse input. " + message,
                    "Error",
                    MessageBoxButtons.AbortRetryIgnore,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Abort)
                {
                    xAxis = null;
                    Close();
                }
                else if (result == DialogResult.Ignore)
                {
                    // Proceed with whatever columns were parsed — caller handles the mismatch.
                    NumberFormat = NumberFormatter.FormatDouble(xAxis);
                    Close();
                }
                else
                {
                    return; // retry
                }
            }
        }
        else
        {
            // Happy path — axis length matches the table.
            NumberFormat = NumberFormatter.FormatDouble(xAxis);
            Close();
        }
    }
}
