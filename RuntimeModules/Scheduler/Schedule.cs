using System.Linq;
using System.Text.Json.Nodes;

namespace Runtime.Modules.Scheduler;

public enum ScheduleType { OneTime, Daily, Weekly, Monthly, Interval, Custom }

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
    public int DayOfWeek { get; set; } = -1; // -1 = not set, 0 = Sunday, 1 = Monday, etc.
    public int DayOfMonth { get; set; } = -1; // -1 = not set, 1-31 = day of month
    public string CronExpression { get; set; } = ""; // For custom schedules
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
        
        var scheduledTime = from.Date.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
        
        if (Type == ScheduleType.Daily)
        {
            return scheduledTime > from ? scheduledTime : scheduledTime.AddDays(1);
        }
        
        if (Type == ScheduleType.Weekly)
        {
            // If dayOfWeek is set, use it; otherwise use DaysOfWeek list
            if (DayOfWeek >= 0 && DayOfWeek <= 6)
            {
                var targetDay = (DayOfWeek)DayOfWeek;
                var daysUntilTarget = ((int)targetDay - (int)from.DayOfWeek + 7) % 7;
                if (daysUntilTarget == 0 && scheduledTime <= from)
                    daysUntilTarget = 7;
                return scheduledTime.AddDays(daysUntilTarget);
            }
            else if (DaysOfWeek.Count > 0)
            {
                // Find next matching day of week
                for (int i = 1; i <= 7; i++)
                {
                    var candidate = scheduledTime.AddDays(i);
                    if (DaysOfWeek.Contains(candidate.DayOfWeek) && candidate > from)
                        return candidate;
                }
            }
            // Default: next occurrence at same time
            return scheduledTime > from ? scheduledTime : scheduledTime.AddDays(1);
        }
        
        if (Type == ScheduleType.Monthly)
        {
            if (DayOfMonth > 0 && DayOfMonth <= 31)
            {
                var targetDate = new DateTime(from.Year, from.Month, Math.Min(DayOfMonth, DateTime.DaysInMonth(from.Year, from.Month)));
                targetDate = targetDate.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
                
                if (targetDate <= from)
                {
                    // Move to next month
                    targetDate = targetDate.AddMonths(1);
                    var nextMonth = targetDate.Month;
                    var nextYear = targetDate.Year;
                    var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
                    targetDate = new DateTime(nextYear, nextMonth, Math.Min(DayOfMonth, daysInNextMonth));
                    targetDate = targetDate.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
                }
                return targetDate;
            }
            else if (DaysOfMonth.Count > 0)
            {
                // Find next matching day of month
                var currentMonth = from.Month;
                var currentYear = from.Year;
                var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
                
                foreach (var day in DaysOfMonth.OrderBy(d => d))
                {
                    if (day < 1 || day > 31) continue;
                    var targetDay = Math.Min(day, daysInMonth);
                    var candidate = new DateTime(currentYear, currentMonth, targetDay);
                    candidate = candidate.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
                    
                    if (candidate > from)
                        return candidate;
                }
                
                // Move to next month
                var nextMonth = currentMonth == 12 ? 1 : currentMonth + 1;
                var nextYear = currentMonth == 12 ? currentYear + 1 : currentYear;
                daysInMonth = DateTime.DaysInMonth(nextYear, nextMonth);
                var firstDay = DaysOfMonth.Where(d => d >= 1 && d <= 31).OrderBy(d => d).FirstOrDefault();
                if (firstDay > 0)
                {
                    var targetDay = Math.Min(firstDay, daysInMonth);
                    var candidate = new DateTime(nextYear, nextMonth, targetDay);
                    return candidate.Add(new TimeSpan(Time.Hour, Time.Minute, Time.Second));
                }
            }
            // Default: next occurrence at same time next month
            return scheduledTime > from ? scheduledTime : scheduledTime.AddMonths(1);
        }
        
        if (Type == ScheduleType.Custom)
        {
            // For Custom schedules, if cronExpression is provided, we'd need a cron parser
            // For now, fall back to interval-based if intervalSeconds is set, otherwise daily
            if (IntervalSeconds > 0)
            {
                if (LastExecution == default)
                    return from.AddSeconds(StaggerSeconds);
                return LastExecution.AddSeconds(IntervalSeconds);
            }
            // Default to daily if no interval specified
            return scheduledTime > from ? scheduledTime : scheduledTime.AddDays(1);
        }
        
        // Default fallback
        return scheduledTime > from ? scheduledTime : scheduledTime.AddDays(1);
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
            Priority = json["priority"]?.GetValue<int>() ?? 0,
            DayOfWeek = json["dayOfWeek"]?.GetValue<int>() ?? -1,
            DayOfMonth = json["dayOfMonth"]?.GetValue<int>() ?? -1,
            CronExpression = json["cronExpression"]?.GetValue<string>() ?? ""
        };
        
        // Parse schedule type from "type" field or "recurrence" field
        if (json["type"] != null && Enum.TryParse<ScheduleType>(json["type"]!.ToString(), true, out var st))
        {
            s.Type = st;
        }
        else if (json["recurrence"] != null)
        {
            var rec = json["recurrence"]!.ToString();
            s.Type = rec.Equals("Interval", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Interval
                : rec.Equals("Once", StringComparison.OrdinalIgnoreCase) ? ScheduleType.OneTime
                : rec.Equals("Daily", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Daily
                : rec.Equals("Weekly", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Weekly
                : rec.Equals("Monthly", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Monthly
                : rec.Equals("Custom", StringComparison.OrdinalIgnoreCase) ? ScheduleType.Custom
                : ScheduleType.Daily;
        }
        
        // Parse target type (Script or MLModel)
        if (json["targetType"] != null)
        {
            var tt = json["targetType"]!.ToString();
            s.TargetKind = tt.IndexOf("ML", StringComparison.OrdinalIgnoreCase) >= 0 ? ScheduleTargetKind.MLModel : ScheduleTargetKind.Script;
        }
        
        // Parse time field (HH:mm format)
        if (json["time"] != null && TimeOnly.TryParse(json["time"]!.ToString(), out var to))
        {
            s.Time = to;
        }
        
        // Parse oneTimeDateTime if present
        if (json["oneTimeDateTime"] != null && DateTime.TryParse(json["oneTimeDateTime"]!.ToString(), out var dt))
        {
            s.OneTimeDateTime = dt;
        }
        
        // For Custom recurrence with intervalSeconds, treat it as Interval for execution timing
        if (s.Type == ScheduleType.Custom && s.IntervalSeconds > 0)
        {
            // Keep as Custom but use interval-based execution
            System.Diagnostics.Trace.WriteLine($"[Scheduler] FromJson: Custom schedule '{s.Name}' using interval-based execution ({s.IntervalSeconds}s)");
        }
        
        System.Diagnostics.Trace.WriteLine($"[Scheduler] FromJson: Parsed schedule '{s.Name}' - Type: {s.Type}, Target: {s.TargetKind}, " +
            $"Enabled: {s.Enabled}, Interval: {s.IntervalSeconds}s, Time: {s.Time}");
        
        return s;
    }
}
