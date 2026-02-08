using System.Collections.Concurrent;
using System.Text.Json;
using AccuTrack.Runtime.EventDispatch;

namespace AccuTrack.Runtime.Tags;

/// <summary>
/// Load from JSON, in-memory tag list, get/set value; publishes TagValueChanged.
/// </summary>
public sealed class TagManager
{
    private readonly ConcurrentDictionary<string, RuntimeTag> _tags = new(StringComparer.OrdinalIgnoreCase);
    private readonly EventDispatcher _dispatcher = EventDispatcher.Instance;

    public IReadOnlyCollection<RuntimeTag> GetAllTags() => _tags.Values.ToList();

    public RuntimeTag? GetTag(string name) =>
        _tags.TryGetValue(name, out var tag) ? tag : null;

    public object? GetValue(string name) => GetTag(name)?.Value;

    public void SetValue(string name, object? value)
    {
        var tag = GetTag(name);
        if (tag == null) return;
        tag.Value = value;
        tag.LastUpdated = DateTime.UtcNow;
        _dispatcher.Publish(new Event(EventTypes.TagValueChanged, name, new Dictionary<string, object?>
        {
            ["TagName"] = name,
            ["Value"] = value,
            ["Timestamp"] = tag.LastUpdated
        }));
    }

    public void LoadFromJson(JsonElement? config)
    {
        if (config == null || config.Value.ValueKind != JsonValueKind.Array) return;
        foreach (var item in config.Value.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var n) ? n.GetString() : item.TryGetProperty("Name", out var n2) ? n2.GetString() : null;
            if (string.IsNullOrEmpty(name)) continue;
            var tag = new RuntimeTag
            {
                Name = name,
                Address = item.TryGetProperty("address", out var a) ? a.GetString() ?? "" : item.TryGetProperty("Address", out var a2) ? a2.GetString() ?? "" : "",
                DataType = item.TryGetProperty("dataType", out var dt) ? dt.GetString() ?? "Double" : item.TryGetProperty("DataType", out var dt2) ? dt2.GetString() ?? "Double" : "Double"
            };
            _tags[name] = tag;
        }
    }

    public void Clear() => _tags.Clear();
}
