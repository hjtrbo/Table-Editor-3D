using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace TableEditor.Common;

// Lightweight debug logger that writes to the VS Output window and, optionally, to a
// rolling log.txt file next to the executable.
//
// Usage:
//   Logger.Log(this, "something interesting happened");
//   // Output: (MyClass).MyMethod something interesting happened
//
// File logging is off by default.  Set IsFileLoggingEnabled = true before the first
// call if you need a persistent trace.  The write is guarded by a directory-existence
// check so a missing output folder degrades gracefully to Trace output rather than
// throwing an unhandled IOException.
public static class Logger
{
    private static readonly string logFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

    public static bool IsFileLoggingEnabled { get; set; } = false;

    static Logger()
    {
        // Create the log file on first use when file logging is turned on, but only if
        // the target directory actually exists (network drives, sandboxed deployments, etc.).
        if (IsFileLoggingEnabled)
        {
            string directory = Path.GetDirectoryName(logFilePath);
            if (Directory.Exists(directory) && !File.Exists(logFilePath))
            {
                using (var stream = File.Create(logFilePath)) { }
            }
        }
    }

    [Conditional("DEBUG")]
    public static void Log(
        object instance,
        string message = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string filePath   = "",
        [CallerLineNumber] int    lineNumber  = 0)
    {
        string instanceName = instance.GetType().Name;
        string logMessage   = $"({instanceName}).{memberName} {message}";

        Debug.WriteLine(logMessage);

        if (!IsFileLoggingEnabled)
            return;

        string directory = Path.GetDirectoryName(logFilePath);

        // If the log directory has disappeared since startup (e.g. temp folder purged),
        // fall back to Trace so the message is not silently swallowed.
        if (!Directory.Exists(directory))
        {
            Trace.WriteLine($"[Logger] Directory missing, falling back to trace: {logMessage}");
            return;
        }

        try
        {
            using (StreamWriter writer = File.AppendText(logFilePath))
            {
                writer.WriteLine(logMessage);
            }
        }
        catch (IOException ex)
        {
            // Last-resort: surface the write failure via Trace rather than crashing the UI.
            Trace.WriteLine($"[Logger] File write failed ({ExceptionHelper.FormatStackTrace(ex)}): {logMessage}");
        }
    }
}
