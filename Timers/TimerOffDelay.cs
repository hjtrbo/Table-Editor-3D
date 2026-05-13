using System;
using System.Threading;
using System.Windows.Forms;
using ThreadingTimer = System.Threading.Timer;

namespace TableEditor.Timers;

// Off-delay timer: the done event fires after Preset milliseconds have elapsed since the most
// recent Start() call. Each Start() while timing reloads the delay (retriggerable), matching the
// PLC TOF instruction.
//
// Backed by a System.Threading.Timer — the callback runs on a ThreadPool thread, not the UI
// thread. OnTimingDone is marshalled to UiControl via BeginInvoke.
public class TimerOffDelay
{
    #region Properties

    // Timer preset in milliseconds. Must be >= 1.
    public int Preset { get; set; }

    // Snapshot of remaining time in ms. Updated only when the timer (re)starts, not continuously.
    public int Accumulator { get; private set; }

    // True between Start() and the done event (or Stop()).
    public bool TimerTiming { get; private set; }

    // Latches true on the done event. Stays high until the next Start() or Stop().
    public bool TimerDone { get; set; }

    // Debug mode. Writes status to console.
    public bool Debug { get; set; }

    // The name of the class that owns this timer instance.
    public string DebugInstanceName { get; set; }

    // Meaningful name for the debug console output.
    public string DebugTimerName { get; set; }

    // Form control used to marshal the done event back to the UI thread. Stored per-instance so
    // multiple timers can target separate controls.
    public Control UiControl { get; set; }

    #endregion

    #region Variables

    private readonly MyStopWatch stopWatch;
    private readonly ThreadingTimer timer;
    private readonly object syncLock = new object();

    // True while the underlying Timer has been Change()'d to fire.
    private bool isScheduled;

    public delegate void TimingDoneCallback();

    // Assign your callback method to this field to be notified when timing completes.
    public TimingDoneCallback OnTimingDone;

    #endregion

    #region Constructor

    public TimerOffDelay()
    {
        // Start dormant; Start() arms it.
        timer = new ThreadingTimer(OnTick, null, Timeout.Infinite, Timeout.Infinite);

        stopWatch = new MyStopWatch();
    }

    #endregion

    #region Functions

    // Starts (or re-triggers) the off-delay. If the timer is already running, the period is
    // reloaded to extend the delay — this is the retrig behaviour callers rely on for debouncing.
    public void Start()
    {
        if (Preset < 1)
            throw new Exception("Preset must be >= 1 ms");

        lock (syncLock)
        {
            if (Debug && !TimerTiming)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start()");

            if (TimerTiming)
            {
                // Retrig: extend the in-flight period without firing a fresh debug start line.
                timer.Change(Preset, Timeout.Infinite);
                Accumulator = Preset;
                if (Debug)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Reloaded. Timer already timing");
                return;
            }

            Accumulator = Preset;
            TimerTiming = true;
            TimerDone = false;

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Initialised");

            timer.Change(Preset, Timeout.Infinite);
            isScheduled = true;

            stopWatch.Restart();

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - taskTimer.Start()");
        }
    }

    public void Stop()
    {
        lock (syncLock)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            isScheduled = false;

            stopWatch.Stop();

            Accumulator = 0;
            TimerTiming = false;
            TimerDone = false;

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Stop()");
        }
    }

    // ThreadPool callback. Latches TimerDone, then marshals OnTimingDone to the UI thread.
    private void OnTick(object state)
    {
        bool fireEvent;

        lock (syncLock)
        {
            // Drop stray callbacks that may have been queued before Stop() disarmed the timer.
            if (!isScheduled)
                return;

            isScheduled = false;

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - taskTimer.Stop()");

            Accumulator = 0;
            TimerTiming = false;
            TimerDone = true;
            fireEvent = true;
        }

        if (fireEvent)
            RaiseEventReq();
    }

    private void RaiseEventReq()
    {
        if (Debug)
        {
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer done, event fired");
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}");
        }

        // Guard against the control being destroyed between the timer firing and the invoke being
        // queued.
        if (UiControl?.IsHandleCreated == true && !UiControl.IsDisposed)
            RaiseOnTimingDoneEvent();
        else
            Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Event fire failed! Control Handle not created");
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
