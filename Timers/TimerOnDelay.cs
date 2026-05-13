using MicroLibrary;
using System;
using System.Windows.Forms;

namespace Timers;

// On-delay timer: the done event fires once the signal has been continuously true for the
// configured Preset duration. Repeated Start() calls while timing are ignored; calling Stop()
// cancels. AutoRestart and AutoStop modes extend this for periodic / watchdog patterns.
public class TimerOnDelay
{
    #region Properties

    // Timer preset value in milliseconds
    public int Preset { get; set; }

    // Timer accumulator value in ms. Decrements as timer is timing down
    public int Accumulator { get; private set; }

    // True when timer is timing down
    public bool TimerTiming { get; private set; }

    // Stays high until timer is next enabled
    public bool TimerDone { get; private set; }

    // Enables 'endless timing' firing the timer done event each timing period. Call Stop() to end
    public bool AutoRestart { get; set; }

    // If using AutoRestart timing mode and this property is set true, the timer will automatically
    // stop if a call to Start() has not been received within the number of timer done events as
    // configured in the AutoStop_CountsPreset property
    public bool AutoStop { get; set; }

    // If in AutoRestart timing mode and if AutoStop is true and a call to Start() hasn't been
    // received for this many timer done events, the timer will stop
    public int AutoStop_CountsPreset { get; set; }

    // If AutoStop is used, this property is the counts remaining until the timer stops
    public int AutoStop_CountsToGo { get; private set; }

    // Debug mode. Writes status to console
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

    private readonly MyStopWatch stopWatch;
    private readonly MicroTimer microTimer;

    public delegate void TimingDoneCallback();

    // Assign your callback method to this field to be notified when timing completes.
    public TimingDoneCallback OnTimingDone;

    private bool restartLatch = false;

    #endregion

    #region Constructor

    public TimerOnDelay()
    {
        // New MicroTimer and add event handler
        microTimer = new MicroTimer();
        microTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(Tick);

        // Stopwatch for debug mode
        stopWatch = new MyStopWatch();

        // Set tick interval. 2 ms resolution gives consistent ticks without excessive CPU spin.
        microTimer.Interval = 2000;
    }

    #endregion

    #region Functions

    // Starts the timer task. The timer runs to completion each call. Repeated calls whilst the
    // timer is running are ignored. If AutoStop mode is active, each call resets the number of
    // timer period done events counted.
    public void Start()
    {
        // Preset must be greater than the task timer interval
        if (Preset < microTimer.Interval / 1000) // us to ms
            throw new Exception($"Preset must be greater or equal to {microTimer.Interval / 1000}");

        // Cannot have AutoRestart and AutoStop on at the same time
        if (AutoStop && AutoRestart)
            throw new Exception($"Cannot have AutoStop and AutoRestart set true at the same time");

        // If no off counts are set by the user, raise this error
        if (AutoStop && AutoStop_CountsPreset <= 0)
            throw new Exception($"AutoStopOffCounts must be > 0");

        // If AutoStop mode is enabled, load / reload the number of autostop counts to go then return.
        if (AutoStop)
        {
            if (!restartLatch)
            {
                if (Debug && (AutoStop_CountsToGo != AutoStop_CountsPreset))
                {
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start() AutoStop_OffCountsToGo Loaded");
                }

                AutoStop_CountsToGo = AutoStop_CountsPreset;
            }
        }

        // Return if we are already timing. This bit will be false if we have arrived here from a call to Restart()
        if (TimerTiming)
        {
            if (Debug)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start() call ignored. Timer already running");
            }

            return;
        }

        // Anticipated setting of the timer timing flag. Bridges the delay between the first start
        // call and the first microTimer tick.
        TimerTiming = true;

        // Timer started debug
        if (Debug && !restartLatch)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer started");
        }

        // Clear the restart flag
        restartLatch = false;

        // For debug and ToString override
        stopWatch.Start();

        // Preset loads into accumulator: ms / ticks
        Accumulator = Preset / (int)(microTimer.Interval / 1000);

        // Start the task timer — must be the last code line in the start function
        if (!microTimer.Enabled)
            microTimer.Start();
    }

    // Internal restart path: keeps the microTimer thread alive across consecutive timer periods
    // instead of tearing it down and spinning it back up, which would introduce latency jitter.
    private void Restart()
    {
        // Restart flag to keep taskTimer running and allows for selective conditions in the start
        // function that are required to auto restart the timer
        restartLatch = true;

        Start();

        // Reset the restart latch
        restartLatch = false;

        if (Debug)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer restarted");
        }
    }

    public void Stop()
    {
        // If not set to auto restart, nuke the microtimer thread
        if (!restartLatch && microTimer.Enabled)
        {
            if (Debug)
            {
                if (!AutoStop)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer stopped");
                else
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer auto stopped");
            }

            // For debug and ToString override
            stopWatch.Stop();

            // Resets
            TimerDone = false;
            TimerTiming = false;
            Accumulator = 0;
            AutoStop_CountsToGo = 0;

            microTimer.Stop();
        }
    }

    // Task timer tick event callback — runs on the high-priority timer thread, not the UI thread.
    private void Tick(object sender, MicroTimerEventArgs timerEventArgs)
    {
        RunTimerTask();
    }

    private void RunTimerTask()
    {
        Accumulator--;
        TimerTiming = true;

        if (Accumulator <= 0)
        {
            Accumulator = 0;
            TimerTiming = false;
            TimerDone = true;

            RaiseEventReq();
        }
    }

    private void RaiseEventReq()
    {
        if (Debug)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer done, event fired");
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}");
        }

        // Marshal the event back to the UI thread. Guard against the control being destroyed
        // between the timer firing and the invoke being queued.
        if (UiControl?.IsHandleCreated == true && !UiControl.IsDisposed)
        {
            RaiseOnTimingDoneEvent();
        }
        else
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Event fire failed! Control Handle not created");
        }

        // If AutoStop mode is enabled, decrement the remaining count. Stop when exhausted.
        if (AutoStop)
        {
            AutoStop_CountsToGo--;

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - AutoStopOffCountsToGo = {AutoStop_CountsToGo}");

            if (AutoStop_CountsToGo == 0)
            {
                Stop();
            }
            else
                Restart();
        }

        // If AutoRestart is enabled, start the timer again
        if (AutoRestart)
        {
            Restart();
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
