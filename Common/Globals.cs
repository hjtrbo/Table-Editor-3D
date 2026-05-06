using Timers;

namespace TableEditor;

// Application-wide singletons that don't belong to any particular subsystem.
// Keep this class small — add a new home for things that grow into their own concern.
public static class Globals
{
    // A shared stopwatch available anywhere for ad-hoc perf timing during development.
    // Not used in production paths; wrap calls with #if DEBUG if you want zero overhead
    // in release builds.
    public static MyStopWatch StopWatch = new MyStopWatch();
}
