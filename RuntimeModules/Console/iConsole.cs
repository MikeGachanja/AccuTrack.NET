namespace Runtime.Modules.Console;

/// <summary>
/// Console module interface for runtime logging.
/// </summary>
public interface IConsole
{
    /// <summary>Log an info message.</summary>
    void LogInfo(string message, string source = "System");

    /// <summary>Log a warning message.</summary>
    void LogWarning(string message, string source = "System");

    /// <summary>Log an error message.</summary>
    void LogError(string message, string source = "System");

    /// <summary>Log a debug message.</summary>
    void LogDebug(string message, string source = "System");

    /// <summary>Get all log entries.</summary>
    IReadOnlyList<LogEntry> GetLogEntries();

    /// <summary>Clear all log entries.</summary>
    void ClearLogs();

    /// <summary>Set maximum number of log entries to keep.</summary>
    void SetMaxLogEntries(int count);

    /// <summary>Get maximum number of log entries.</summary>
    int GetMaxLogEntries();

    /// <summary>Raised when a new log entry is added.</summary>
    event EventHandler<LogEntry>? NewLogEntry;

    /// <summary>Raised when logs are cleared.</summary>
    event EventHandler? LogsCleared;
}

/// <summary>
/// Represents a single log entry.
/// </summary>
public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Message { get; init; } = "";
    public string Level { get; init; } = "";
    public string Source { get; init; } = "";
}
