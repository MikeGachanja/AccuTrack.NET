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
        if (!File.Exists(filePath)) return false;
        try
        {
            var json = File.ReadAllText(filePath);
            var node = JsonNode.Parse(json);
            if (node is JsonObject root && root["schedules"] is JsonArray arr)
            {
                foreach (var item in arr)
                    if (item is JsonObject obj && Schedule.FromJson(obj) is { } s)
                        AddSchedule(s);
            }
            return true;
        }
        catch { return false; }
    }

    public bool LoadFromJson(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("schedules", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var n = JsonNode.Parse(item.GetRawText());
                    if (n is JsonObject obj && Schedule.FromJson(obj) is { } s)
                        AddSchedule(s);
                }
            }
            return true;
        }
        catch { return false; }
    }

    public bool AddSchedule(Schedule schedule)
    {
        if (schedule == null || string.IsNullOrEmpty(schedule.Name)) return false;
        schedule.NextExecution = schedule.CalculateNextExecution(DateTime.Now);
        _schedules[schedule.Name] = schedule;
        return true;
    }

    public bool RemoveSchedule(string scheduleName) => _schedules.TryRemove(scheduleName, out _);
    public Schedule? GetSchedule(string scheduleName) => _schedules.TryGetValue(scheduleName, out var s) ? s : null;
    public IReadOnlyList<string> GetScheduleNames() => _schedules.Keys.ToList();

    public void Start()
    {
        if (_running) return;
        _running = true;
        _checkTimer = new Timer(_ => CheckSchedules(), null, 1000, 1000);
    }

    public void Stop()
    {
        _running = false;
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    private void CheckSchedules()
    {
        var now = DateTime.Now;
        foreach (var schedule in _schedules.Values)
        {
            if (!schedule.ShouldExecute(now)) continue;
            try
            {
                _executor.Execute(schedule);
                schedule.MarkExecuted(now);
            }
            catch { }
        }
    }
}
