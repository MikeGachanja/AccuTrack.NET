using System.Collections.Concurrent;
using System.Text.Json;
using AccuTrack.Runtime.Tags;

namespace AccuTrack.Runtime.Alarms;

public sealed class AlarmManager
{
    private readonly ConcurrentDictionary<string, AlarmCondition> _conditions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<AlarmEntry> _activeAlarms = new();
    private TagManager? _tagManager;

    public void SetTagManager(TagManager? manager) => _tagManager = manager;

    public void LoadFromConfig(JsonElement? config)
    {
        _conditions.Clear();
        if (config == null || config.Value.ValueKind != JsonValueKind.Array) return;
        foreach (var item in config.Value.EnumerateArray())
        {
            var tag = item.TryGetProperty("tag", out var t) ? t.GetString() : item.TryGetProperty("Tag", out var t2) ? t2.GetString() : null;
            if (string.IsNullOrEmpty(tag)) continue;
            var condition = item.TryGetProperty("condition", out var c) ? c.GetString() ?? "Above" : "Above";
            var threshold = item.TryGetProperty("threshold", out var th) ? th.GetDouble() : 0;
            var message = item.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            var cond = new AlarmCondition(tag, condition, threshold, message);
            _conditions[tag] = cond;
        }
    }

    public void EvaluateConditions(string tagName)
    {
        if (_tagManager == null || !_conditions.TryGetValue(tagName, out var cond)) return;
        var value = _tagManager.GetValue(tagName);
        var num = value is double d ? d : value is int i ? i : value is float f ? f : 0.0;
        var triggered = cond.Condition switch
        {
            "Above" => num > cond.Threshold,
            "Below" => num < cond.Threshold,
            "Equal" => Math.Abs(num - cond.Threshold) < 1e-9,
            _ => false
        };
        if (triggered)
            _activeAlarms.Enqueue(new AlarmEntry(DateTime.UtcNow, tagName, cond.Message, true));
    }

    public IReadOnlyList<AlarmEntry> GetActiveAlarms() => _activeAlarms.ToArray();
    public void ClearAlarm(string tagName) { /* remove from active */ }
}

public record AlarmCondition(string TagName, string Condition, double Threshold, string Message);
public record AlarmEntry(DateTime Timestamp, string TagName, string Message, bool Active);
