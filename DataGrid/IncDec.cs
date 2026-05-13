using System;
using System.Globalization;
using System.Windows.Forms;
using Timers;
using Key = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;

namespace TableEditor.DataGrid;

// Handles keyboard-driven increment/decrement of selected DataGridView cells.
// The speed of change ramps up the longer the +/- key is held.
public class IncDec
{
    public string ClassName { get; set; } = "IncDec";
    public string InstanceName
    {
        get { return instanceName; }
        set { instanceName = value; tmr_IncDec.DebugInstanceName = value; }
    }
    public bool Debug
    {
        get { return debug; }
        set { debug = value; tmr_IncDec.Debug = value; }
    }
    public bool StopRequest { get; set; } = false;

    public event EventHandler IncDec_Incremental_NDR;
    public event EventHandler IncDec_Completed_NDR;

    public struct KeyIncDec
    {
        public bool Add;
        public bool Subtract;
        public int  Counter;
        public bool Enabled;
    }
    public KeyIncDec Mode;

    public enum IncDecSpeedMode
    {
        LowLow,
        Low,
        Med,
        High,
        HighHigh
    }

    // Private fields
    DgvCtrl dgvCtrl;
    public TimerOnDelay tmr_IncDec;
    double initialIncrement;
    bool newSpeedMode;
    double increment = 0;
    bool debug;
    string instanceName;
    IncDecSpeedMode speedMode;

    public IncDec(DgvCtrl dgvCtrl)
    {
        this.dgvCtrl = dgvCtrl;

        tmr_IncDec = new TimerOnDelay
        {
            Preset         = 125,
            AutoRestart    = true,
            UiControl      = dgvCtrl.dgv,
            OnTimingDone   = IncDecTimer_Tick,
            DebugTimerName = "tmr_IncDec"
        };
    }

    public void Start()
    {
        // Only called on the initial key press. The timer tick drives subsequent steps.

        tmr_IncDec.DebugInstanceName = InstanceName;

        Mode.Counter = 0;

        string formattedValue = dgvCtrl.dgv.SelectedCells[0].FormattedValue.ToString();

        if (decimal.TryParse(formattedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            initialIncrement = InitialIncrement(result);
        }
        else
        {
            initialIncrement = 1;
        }

        if (Debug)
            Console.WriteLine($"{InstanceName} - {ClassName} - Initial increment = {initialIncrement}");

        speedMode    = IncDecSpeedMode.LowLow;
        Mode.Enabled = true;
        newSpeedMode = true;

        if (Keyboard.IsKeyDown(Key.Add))
        {
            Mode.Add      = true;
            Mode.Subtract = false;
        }
        else if (Keyboard.IsKeyDown(Key.Subtract))
        {
            Mode.Add      = false;
            Mode.Subtract = true;
        }

        tmr_IncDec.Start();
    }

    private void IncDecTimer_Tick()
    {
        Mode.Counter++;

        if (Mode.Counter >= 30)
        {
            speedMode    = IncDecSpeedMode.HighHigh;
            newSpeedMode = true;
        }
        else if (Mode.Counter >= 20)
        {
            speedMode    = IncDecSpeedMode.High;
            newSpeedMode = true;
        }
        else if (Mode.Counter >= 10)
        {
            speedMode    = IncDecSpeedMode.Med;
            newSpeedMode = true;
        }
        else if (Mode.Counter >= 5)
        {
            speedMode    = IncDecSpeedMode.Low;
            newSpeedMode = true;
        }
        else if (Mode.Counter >= 0)
        {
            speedMode    = IncDecSpeedMode.LowLow;
            newSpeedMode = true;
        }

        IncDecCellValue();
    }

    private void IncDecCellValue()
    {
        if (newSpeedMode)
        {
            switch (speedMode)
            {
                case IncDecSpeedMode.LowLow:
                    increment = initialIncrement;
                    break;

                case IncDecSpeedMode.Low:
                    increment = initialIncrement * 2;
                    break;

                case IncDecSpeedMode.Med:
                    increment = initialIncrement * 4;
                    break;

                case IncDecSpeedMode.High:
                    increment = initialIncrement * 8;
                    break;

                case IncDecSpeedMode.HighHigh:
                    increment = initialIncrement * 16;
                    break;

                default:
                    increment = 1;
                    break;
            }

            newSpeedMode = false;
        }

        foreach (DataGridViewCell cell in dgvCtrl.dgv.SelectedCells)
        {
            double value = dgvCtrl.ReadDt(cell.RowIndex, cell.ColumnIndex);

            if (Mode.Add)
                value += increment;
            else if (Mode.Subtract)
                value -= increment;

            dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, value);
        }

        IncDec_Incremental_NDR?.Invoke(null, EventArgs.Empty);

        if (Debug)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - Increment  = {increment}");
            Console.WriteLine($"{InstanceName} - {ClassName} - Speed mode = {speedMode}");
            Console.WriteLine($"{InstanceName} - {ClassName} - Raise IncDec_Incremental_NDR");
        }

        if (StopRequest)
        {
            if (Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Stop request executing");

            Stop();
        }
    }

    public void Stop()
    {
        tmr_IncDec.Stop();

        Mode.Add      = false;
        Mode.Subtract = false;
        Mode.Counter  = 0;
        Mode.Enabled  = false;
        StopRequest   = false;

        if (Debug)
        {
            Console.WriteLine($"{InstanceName} - {ClassName} - Stop executed");
            Console.WriteLine($"{InstanceName} - {ClassName} - Fired IncDec_Completed_NDR()");
        }

        IncDec_Completed_NDR?.Invoke(null, EventArgs.Empty);
    }

    public double InitialIncrement(decimal myValue)
    {
        int intDigits, decDigits;

        (intDigits, decDigits) = GetSignificantIntegerAndDecimalDigitPlaces(myValue);

        if (decDigits > 0)
        {
            initialIncrement = 1 * System.Math.Pow(10, -decDigits);
            return initialIncrement;
        }

        int modExponent = intDigits - 4;

        if (modExponent > 0)
        {
            initialIncrement = 1 * System.Math.Pow(10, (double)modExponent);
            return initialIncrement;
        }
        else
        {
            initialIncrement = 1;
            return initialIncrement;
        }
    }

    private (int, int) GetSignificantIntegerAndDecimalDigitPlaces(decimal myValue)
    {
        int sigWholeDigit;
        int sigDecimalDigit;

        int iValue = (int)myValue;

        if (iValue > 0)
            sigWholeDigit = iValue.ToString().Length;
        else
            sigWholeDigit = 0;

        string s = myValue.ToString();

        if (s.Contains("."))
        {
            string decimalPart = myValue.ToString().Split('.')[1];
            sigDecimalDigit = decimalPart.Length;
        }
        else
        {
            sigDecimalDigit = 0;
        }

        return (sigWholeDigit, sigDecimalDigit);
    }
}
