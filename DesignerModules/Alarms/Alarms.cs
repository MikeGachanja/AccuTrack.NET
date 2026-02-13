using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Alarms;

/// <summary>
/// Represents alarms configuration for a SCADA project.
/// </summary>
public class Alarms
{
    public List<AlarmDefinition> AlarmDefinitions { get; set; } = new List<AlarmDefinition>();

    /// <summary>
    /// Converts alarms to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var obj = new JObject();
        var alarmsArray = new JArray();

        foreach (var alarm in AlarmDefinitions)
        {
            alarmsArray.Add(alarm.ToJson());
        }

        obj["alarms"] = alarmsArray;
        return obj;
    }

    /// <summary>
    /// Creates alarms from JSON object.
    /// </summary>
    public static Alarms FromJson(JObject json)
    {
        var alarms = new Alarms();
        var alarmsArray = json["alarms"] as JArray;

        if (alarmsArray != null)
        {
            foreach (var item in alarmsArray)
            {
                if (item is JObject alarmObj)
                {
                    alarms.AlarmDefinitions.Add(AlarmDefinition.FromJson(alarmObj));
                }
            }
        }

        return alarms;
    }
}

/// <summary>
/// Represents a single alarm definition.
/// </summary>
public class AlarmDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string Condition { get; set; } = "GreaterThan";
    public double Threshold { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Message { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Type { get; set; } = "HMI"; // "HMI" or "Controller"
    public string Source { get; set; } = "Digital"; // "Digital" or "Analog"

    /// <summary>
    /// Converts alarm definition to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["tagName"] = TagName,
            ["condition"] = Condition,
            ["threshold"] = Threshold,
            ["priority"] = Priority,
            ["message"] = Message,
            ["enabled"] = Enabled,
            ["type"] = Type,
            ["source"] = Source
        };
    }

    /// <summary>
    /// Creates alarm definition from JSON object.
    /// </summary>
    public static AlarmDefinition FromJson(JObject json)
    {
        var alarm = new AlarmDefinition
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            TagName = json["tagName"]?.ToString() ?? string.Empty,
            Condition = json["condition"]?.ToString() ?? "GreaterThan",
            Threshold = json["threshold"]?.ToObject<double>() ?? 0,
            Priority = json["priority"]?.ToString() ?? "Medium",
            Message = json["message"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            Type = json["type"]?.ToString() ?? "HMI",
            Source = json["source"]?.ToString() ?? "Digital"
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            alarm.Id = id;
        }

        return alarm;
    }
}
