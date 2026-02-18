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

/// <summary>Target of a schedule: run a script or run an ML model.</summary>
public enum ScheduleTargetType
{
    Script,
    MLModel
}

/// <summary>
/// Represents a single schedule definition.
/// </summary>
public class ScheduleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    /// <summary>Script or ML model.</summary>
    public ScheduleTargetType TargetType { get; set; } = ScheduleTargetType.Script;
    public string ScriptName { get; set; } = string.Empty;
    /// <summary>ML model id (guid) when TargetType is MLModel.</summary>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>ML model display name when TargetType is MLModel.</summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>Recurrence: Once, Daily, Weekly, Monthly, Custom, Interval (for ML: run every N seconds).</summary>
    public string Recurrence { get; set; } = "Once";
    /// <summary>When Recurrence is Interval, run every this many seconds.</summary>
    public int IntervalSeconds { get; set; } = 60;
    /// <summary>When Recurrence is Interval, stagger: delay first run by this many seconds (stagger models).</summary>
    public int StaggerSeconds { get; set; } = 0;
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
        var obj = new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["targetType"] = TargetType.ToString(),
            ["scriptName"] = ScriptName,
            ["modelId"] = ModelId,
            ["modelName"] = ModelName,
            ["recurrence"] = Recurrence,
            ["intervalSeconds"] = IntervalSeconds,
            ["staggerSeconds"] = StaggerSeconds,
            ["time"] = Time,
            ["dayOfWeek"] = DayOfWeek,
            ["dayOfMonth"] = DayOfMonth,
            ["cronExpression"] = CronExpression,
            ["enabled"] = Enabled,
            ["description"] = Description
        };
        return obj;
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
            ModelId = json["modelId"]?.ToString() ?? string.Empty,
            ModelName = json["modelName"]?.ToString() ?? string.Empty,
            Recurrence = json["recurrence"]?.ToString() ?? "Once",
            IntervalSeconds = json["intervalSeconds"]?.ToObject<int>() ?? 60,
            StaggerSeconds = json["staggerSeconds"]?.ToObject<int>() ?? 0,
            Time = json["time"]?.ToString() ?? "00:00",
            DayOfWeek = json["dayOfWeek"]?.ToObject<int>() ?? 0,
            DayOfMonth = json["dayOfMonth"]?.ToObject<int>() ?? 1,
            CronExpression = json["cronExpression"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            Description = json["description"]?.ToString() ?? string.Empty
        };
        if (Enum.TryParse<ScheduleTargetType>(json["targetType"]?.ToString(), true, out var tt))
            schedule.TargetType = tt;
        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
            schedule.Id = id;
        return schedule;
    }
}
