namespace VoiceCraft.Server;

/// <summary>
/// Provides console logging functionality with colored output.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Logs a message to the console with timestamp and optional coloring.
    /// </summary>
    /// <param name="logType">The type of log message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="tag">The source tag for the log message.</param>
    public static void LogToConsole(LogType logType, string message, string tag)
    {
        switch (logType)
        {
            case LogType.Info:
                Console.ResetColor();
                Console.WriteLine($"[{DateTime.Now}] [{tag}]: {message}");
                break;

            case LogType.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now}] [Error] [{tag}]: {message}");
                Console.ResetColor();
                break;

            case LogType.Warn:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now}] [Warning] [{tag}]: {message}");
                Console.ResetColor();
                break;

            case LogType.Success:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now}] [{tag}]: {message}");
                Console.ResetColor();
                break;
        }
    }
}

/// <summary>
/// Log message severity types.
/// </summary>
public enum LogType
{
    /// <summary>Informational message.</summary>
    Info,
    /// <summary>Warning message.</summary>
    Warn,
    /// <summary>Error message.</summary>
    Error,
    /// <summary>Success message.</summary>
    Success
}
