using System;
using System.Diagnostics;

namespace TableEditor.Timers;

// Wraps Stopwatch to provide a formatted elapsed-time string, primarily for debug output and
// ToString overrides in the timer classes. Keeps the formatting logic in one place so callers
// do not need to know about TimeSpan arithmetic.
public class MyStopWatch
{
    private readonly Stopwatch stopwatch;

    public MyStopWatch()
    {
        stopwatch = new Stopwatch();
    }

    public MyStopWatch(bool startNow)
    {
        stopwatch = new Stopwatch();

        if (startNow)
            stopwatch.Start();
    }

    // Returns elapsed time as "s.mmmS". Optionally resets or stops the watch after reading so
    // callers can use it as a lap timer without managing state themselves.
    public string Get(bool autoReset = false, bool stop = false)
    {
        if (!stopwatch.IsRunning)
            stopwatch.Start();

        // Use TimeSpan to enable reformatting of Stopwatch with high resolution.
        TimeSpan timeSpan = stopwatch.Elapsed;

        // Format to display seconds.milliseconds
        string formattedElapsed = $"{timeSpan.Seconds}.{timeSpan.Milliseconds:000}s";

        if (autoReset)
            stopwatch.Restart();

        if (stop)
            stopwatch.Stop();

        return formattedElapsed;
    }

    public void Start()
    {
        if (!stopwatch.IsRunning)
            stopwatch.Start();
        else
            stopwatch.Restart();
    }

    public void Restart()
    {
        if (!stopwatch.IsRunning)
            stopwatch.Start();
        else
            stopwatch.Restart();
    }

    public void Stop()
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
            stopwatch.Reset();
        }
    }
}
