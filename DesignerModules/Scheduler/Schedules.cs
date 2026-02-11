using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Scheduler;

/// <summary>
/// Represents schedules configuration for a SCADA project.
/// </summary>
public class Schedules
{
    public List<ScheduleDefinition> ScheduleDefinitions { get; set; } = new List<ScheduleDefinition>();

    /// <summary>
    /// Converts schedules to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var obj = new JObject();
        var schedulesArray = new JArray();

        foreach (var schedule in ScheduleDefinitions)
        {
            schedulesArray.Add(schedule.ToJson());
        }

        obj["schedules"] = schedulesArray;
        return obj;
    }

    /// <summary>
    /// Creates schedules from JSON object.
    /// </summary>
    public static Schedules FromJson(JObject json)
    {
        var schedules = new Schedules();
        var schedulesArray = json["schedules"] as JArray;

        if (schedulesArray != null)
        {
            foreach (var item in schedulesArray)
            {
                if (item is JObject scheduleObj)
                {
                    schedules.ScheduleDefinitions.Add(ScheduleDefinition.FromJson(scheduleObj));
                }
            }
        }

        return schedules;
    }
}

/// <summary>
/// Represents a single schedule definition.
/// </summary>
public class ScheduleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
    public string Recurrence { get; set; } = "Once"; // Once, Daily, Weekly, Monthly, Custom
    public string Time { get; set; } = "00:00"; // HH:mm format
    public int DayOfWeek { get; set; } = 0; // 0 = Sunday, 1 = Monday, etc.
    public int DayOfMonth { get; set; } = 1; // 1-31
    public string CronExpression { get; set; } = string.Empty; // For custom schedules
    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Converts schedule definition to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["scriptName"] = ScriptName,
            ["recurrence"] = Recurrence,
            ["time"] = Time,
            ["dayOfWeek"] = DayOfWeek,
            ["dayOfMonth"] = DayOfMonth,
            ["cronExpression"] = CronExpression,
            ["enabled"] = Enabled,
            ["description"] = Description
        };
    }

    /// <summary>
    /// Creates schedule definition from JSON object.
    /// </summary>
    public static ScheduleDefinition FromJson(JObject json)
    {
        var schedule = new ScheduleDefinition
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            ScriptName = json["scriptName"]?.ToString() ?? string.Empty,
            Recurrence = json["recurrence"]?.ToString() ?? "Once",
            Time = json["time"]?.ToString() ?? "00:00",
            DayOfWeek = json["dayOfWeek"]?.ToObject<int>() ?? 0,
            DayOfMonth = json["dayOfMonth"]?.ToObject<int>() ?? 1,
            CronExpression = json["cronExpression"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            Description = json["description"]?.ToString() ?? string.Empty
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            schedule.Id = id;
        }

        return schedule;
    }
}
