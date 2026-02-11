using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;

namespace Runtime.Modules.Console;

/// <summary>
/// Console module implementation for runtime logging.
/// </summary>
public sealed class ConsoleModule : ModuleBase, IConsole
{
    private const int DefaultMaxEntries = 1000;
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private int _maxLogEntries = DefaultMaxEntries;

    public override string ModuleName => "Console";
    public override string DisplayName => "Console Module";

    public event EventHandler<LogEntry>? NewLogEntry;
    public event EventHandler? LogsCleared;

    public override bool Initialize(JsonObject? config = null)
    {
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        SetRunning(false);
    }

    public void LogInfo(string message, string source = "System")
    {
        AddLogEntry(message, "Info", source);
    }

    public void LogWarning(string message, string source = "System")
    {
        AddLogEntry(message, "Warning", source);
    }

    public void LogError(string message, string source = "System")
    {
        AddLogEntry(message, "Error", source);
    }

    public void LogDebug(string message, string source = "System")
    {
        AddLogEntry(message, "Debug", source);
    }

    public IReadOnlyList<LogEntry> GetLogEntries()
    {
        return _logEntries.ToArray();
    }

    public void ClearLogs()
    {
        while (_logEntries.TryDequeue(out _)) { }
        LogsCleared?.Invoke(this, EventArgs.Empty);
    }

    public void SetMaxLogEntries(int count)
    {
        _maxLogEntries = Math.Max(1, count);
        TrimLogs();
    }

    public int GetMaxLogEntries() => _maxLogEntries;

    private void AddLogEntry(string message, string level, string source)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message ?? "",
            Level = level ?? "",
            Source = source ?? ""
        };

        _logEntries.Enqueue(entry);
        TrimLogs();
        NewLogEntry?.Invoke(this, entry);
    }

    private void TrimLogs()
    {
        while (_logEntries.Count > _maxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }
    }
}
