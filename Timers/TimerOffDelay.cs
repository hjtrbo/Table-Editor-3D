using MicroLibrary;
using System;
using System.Windows.Forms;

namespace Timers;

// Off-delay timer: the output stays true for the Preset duration after the enabling signal goes
// false. Repeated Start() calls while timing reload the accumulator, extending the delay (retrig
// behaviour). Replicates the PLC TOF instruction.
public class TimerOffDelay
{
    #region Properties

    // Timer preset value in milliseconds
    public int Preset { get; set; }

    // Timer accumulator value in ms. Decrements as timer is timing down
    public int Accumulator { get; private set; }

    // Timer timing. On when timer is timing down
    public bool TimerTiming { get; private set; }

    // Timer period done. Stays high until Start() or Stop() is next called
    public bool TimerDone { get; set; }

    // Debug mode. Writes status to console with timer information
    public bool Debug { get; set; }

    // The name of the class that owns this timer instance
    public string DebugInstanceName { get; set; }

    // Meaningful name for the debug console output
    public string DebugTimerName { get; set; }

    // Form control reference used to marshal the done event back to the UI thread.
    // Stored per-instance so that multiple timers can target different controls.
    public Control UiControl { get; set; }

    #endregion

    #region Variables

    private readonly MyStopWatch stopWatch; // for debugging
    private readonly MicroTimer taskTimer;

    public delegate void TimingDoneCallback();

    // Assign your callback method to this field to be notified when timing completes.
    public TimingDoneCallback OnTimingDone;

    #endregion

    #region Constructor

    public TimerOffDelay()
    {
        // New MicroTimer and add event handler
        taskTimer = new MicroTimer();
        taskTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(Tick);

        // Stopwatch for debug mode
        stopWatch = new MyStopWatch();

        // Set tick interval. 2 ms resolution gives consistent ticks without excessive CPU spin.
        taskTimer.Interval = 2000;
    }

    #endregion

    #region Functions

    // Starts the timer off delay. Repeated calls whilst the timer is running reset the accumulator
    // to the preset value (retriggerable). Replicates setting the Enabled property true then false.
    public void Start()
    {
        if (Debug && !TimerTiming)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start()");
        }

        // If timer is already timing, reload the accumulator and return to extend the delay
        if (TimerTiming)
        {
            Accumulator = Preset / (int)(taskTimer.Interval / 1000); // Convert preset ms into interval ticks
            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Returned. Timer already timing");
            return;
        }

        Initialise();
        Start_Internal();
    }

    // Loads timer parameters before the first tick so state is consistent from the moment Start()
    // returns, even before the microTimer fires its first callback.
    private void Initialise()
    {
        Accumulator = Preset / (int)(taskTimer.Interval / 1000); // Convert preset ms into interval ticks
        TimerTiming = true;
        TimerDone = false;

        if (Debug)
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Initialised");
    }

    // Starts the internal microTimer if it is not already running.
    private void Start_Internal()
    {
        if (!taskTimer.Enabled)
        {
            taskTimer.Start();

            if (Debug)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - taskTimer.Start()");
            }

            // For debug and ToString override
            stopWatch.Restart();
        }
    }

    public void Stop()
    {
        taskTimer.Stop();

        stopWatch.Stop();

        Accumulator = 0;
        TimerTiming = false;
        TimerDone = false;

        if (Debug)
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Stop()");
    }

    // Task timer tick event callback — runs on the high-priority timer thread, not the UI thread.
    private void Tick(object sender, MicroTimerEventArgs timerEventArgs)
    {
        RunTimerTask();
    }

    // Decrements the accumulator each tick and triggers the done event when it reaches zero.
    private void RunTimerTask()
    {
        Accumulator--;

        if (Accumulator <= 0)
        {
            Accumulator = 0;
            TimerTiming = false;

            RaiseEventReq();
        }
    }

    // Stops the internal timer, latches TimerDone, then marshals the callback to the UI thread.
    private void RaiseEventReq()
    {
        // Stop the task timer
        taskTimer.Stop();

        if (Debug)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - taskTimer.Stop()");
        }

        // Stays true until Start() or Stop() is called
        TimerDone = true;

        if (Debug)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer done, event fired");
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}");
        }

        // Guard against the control being destroyed between the timer firing and the invoke being
        // queued.
        if (UiControl?.IsHandleCreated == true && !UiControl.IsDisposed)
        {
            RaiseOnTimingDoneEvent();
        }
        else
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Event fire failed! Control Handle not created");
        }
    }

    protected virtual void RaiseOnTimingDoneEvent()
    {
        UiControl.BeginInvoke((MethodInvoker)delegate
        {
            OnTimingDone?.Invoke();
        });
    }

    public override string ToString()
    {
        return $"Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}";
    }

    #endregion
}
