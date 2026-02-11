using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runtime.Modules.EventDispatcher;

/// <summary>
/// Base class for all events in the SCADA system.
/// </summary>
public class Event
{
    public EventType Type { get; }
    public string Source { get; }
    public DateTime Timestamp { get; }
    public EventPriority Priority { get; }
    public Dictionary<string, object?> Data { get; }

    public Event(EventType type, string source = "", EventPriority priority = EventPriority.Normal)
    {
        Type = type;
        Source = source ?? "";
        Timestamp = DateTime.Now;
        Priority = priority;
        Data = new Dictionary<string, object?>();
    }

    public void SetData(string key, object? value)
    {
        Data[key] = value;
    }

    public void SetData(Dictionary<string, object?> data)
    {
        foreach (var kvp in data)
            Data[kvp.Key] = kvp.Value;
    }

    public T? GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value) && value is T t)
            return t;
        return default;
    }

    public JsonObject ToJson()
    {
        var obj = new JsonObject
        {
            ["type"] = Type.ToString(),
            ["source"] = Source,
            ["timestamp"] = Timestamp.ToString("O"),
            ["priority"] = Priority.ToString(),
            ["data"] = JsonSerializer.SerializeToNode(Data) ?? new JsonObject()
        };
        return obj;
    }

    public static Event? FromJson(JsonObject json)
    {
        if (!json.TryGetPropertyValue("type", out var typeNode) || typeNode == null)
            return null;

        if (!Enum.TryParse<EventType>(typeNode.ToString(), out var type))
            return null;

        var source = json["source"]?.ToString() ?? "";
        var priorityStr = json["priority"]?.ToString() ?? "Normal";
        Enum.TryParse<EventPriority>(priorityStr, out var priority);

        var evt = new Event(type, source, priority);
        
        if (json.TryGetPropertyValue("data", out var dataNode) && dataNode is JsonObject dataObj)
        {
            foreach (var prop in dataObj)
            {
                evt.Data[prop.Key] = prop.Value?.ToString();
            }
        }

        return evt;
    }

    public static string TypeToString(EventType type) => type.ToString();

    public static bool TryParseType(string typeStr, out EventType type)
    {
        return Enum.TryParse<EventType>(typeStr, out type);
    }
}
