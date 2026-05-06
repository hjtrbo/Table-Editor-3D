using System;
using System.Threading;
using System.Windows.Forms;

namespace MicroLibrary;

// https://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer
// Written by: https://www.codeproject.com/Members/ken-loveday
// MicroTimer: A microsecond and millisecond timer in C# that is used in a similar way to the
// .NET System.Timers.Timer.

// High-resolution timer that fires MicroTimerElapsed on a dedicated highest-priority thread.
// The notification loop uses SpinWait to avoid OS scheduler latency, giving microsecond-scale
// accuracy at the cost of one full CPU core while running — only use for short bursts.
//
// The uiControl instance field (previously static) lets each MicroTimer instance target a
// different UI control for BeginInvoke marshalling, avoiding cross-instance aliasing bugs.
public class MicroTimer
{
    public delegate void MicroTimerElapsedEventHandler(
                         object sender,
                         MicroTimerEventArgs timerEventArgs);

    public event MicroTimerElapsedEventHandler MicroTimerElapsed;

    private Thread threadTimer = null;
    private long ignoreEventIfLateBy = long.MaxValue;
    private long timerIntervalInMicroSec = 0;
    private bool stopTimer = true;

    // The control used to marshal callbacks back to the UI thread. Kept as an instance field so
    // separate MicroTimer instances can target separate controls without interfering with each other.
    private readonly Control uiControl;

    // Overload for callers that handle their own thread marshalling and do not need BeginInvoke.
    public MicroTimer()
    {
        uiControl = null;
    }

    // Pass a live UI control so that any BeginInvoke calls inside the elapsed handler are
    // dispatched to the correct message loop.
    public MicroTimer(Control uiControl)
    {
        this.uiControl = uiControl;
    }

    public MicroTimer(long timerIntervalInMicroseconds)
    {
        Interval = timerIntervalInMicroseconds;
    }

    public long Interval
    {
        get
        {
            return Interlocked.Read(ref timerIntervalInMicroSec);
        }
        set
        {
            Interlocked.Exchange(ref timerIntervalInMicroSec, value);
        }
    }

    public long IgnoreEventIfLateBy
    {
        get
        {
            return Interlocked.Read(ref ignoreEventIfLateBy);
        }
        set
        {
            Interlocked.Exchange(ref ignoreEventIfLateBy, value <= 0 ? long.MaxValue : value);
        }
    }

    public bool Enabled
    {
        set
        {
            if (value)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
        get
        {
            return (threadTimer != null && threadTimer.IsAlive);
        }
    }

    public void Start()
    {
        if (Enabled || Interval <= 0)
        {
            return;
        }

        stopTimer = false;

        ThreadStart threadStart = delegate ()
        {
            NotificationTimer(ref timerIntervalInMicroSec,
                              ref ignoreEventIfLateBy,
                              ref stopTimer);
        };

        threadTimer = new Thread(threadStart);
        threadTimer.Priority = ThreadPriority.Highest;
        threadTimer.Start();
    }

    public void Stop()
    {
        stopTimer = true;
    }

    public void StopAndWait()
    {
        StopAndWait(Timeout.Infinite);
    }

    public bool StopAndWait(int timeoutInMilliSec)
    {
        stopTimer = true;

        if (!Enabled || threadTimer.ManagedThreadId ==
            Thread.CurrentThread.ManagedThreadId)
        {
            return true;
        }

        return threadTimer.Join(timeoutInMilliSec);
    }

    public void Abort()
    {
        stopTimer = true;

        if (Enabled)
        {
            threadTimer.Abort();
        }
    }

    // Core notification loop. Runs entirely on the timer thread. SpinWait is intentional:
    // Thread.Sleep granularity (~15 ms) is far too coarse for microsecond intervals.
    private void NotificationTimer(ref long timerIntervalInMicroSecRef,
                                   ref long ignoreEventIfLateByRef,
                                   ref bool stopTimerRef)
    {
        int timerCount = 0;
        long nextNotification = 0;

        MicroStopwatch microStopwatch = new MicroStopwatch();
        microStopwatch.Start();

        while (!stopTimerRef)
        {
            long callbackFunctionExecutionTime =
                microStopwatch.ElapsedMicroseconds - nextNotification;

            long timerIntervalInMicroSecCurrent =
                Interlocked.Read(ref timerIntervalInMicroSecRef);
            long ignoreEventIfLateByCurrent =
                Interlocked.Read(ref ignoreEventIfLateByRef);

            nextNotification += timerIntervalInMicroSecCurrent;
            timerCount++;
            long elapsedMicroseconds = 0;

            while ((elapsedMicroseconds = microStopwatch.ElapsedMicroseconds)
                    < nextNotification)
            {
                Thread.SpinWait(10);
            }

            long timerLateBy = elapsedMicroseconds - nextNotification;

            if (timerLateBy >= ignoreEventIfLateByCurrent)
            {
                continue;
            }

            MicroTimerEventArgs microTimerEventArgs =
                 new MicroTimerEventArgs(timerCount,
                                         elapsedMicroseconds,
                                         timerLateBy,
                                         callbackFunctionExecutionTime);

            // Guard the BeginInvoke path: if no uiControl was provided the event handler is
            // responsible for its own thread marshalling.
            if (uiControl?.IsHandleCreated == true && !uiControl.IsDisposed)
            {
                uiControl.BeginInvoke((MethodInvoker)delegate
                {
                    MicroTimerElapsed?.Invoke(this, microTimerEventArgs);
                });
            }
            else
            {
                MicroTimerElapsed?.Invoke(this, microTimerEventArgs);
            }
        }

        microStopwatch.Stop();
    }
}
