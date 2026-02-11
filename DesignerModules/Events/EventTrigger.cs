using Newtonsoft.Json.Linq;

namespace Designer.Modules.Events;

/// <summary>
/// Represents an event trigger that fires an event.
/// </summary>
public class EventTrigger
{
    /// <summary>
    /// Component ID that triggers the event.
    /// </summary>
    public string ComponentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Trigger type (OnClick, OnDoubleClick, etc.).
    /// </summary>
    public string Type { get; set; } = "OnClick";
    
    /// <summary>
    /// Optional condition expression for OnCondition trigger type.
    /// </summary>
    public string? Condition { get; set; }
    
    /// <summary>
    /// Optional timer interval in milliseconds for OnTimer trigger type.
    /// </summary>
    public int? TimerInterval { get; set; }
    
    /// <summary>
    /// Optional tag name for OnTagChange trigger type.
    /// </summary>
    public string? TagName { get; set; }

    /// <summary>
    /// Converts trigger to JSON.
    /// </summary>
    public JObject ToJson()
    {
        var json = new JObject
        {
            ["componentId"] = ComponentId,
            ["type"] = Type
        };
        
        if (!string.IsNullOrEmpty(Condition))
            json["condition"] = Condition;
            
        if (TimerInterval.HasValue)
            json["timerInterval"] = TimerInterval.Value;
            
        if (!string.IsNullOrEmpty(TagName))
            json["tagName"] = TagName;
            
        return json;
    }
    
    /// <summary>
    /// Creates trigger from JSON.
    /// </summary>
    public static EventTrigger FromJson(JObject json)
    {
        var trigger = new EventTrigger
        {
            ComponentId = json["componentId"]?.ToString() ?? string.Empty,
            Type = json["type"]?.ToString() ?? "OnClick"
        };
        
        if (json["condition"] != null)
            trigger.Condition = json["condition"].ToString();
            
        if (json["timerInterval"] != null && int.TryParse(json["timerInterval"].ToString(), out int interval))
            trigger.TimerInterval = interval;
            
        if (json["tagName"] != null)
            trigger.TagName = json["tagName"].ToString();
            
        return trigger;
    }
}
