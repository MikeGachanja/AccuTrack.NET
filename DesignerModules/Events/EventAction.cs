using Newtonsoft.Json.Linq;

namespace Designer.Modules.Events;

/// <summary>
/// Represents an action to execute when an event is triggered.
/// </summary>
public class EventAction
{
    /// <summary>
    /// Action type (NavigateScreen, SetBit, WriteTag, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Screen ID for navigation actions.
    /// </summary>
    public string? ScreenId { get; set; }
    
    /// <summary>
    /// Tag name or address for tag operations.
    /// </summary>
    public string? Tag { get; set; }
    
    /// <summary>
    /// Value for tag write operations.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Script path for script actions.
    /// </summary>
    public string? Script { get; set; }
    
    /// <summary>
    /// Script arguments for script actions.
    /// </summary>
    public string? Arguments { get; set; }
    
    /// <summary>
    /// Message for ShowMessage action.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Component ID for component control actions.
    /// </summary>
    public string? ComponentId { get; set; }
    
    /// <summary>
    /// Additional parameters as key-value pairs.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Converts action to JSON.
    /// </summary>
    public JObject ToJson()
    {
        var json = new JObject
        {
            ["type"] = Type
        };
        
        if (!string.IsNullOrEmpty(ScreenId))
            json["screenId"] = ScreenId;
            
        if (!string.IsNullOrEmpty(Tag))
            json["tag"] = Tag;
            
        if (!string.IsNullOrEmpty(Value))
            json["value"] = Value;
            
        if (!string.IsNullOrEmpty(Script))
            json["script"] = Script;
            
        if (!string.IsNullOrEmpty(Arguments))
            json["arguments"] = Arguments;
            
        if (!string.IsNullOrEmpty(Message))
            json["message"] = Message;
            
        if (!string.IsNullOrEmpty(ComponentId))
            json["componentId"] = ComponentId;
            
        if (Parameters.Count > 0)
        {
            var paramsObj = new JObject();
            foreach (var kvp in Parameters)
            {
                paramsObj[kvp.Key] = kvp.Value;
            }
            json["parameters"] = paramsObj;
        }
        
        return json;
    }
    
    /// <summary>
    /// Creates action from JSON.
    /// </summary>
    public static EventAction FromJson(JObject json)
    {
        var action = new EventAction
        {
            Type = json["type"]?.ToString() ?? string.Empty
        };
        
        if (json["screenId"] != null)
            action.ScreenId = json["screenId"].ToString();
            
        if (json["tag"] != null)
            action.Tag = json["tag"].ToString();
            
        if (json["value"] != null)
            action.Value = json["value"].ToString();
            
        if (json["script"] != null)
            action.Script = json["script"].ToString();
            
        if (json["arguments"] != null)
            action.Arguments = json["arguments"].ToString();
            
        if (json["message"] != null)
            action.Message = json["message"].ToString();
            
        if (json["componentId"] != null)
            action.ComponentId = json["componentId"].ToString();
            
        if (json["parameters"] is JObject paramsObj)
        {
            foreach (var prop in paramsObj.Properties())
            {
                action.Parameters[prop.Name] = prop.Value?.ToString() ?? string.Empty;
            }
        }
        
        return action;
    }
}
