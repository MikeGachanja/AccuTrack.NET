using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Events;

/// <summary>
/// Represents a SCADA event with trigger and actions.
/// </summary>
public class ScadaEvent
{
    /// <summary>
    /// Unique event ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Event name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Event description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Event trigger.
    /// </summary>
    public EventTrigger Trigger { get; set; } = new EventTrigger();
    
    /// <summary>
    /// List of actions to execute when event is triggered.
    /// </summary>
    public List<EventAction> Actions { get; set; } = new List<EventAction>();
    
    /// <summary>
    /// Event metadata.
    /// </summary>
    public EventMetadata Metadata { get; set; } = new EventMetadata();

    /// <summary>
    /// Converts event to JSON.
    /// </summary>
    public JObject ToJson()
    {
        var actionsArray = new JArray();
        foreach (var action in Actions)
        {
            actionsArray.Add(action.ToJson());
        }
        
        var json = new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["description"] = Description,
            ["trigger"] = Trigger.ToJson(),
            ["actions"] = actionsArray
        };
        
        if (Metadata != null)
        {
            json["metadata"] = Metadata.ToJson();
        }
        
        return json;
    }
    
    /// <summary>
    /// Creates event from JSON.
    /// </summary>
    public static ScadaEvent FromJson(JObject json)
    {
        var evt = new ScadaEvent
        {
            Id = json["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name = json["name"]?.ToString() ?? string.Empty,
            Description = json["description"]?.ToString() ?? string.Empty
        };
        
        if (json["trigger"] is JObject triggerObj)
        {
            evt.Trigger = EventTrigger.FromJson(triggerObj);
        }
        
        if (json["actions"] is JArray actionsArray)
        {
            foreach (var item in actionsArray)
            {
                if (item is JObject actionObj)
                {
                    evt.Actions.Add(EventAction.FromJson(actionObj));
                }
            }
        }
        
        if (json["metadata"] is JObject metadataObj)
        {
            evt.Metadata = EventMetadata.FromJson(metadataObj);
        }
        
        return evt;
    }
}

/// <summary>
/// Event metadata.
/// </summary>
public class EventMetadata
{
    /// <summary>
    /// Event priority (0 = low, 1 = normal, 2 = high, 3 = critical).
    /// </summary>
    public int Priority { get; set; } = 1;
    
    /// <summary>
    /// User who created the event.
    /// </summary>
    public string CreatedBy { get; set; } = "User";
    
    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public string CreatedAt { get; set; } = DateTime.Now.ToString("O");
    
    /// <summary>
    /// User who last modified the event.
    /// </summary>
    public string? ModifiedBy { get; set; }
    
    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public string? ModifiedAt { get; set; }

    /// <summary>
    /// Converts metadata to JSON.
    /// </summary>
    public JObject ToJson()
    {
        var json = new JObject
        {
            ["priority"] = Priority,
            ["createdBy"] = CreatedBy,
            ["createdAt"] = CreatedAt
        };
        
        if (!string.IsNullOrEmpty(ModifiedBy))
            json["modifiedBy"] = ModifiedBy;
            
        if (!string.IsNullOrEmpty(ModifiedAt))
            json["modifiedAt"] = ModifiedAt;
            
        return json;
    }
    
    /// <summary>
    /// Creates metadata from JSON.
    /// </summary>
    public static EventMetadata FromJson(JObject json)
    {
        return new EventMetadata
        {
            Priority = json["priority"]?.ToObject<int>() ?? 1,
            CreatedBy = json["createdBy"]?.ToString() ?? "User",
            CreatedAt = json["createdAt"]?.ToString() ?? DateTime.Now.ToString("O"),
            ModifiedBy = json["modifiedBy"]?.ToString(),
            ModifiedAt = json["modifiedAt"]?.ToString()
        };
    }
}
