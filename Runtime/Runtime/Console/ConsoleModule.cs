using System.Collections.Concurrent;
using System.Linq;

namespace AccuTrack.Runtime.Console;

/// <summary>
/// In-memory log list; LogError, LogInfo, etc.; expose to Logs screen. No IModuleInterface.
/// </summary>
public sealed class ConsoleModule
{
    public enum LogLevel { Debug, Info, Warning, Error }

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries = 1000;

    public void LogDebug(string message, string? source = null) => Add(LogLevel.Debug, message, source);
    public void LogInfo(string message, string? source = null) => Add(LogLevel.Info, message, source);
    public void LogWarning(string message, string? source = null) => Add(LogLevel.Warning, message, source);
    public void LogError(string message, string? source = null) => Add(LogLevel.Error, message, source);

    private void Add(LogLevel level, string message, string? source)
    {
        var entry = new LogEntry(DateTime.UtcNow, level, message, source ?? "");
        _entries.Enqueue(entry);
        while (_entries.Count > _maxEntries && _entries.TryDequeue(out _)) { }
        LogAdded?.Invoke(this, entry);
    }

    public IReadOnlyList<LogEntry> GetRecent(int count = 100)
    {
        return _entries.TakeLast(count).ToList();
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }

    public event EventHandler<LogEntry>? LogAdded;
}

public record LogEntry(DateTime Timestamp, ConsoleModule.LogLevel Level, string Message, string Source);
