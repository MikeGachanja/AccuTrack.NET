using System;
using System.Text.Json.Nodes;

namespace Runtime.Modules.Alarms;

/// <summary>Alarm state.</summary>
public enum AlarmState
{
    Normal,
    Active,
    Acknowledged,
    Cleared
}

/// <summary>Alarm type.</summary>
public enum AlarmType
{
    Digital,
    Analog,
    Derived
}

/// <summary>Alarm priority.</summary>
public enum AlarmPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Represents an alarm definition and state.</summary>
public sealed class Alarm
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public AlarmType Type { get; set; }
    public AlarmPriority Priority { get; set; }
    public AlarmState State { get; set; }
    public string TagName { get; set; } = "";
    public object? Threshold { get; set; }
    public string Condition { get; set; } = "";  // ">", "<", "==", "!=", etc.
    public double Deadband { get; set; }
    public double Hysteresis { get; set; }
    public DateTime ActivationTime { get; set; }
    public DateTime AcknowledgmentTime { get; set; }
    public bool Enabled { get; set; } = true;
    internal object? LastValue { get; set; }

    public event EventHandler<(AlarmState NewState, AlarmState OldState)>? StateChanged;

    public void Activate(DateTime? time = null)
    {
        var t = time ?? DateTime.Now;
        var old = State;
        State = AlarmState.Active;
        ActivationTime = t;
        StateChanged?.Invoke(this, (State, old));
    }

    public void Acknowledge(DateTime? time = null)
    {
        var t = time ?? DateTime.Now;
        var old = State;
        State = AlarmState.Acknowledged;
        AcknowledgmentTime = t;
        StateChanged?.Invoke(this, (State, old));
    }

    public void Clear(DateTime? time = null)
    {
        var old = State;
        State = AlarmState.Cleared;
        StateChanged?.Invoke(this, (State, old));
    }

    public JsonObject ToJson()
    {
        var obj = new JsonObject
        {
            ["name"] = Name,
            ["description"] = Description,
            ["type"] = Type.ToString(),
            ["priority"] = Priority.ToString(),
            ["tagName"] = TagName,
            ["condition"] = Condition,
            ["deadband"] = Deadband,
            ["hysteresis"] = Hysteresis,
            ["enabled"] = Enabled
        };
        if (Threshold != null) obj["threshold"] = JsonValue.Create(Threshold);
        return obj;
    }

    public static Alarm? FromJson(JsonObject json)
    {
        var alarm = new Alarm
        {
            Name = json["name"]?.GetValue<string>() ?? "",
            Description = json["description"]?.GetValue<string>() ?? json["message"]?.GetValue<string>() ?? "",
            TagName = json["tagName"]?.GetValue<string>() ?? "",
            Condition = NormalizeCondition(json["condition"]?.GetValue<string>() ?? ""),
            Deadband = json["deadband"]?.GetValue<double>() ?? 0,
            Hysteresis = json["hysteresis"]?.GetValue<double>() ?? 0,
            Enabled = json["enabled"]?.GetValue<bool>() ?? true
        };
        
        // Map alarm type from JSON format (type: "HMI"/"Controller", source: "Digital"/"Analog")
        // to AlarmType enum (Digital/Analog)
        var typeStr = json["type"]?.GetValue<string>() ?? "";
        var sourceStr = json["source"]?.GetValue<string>() ?? "";
        
        if (!string.IsNullOrEmpty(sourceStr))
        {
            // Use source field to determine alarm type
            if (sourceStr.Equals("Digital", StringComparison.OrdinalIgnoreCase))
                alarm.Type = AlarmType.Digital;
            else if (sourceStr.Equals("Analog", StringComparison.OrdinalIgnoreCase))
                alarm.Type = AlarmType.Analog;
            else
                alarm.Type = AlarmType.Digital; // Default
        }
        else if (!string.IsNullOrEmpty(typeStr))
        {
            // Fallback: try to parse type directly
            if (Enum.TryParse<AlarmType>(typeStr, true, out var at))
                alarm.Type = at;
            else
                alarm.Type = AlarmType.Digital; // Default
        }
        else
        {
            alarm.Type = AlarmType.Digital; // Default
        }
        
        // Parse priority
        var priorityStr = json["priority"]?.GetValue<string>() ?? "Medium";
        if (Enum.TryParse<AlarmPriority>(priorityStr, true, out var ap))
            alarm.Priority = ap;
        else
            alarm.Priority = AlarmPriority.Medium; // Default
        
        // Parse threshold
        try
        {
            if (json["threshold"] != null)
                alarm.Threshold = json["threshold"]!.GetValue<double>();
        }
        catch
        {
            if (json["threshold"] != null)
                alarm.Threshold = json["threshold"]!.ToString();
        }
        
        return alarm;
    }
    
    /// <summary>
    /// Normalizes condition strings from designer format to runtime format.
    /// </summary>
    private static string NormalizeCondition(string condition)
    {
        return condition switch
        {
            "EqualTo" or "Equal" => "==",
            "NotEqualTo" or "NotEqual" => "!=",
            "GreaterThan" or "Greater" => ">",
            "LessThan" or "Less" => "<",
            "GreaterThanOrEqual" or "GreaterOrEqual" => ">=",
            "LessThanOrEqual" or "LessOrEqual" => "<=",
            _ => condition // Return as-is if already normalized or unknown
        };
    }
}
