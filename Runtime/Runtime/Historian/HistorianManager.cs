using System.Collections.Concurrent;
using System.Text.Json;

namespace AccuTrack.Runtime.Historian;

public record HistorianDataPoint(DateTime Timestamp, object? Value, string? Quality);

public sealed class HistorianManager
{
    private readonly ConcurrentDictionary<string, List<HistorianDataPoint>> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _sampledTags = new();
    private int _sampleIntervalMs = 1000;
    private int _maxPointsPerTag = 10000;

    public int SampleIntervalMs => _sampleIntervalMs;
    public IReadOnlyList<string> SampledTags => _sampledTags;

    public void LoadConfig(JsonElement? config)
    {
        _sampledTags.Clear();
        if (config == null) return;
        var root = config.Value;
        _sampleIntervalMs = root.TryGetProperty("sampleIntervalMs", out var i) ? i.GetInt32() : 1000;
        _maxPointsPerTag = root.TryGetProperty("maxPointsPerTag", out var m) ? m.GetInt32() : 10000;
        if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tags.EnumerateArray())
            {
                var name = t.GetString();
                if (!string.IsNullOrEmpty(name)) _sampledTags.Add(name);
            }
        }
    }

    public void Record(string tagName, object value)
    {
        var list = _data.GetOrAdd(tagName, _ => new List<HistorianDataPoint>());
        lock (list)
        {
            list.Add(new HistorianDataPoint(DateTime.UtcNow, value, null));
            while (list.Count > _maxPointsPerTag)
                list.RemoveAt(0);
        }
    }

    public IReadOnlyList<HistorianDataPoint> Query(string tagName, DateTime start, DateTime end)
    {
        if (!_data.TryGetValue(tagName, out var list)) return Array.Empty<HistorianDataPoint>();
        lock (list)
            return list.Where(p => p.Timestamp >= start && p.Timestamp <= end).ToList();
    }

    public async Task<IReadOnlyList<HistorianDataPoint>> QueryAsync(string tagName, DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await Task.Run(() => Query(tagName, start, end), ct).ConfigureAwait(false);
    }
}
