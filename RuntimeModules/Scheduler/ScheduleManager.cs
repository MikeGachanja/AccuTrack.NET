using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Runtime.Modules.ScriptingEngine;

namespace Runtime.Modules.Scheduler;

/// <summary>Manages schedules and triggers execution.</summary>
public sealed class ScheduleManager
{
    private readonly ConcurrentDictionary<string, Schedule> _schedules = new();
    private readonly ScheduleExecutor _executor = new();
    private Timer? _checkTimer;
    private volatile bool _running;

    public ScheduleExecutor Executor => _executor;
    public bool IsRunning => _running;

    public void SetScriptEngine(IScriptingEngine? engine) => _executor.SetScriptEngine(engine);
    public void SetMLRunAction(Action<string>? runModelById) => _executor.SetMLRunAction(runModelById);
    public void SetScriptPathResolver(Func<string, string?>? resolver) => _executor.SetScriptPathResolver(resolver);

    public bool LoadFromJsonFile(string filePath)
    {
        System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJsonFile: Attempting to load schedules from {filePath}");
        if (!File.Exists(filePath))
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJsonFile: File not found: {filePath}");
            return false;
        }
        try
        {
            var json = File.ReadAllText(filePath);
            var node = JsonNode.Parse(json);
            if (node is JsonObject root && root["schedules"] is JsonArray arr)
            {
                int loadedCount = 0;
                foreach (var item in arr)
                {
                    if (item is JsonObject obj && Schedule.FromJson(obj) is { } s)
                    {
                        if (AddSchedule(s))
                            loadedCount++;
                    }
                }
                System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJsonFile: Successfully loaded {loadedCount} schedule(s) from {filePath}");
                return true;
            }
            System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJsonFile: No 'schedules' array found in JSON");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJsonFile: Error loading schedules: {ex.Message}");
            return false;
        }
    }

    public bool LoadFromJson(JsonElement root)
    {
        System.Diagnostics.Trace.WriteLine("[Scheduler] LoadFromJson: Attempting to load schedules from JsonElement");
        try
        {
            if (root.TryGetProperty("schedules", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                int loadedCount = 0;
                foreach (var item in arr.EnumerateArray())
                {
                    var n = JsonNode.Parse(item.GetRawText());
                    if (n is JsonObject obj && Schedule.FromJson(obj) is { } s)
                    {
                        if (AddSchedule(s))
                            loadedCount++;
                    }
                }
                System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJson: Successfully loaded {loadedCount} schedule(s) from JsonElement");
                return true;
            }
            System.Diagnostics.Trace.WriteLine("[Scheduler] LoadFromJson: No 'schedules' array found in JsonElement");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] LoadFromJson: Error loading schedules: {ex.Message}");
            return false;
        }
    }

    public bool AddSchedule(Schedule schedule)
    {
        if (schedule == null || string.IsNullOrEmpty(schedule.Name))
        {
            System.Diagnostics.Trace.WriteLine("[Scheduler] AddSchedule: Invalid schedule (null or empty name)");
            return false;
        }
        
        var now = DateTime.Now;
        schedule.NextExecution = schedule.CalculateNextExecution(now);
        _schedules[schedule.Name] = schedule;
        
        System.Diagnostics.Trace.WriteLine($"[Scheduler] AddSchedule: Added '{schedule.Name}' - Type: {schedule.Type}, Target: {schedule.TargetKind}, " +
            $"Enabled: {schedule.Enabled}, NextExecution: {schedule.NextExecution:yyyy-MM-dd HH:mm:ss}, " +
            $"Interval: {schedule.IntervalSeconds}s, Stagger: {schedule.StaggerSeconds}s");
        
        return true;
    }

    public bool RemoveSchedule(string scheduleName) => _schedules.TryRemove(scheduleName, out _);
    public Schedule? GetSchedule(string scheduleName) => _schedules.TryGetValue(scheduleName, out var s) ? s : null;
    public IReadOnlyList<string> GetScheduleNames() => _schedules.Keys.ToList();

    /// <summary>Clear all schedules (e.g. before re-loading project config).</summary>
    public void Clear()
    {
        _schedules.Clear();
    }

    public void Start()
    {
        if (_running)
        {
            System.Diagnostics.Trace.WriteLine("[Scheduler] Start: Already running");
            return;
        }
        _running = true;
        _checkTimer = new Timer(_ => CheckSchedules(), null, 1000, 1000);
        System.Diagnostics.Trace.WriteLine($"[Scheduler] Start: Scheduler started with {_schedules.Count} schedule(s)");
        
        // Log all schedules
        foreach (var schedule in _schedules.Values)
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Schedule '{schedule.Name}': Enabled={schedule.Enabled}, " +
                $"NextExecution={schedule.NextExecution:yyyy-MM-dd HH:mm:ss}, Type={schedule.Type}");
        }
    }

    public void Stop()
    {
        _running = false;
        _checkTimer?.Dispose();
        _checkTimer = null;
        System.Diagnostics.Trace.WriteLine("[Scheduler] Stop: Scheduler stopped");
    }

    private void CheckSchedules()
    {
        if (!_running) return;
        
        var now = DateTime.Now;
        int checkedCount = 0;
        int executedCount = 0;
        
        foreach (var schedule in _schedules.Values)
        {
            checkedCount++;
            if (!schedule.Enabled)
            {
                continue; // Skip disabled schedules silently
            }
            
            if (!schedule.ShouldExecute(now))
            {
                // Log occasionally for debugging (every 10 checks or when close to execution)
                var timeUntilExecution = (schedule.NextExecution - now).TotalSeconds;
                if (checkedCount % 10 == 0 || (timeUntilExecution > 0 && timeUntilExecution < 5))
                {
                    System.Diagnostics.Trace.WriteLine($"[Scheduler] CheckSchedules: '{schedule.Name}' - NextExecution: {schedule.NextExecution:HH:mm:ss}, " +
                        $"Time until: {timeUntilExecution:F1}s");
                }
                continue;
            }
            
            executedCount++;
            System.Diagnostics.Trace.WriteLine($"[Scheduler] CheckSchedules: Executing '{schedule.Name}' at {now:HH:mm:ss}");
            try
            {
                var ok = _executor.Execute(schedule);
                schedule.MarkExecuted(now);
                if (ok)
                {
                    System.Diagnostics.Trace.WriteLine($"[Scheduler] Successfully executed: {schedule.Name} " +
                        $"({(schedule.TargetKind == ScheduleTargetKind.MLModel ? "ML model " + schedule.ModelId : "Script " + schedule.ScriptName)}). " +
                        $"Next execution: {schedule.NextExecution:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[Scheduler] Execution returned false for: {schedule.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute failed for {schedule.Name}: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Exception details: {ex}");
            }
        }
        
        if (executedCount > 0)
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] CheckSchedules: Checked {checkedCount} schedule(s), executed {executedCount}");
        }
    }
}
