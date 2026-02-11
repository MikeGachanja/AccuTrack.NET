using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.Historian;

/// <summary>Historian manager stub: database path, optional tag subscription, query API.</summary>
public sealed class HistorianManager
{
    private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, object? Value, int Quality)>> _tagHistory = new();
    private string _databasePath = "";
    private volatile bool _running;
    private TagManager? _tagManager;

    public string DatabasePath => _databasePath;
    public bool IsRunning => _running;

    public void SetTagManager(TagManager? tagManager)
    {
        _tagManager = tagManager;
    }

    public bool Initialize(object? config)
    {
        if (config is JsonObject obj)
        {
            _databasePath = obj["databasePath"]?.GetValue<string>() ?? "";
        }
        return true;
    }

    public void Start() => _running = true;
    public void Stop() => _running = false;

    /// <summary>Query tag values (stub: returns in-memory history if any).</summary>
    public IReadOnlyList<(DateTime Timestamp, object? Value, int Quality)> QueryTagValues(string tagName, long startTimeMs, long endTimeMs, int maxRows = 0)
    {
        if (!_tagHistory.TryGetValue(tagName, out var list)) return Array.Empty<(DateTime, object?, int)>();
        var start = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).UtcDateTime;
        var end = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs).UtcDateTime;
        var q = list.Where(x => x.Timestamp >= start && x.Timestamp <= end);
        if (maxRows > 0) q = q.Take(maxRows);
        return q.ToList();
    }

    /// <summary>Record a tag value (called when subscribed to TagManager).</summary>
    public void RecordTagValue(string tagName, object? value, int quality)
    {
        if (!_running) return;
        var list = _tagHistory.GetOrAdd(tagName, _ => new List<(DateTime, object?, int)>());
        lock (list)
        {
            list.Add((DateTime.UtcNow, value, quality));
            while (list.Count > 10000) list.RemoveAt(0);
        }
    }
}
