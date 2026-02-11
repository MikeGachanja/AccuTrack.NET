using System.Text.Json.Nodes;

namespace Runtime.Modules.Scheduler;

public enum ScheduleType { OneTime, Daily, Weekly, Monthly }

/// <summary>Represents a scheduled task (script/command at time).</summary>
public sealed class Schedule
{
    public string Name { get; set; } = "";
    public ScheduleType Type { get; set; }
    public TimeOnly Time { get; set; }
    public DateTime OneTimeDateTime { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public List<int> DaysOfMonth { get; set; } = new();
    public string ScriptPath { get; set; } = "";
    public string Command { get; set; } = "";
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime LastExecution { get; set; }
    public DateTime NextExecution { get; set; }

    public DateTime CalculateNextExecution(DateTime from)
    {
        if (Type == ScheduleType.OneTime)
            return OneTimeDateTime > from ? OneTimeDateTime : from;
        if (Type == ScheduleType.Daily)
        {
            var next = from.Date.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
            return next > from ? next : next.AddDays(1);
        }
        // Weekly/Monthly: simplified
        var daily = from.Date.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
        return daily > from ? daily : daily.AddDays(1);
    }

    public bool ShouldExecute(DateTime now)
    {
        if (!Enabled) return false;
        return now >= NextExecution;
    }

    public void MarkExecuted(DateTime? executionTime = null)
    {
        var t = executionTime ?? DateTime.Now;
        LastExecution = t;
        NextExecution = CalculateNextExecution(t);
    }

    public static Schedule? FromJson(JsonObject json)
    {
        var s = new Schedule
        {
            Name = json["name"]?.GetValue<string>() ?? "",
            ScriptPath = json["scriptPath"]?.GetValue<string>() ?? "",
            Command = json["command"]?.GetValue<string>() ?? "",
            Enabled = json["enabled"]?.GetValue<bool>() ?? true,
            Priority = json["priority"]?.GetValue<int>() ?? 0
        };
        if (json["type"] != null && Enum.TryParse<ScheduleType>(json["type"]!.ToString(), out var st)) s.Type = st;
        if (json["time"] != null && TimeOnly.TryParse(json["time"]!.ToString(), out var to)) s.Time = to;
        if (json["oneTimeDateTime"] != null && DateTime.TryParse(json["oneTimeDateTime"]!.ToString(), out var dt)) s.OneTimeDateTime = dt;
        return s;
    }
}
