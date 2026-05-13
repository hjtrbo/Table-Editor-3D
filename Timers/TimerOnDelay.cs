using System;
using System.Threading;
using System.Windows.Forms;
using ThreadingTimer = System.Threading.Timer;

namespace TableEditor.Timers;

// On-delay timer: the done event fires once Preset milliseconds have elapsed since Start() was
// called. Repeated Start() calls while timing are ignored; calling Stop() cancels. AutoRestart
// keeps firing on every Preset interval; AutoStop is AutoRestart with a watchdog count that
// auto-stops if Start() is not refreshed within AutoStop_CountsPreset done events.
//
// Backed by a System.Threading.Timer so the callback runs on a ThreadPool thread, not the UI
// thread. OnTimingDone is marshalled to UiControl via BeginInvoke.
public class TimerOnDelay
{
    #region Properties

    // Timer preset in milliseconds. Must be >= 1.
    public int Preset { get; set; }

    // Remaining time in milliseconds. Snapshot only — refreshed inside the callback when the
    // timer fires, not continuously.
    public int Accumulator { get; private set; }

    // True between Start() and the next done event (or Stop()).
    public bool TimerTiming { get; private set; }

    // Latches true on each done event. Stays high until the next Start() / Stop().
    public bool TimerDone { get; private set; }

    // Endless firing — OnTimingDone is raised every Preset ms until Stop() is called.
    public bool AutoRestart { get; set; }

    // Watchdog mode: like AutoRestart, but the timer auto-stops after AutoStop_CountsPreset done
    // events unless Start() is called again to reload the count. Cannot be combined with
    // AutoRestart.
    public bool AutoStop { get; set; }

    // Number of consecutive done events allowed before AutoStop triggers a Stop().
    public int AutoStop_CountsPreset { get; set; }

    // Remaining done events before AutoStop fires. Reloaded to AutoStop_CountsPreset on each
    // external Start() call.
    public int AutoStop_CountsToGo { get; private set; }

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

    // True while the underlying Timer has been Change()'d to fire — distinguishes a live timer
    // from an idle one without polling the framework.
    private bool isScheduled;

    public delegate void TimingDoneCallback();

    // Assign your callback method to this field to be notified when timing completes.
    public TimingDoneCallback OnTimingDone;

    #endregion

    #region Constructor

    public TimerOnDelay()
    {
        // Start the timer dormant. Start() arms it via Change() once Preset is known.
        timer = new ThreadingTimer(OnTick, null, Timeout.Infinite, Timeout.Infinite);

        stopWatch = new MyStopWatch();
    }

    #endregion

    #region Functions

    // Starts the timer. Repeated calls while running are ignored (the existing period continues).
    // In AutoStop mode each external call reloads AutoStop_CountsToGo, which is how the caller
    // pets the watchdog.
    public void Start()
    {
        if (Preset < 1)
            throw new Exception("Preset must be >= 1 ms");

        if (AutoStop && AutoRestart)
            throw new Exception("Cannot have AutoStop and AutoRestart set true at the same time");

        if (AutoStop && AutoStop_CountsPreset <= 0)
            throw new Exception("AutoStop_CountsPreset must be > 0");

        lock (syncLock)
        {
            // AutoStop: reload the watchdog count on every external Start() call.
            if (AutoStop)
            {
                if (Debug && AutoStop_CountsToGo != AutoStop_CountsPreset)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start() AutoStop_CountsToGo loaded");

                AutoStop_CountsToGo = AutoStop_CountsPreset;
            }

            // Already running — leave the in-flight period alone.
            if (TimerTiming)
            {
                if (Debug)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start() call ignored. Timer already running");
                return;
            }

            TimerTiming = true;
            TimerDone = false;
            Accumulator = Preset;

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer started");

            stopWatch.Start();

            // AutoRestart / AutoStop want repeated fires; single-shot wants Infinite period.
            int period = (AutoRestart || AutoStop) ? Preset : Timeout.Infinite;
            timer.Change(Preset, period);
            isScheduled = true;
        }
    }

    public void Stop()
    {
        lock (syncLock)
        {
            if (!isScheduled)
                return;

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            isScheduled = false;

            if (Debug)
            {
                if (!AutoStop)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer stopped");
                else
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer auto stopped");
            }

            stopWatch.Stop();

            TimerDone = false;
            TimerTiming = false;
            Accumulator = 0;
            AutoStop_CountsToGo = 0;
        }
    }

    // ThreadPool callback. Updates state, decrements the AutoStop watchdog, marshals OnTimingDone
    // to the UI thread.
    private void OnTick(object state)
    {
        bool fireEvent;

        lock (syncLock)
        {
            // Ignore stray callbacks that may have been queued before Stop() disarmed the timer.
            if (!isScheduled)
                return;

            Accumulator = 0;
            TimerTiming = false;
            TimerDone = true;
            fireEvent = true;

            // AutoStop: count down, stop when exhausted.
            if (AutoStop)
            {
                AutoStop_CountsToGo--;

                if (Debug)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - AutoStop_CountsToGo = {AutoStop_CountsToGo}");

                if (AutoStop_CountsToGo <= 0)
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    isScheduled = false;
                    AutoStop_CountsToGo = 0;
                }
            }

            // In repeating modes the next period is already scheduled by the timer itself —
            // reset the visible state so observers see a fresh accumulator.
            if (isScheduled && (AutoRestart || AutoStop))
            {
                TimerTiming = true;
                TimerDone = false;
                Accumulator = Preset;
            }
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
