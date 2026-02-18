using System.Text.Json.Nodes;

namespace Runtime.Modules.Scheduler;

public enum ScheduleType { OneTime, Daily, Weekly, Monthly, Interval }

/// <summary>Target of a schedule: run a script or run an ML model.</summary>
public enum ScheduleTargetKind { Script, MLModel }

/// <summary>Represents a scheduled task (script or ML model at time/interval).</summary>
public sealed class Schedule
{
    public string Name { get; set; } = "";
    public ScheduleType Type { get; set; }
    public ScheduleTargetKind TargetKind { get; set; } = ScheduleTargetKind.Script;
    public TimeOnly Time { get; set; }
    public DateTime OneTimeDateTime { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public List<int> DaysOfMonth { get; set; } = new();
    public string ScriptPath { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int IntervalSeconds { get; set; } = 60;
    public int StaggerSeconds { get; set; } = 0;
    public string Command { get; set; } = "";
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime LastExecution { get; set; }
    public DateTime NextExecution { get; set; }

    public DateTime CalculateNextExecution(DateTime from)
    {
        if (Type == ScheduleType.Interval)
        {
            if (LastExecution == default)
                return from.AddSeconds(StaggerSeconds);
            return LastExecution.AddSeconds(IntervalSeconds);
        }
        if (Type == ScheduleType.OneTime)
            return OneTimeDateTime > from ? OneTimeDateTime : from;
        if (Type == ScheduleType.Daily)
        {
            var next = from.Date.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
            return next > from ? next : next.AddDays(1);
        }
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
            ScriptName = json["scriptName"]?.GetValue<string>() ?? "",
            ModelId = json["modelId"]?.GetValue<string>() ?? "",
            IntervalSeconds = json["intervalSeconds"]?.GetValue<int>() ?? 60,
            StaggerSeconds = json["staggerSeconds"]?.GetValue<int>() ?? 0,
            Command = json["command"]?.GetValue<string>() ?? "",
            Enabled = json["enabled"]?.GetValue<bool>() ?? true,
            Priority = json["priority"]?.GetValue<int>() ?? 0
        };
        if (json["type"] != null && Enum.TryParse<ScheduleType>(json["type"]!.ToString(), true, out var st)) s.Type = st;
        else if (json["recurrence"] != null)
        {
            var rec = json["recurrence"]!.ToString();
            s.Type = rec.Equals("Interval", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Interval
                : rec.Equals("Once", StringComparison.OrdinalIgnoreCase) ? ScheduleType.OneTime
                : rec.Equals("Daily", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Daily
                : rec.Equals("Weekly", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Weekly
                : rec.Equals("Monthly", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Monthly
                : ScheduleType.Daily;
        }
        if (json["targetType"] != null)
        {
            var tt = json["targetType"]!.ToString();
            s.TargetKind = tt.IndexOf("ML", StringComparison.OrdinalIgnoreCase) >= 0 ? ScheduleTargetKind.MLModel : ScheduleTargetKind.Script;
        }
        if (json["time"] != null && TimeOnly.TryParse(json["time"]!.ToString(), out var to)) s.Time = to;
        if (json["oneTimeDateTime"] != null && DateTime.TryParse(json["oneTimeDateTime"]!.ToString(), out var dt)) s.OneTimeDateTime = dt;
        return s;
    }
}
