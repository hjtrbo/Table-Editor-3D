using System;

namespace MicroLibrary;

// https://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer
// Written by: https://www.codeproject.com/Members/ken-loveday

// Carries diagnostic timing information from the notification loop to each elapsed event handler.
// All values are read-only so handlers cannot accidentally mutate shared timer state.
public class MicroTimerEventArgs : EventArgs
{
    // Simple counter — number of times the timed event (callback function) has executed
    public int TimerCount { get; private set; }

    // Time when the timed event was called since the timer started, in microseconds
    public long ElapsedMicroseconds { get; private set; }

    // How late the timer was compared to when it should have been called, in microseconds
    public long TimerLateBy { get; private set; }

    // Time it took to execute the previous call to the callback function, in microseconds
    public long CallbackFunctionExecutionTime { get; private set; }

    public MicroTimerEventArgs(int timerCount,
                               long elapsedMicroseconds,
                               long timerLateBy,
                               long callbackFunctionExecutionTime)
    {
        TimerCount = timerCount;
        ElapsedMicroseconds = elapsedMicroseconds;
        TimerLateBy = timerLateBy;
        CallbackFunctionExecutionTime = callbackFunctionExecutionTime;
    }
}
