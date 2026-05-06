using System;
using System.Diagnostics;

namespace MicroLibrary;

// https://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer
// Written by: https://www.codeproject.com/Members/ken-loveday
// MicroTimer: A microsecond and millisecond timer in C# that is used in a similar way to the
// .NET System.Timers.Timer.

// Extends Stopwatch with microsecond resolution by converting ticks using the hardware frequency.
// Throws on construction if the system does not support a high-resolution counter, because
// MicroTimer's spin-wait loop depends on it for accurate intervals.
public class MicroStopwatch : Stopwatch
{
    private readonly double microSecPerTick = 1000000D / Stopwatch.Frequency;

    public MicroStopwatch()
    {
        if (!Stopwatch.IsHighResolution)
        {
            throw new Exception("On this system the high-resolution " +
                                "performance counter is not available");
        }
    }

    public long ElapsedMicroseconds
    {
        get
        {
            return (long)(ElapsedTicks * microSecPerTick);
        }
    }
}
