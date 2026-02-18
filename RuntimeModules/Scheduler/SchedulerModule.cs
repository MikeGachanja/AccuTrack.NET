using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.ScriptingEngine;

namespace Runtime.Modules.Scheduler;

/// <summary>Scheduler module: implements IModuleInterface, wraps ScheduleManager.</summary>
public sealed class SchedulerModule : ModuleBase, IScheduler
{
    private readonly ScheduleManager _scheduleManager = new();

    public ScheduleManager ScheduleManager => _scheduleManager;

    public override string ModuleName => "SchedulerModule";
    public override string DisplayName => "Scheduler Module";
    public override IReadOnlyList<string> Dependencies => new[] { "EventDispatcher" };

    /// <summary>Set script engine for schedule execution. Called by host.</summary>
    public void SetScriptEngine(IScriptingEngine? engine) => _scheduleManager.SetScriptEngine(engine);
    /// <summary>Set callback to run an ML model by id. Called by host.</summary>
    public void SetMLRunAction(Action<string>? runModelById) => _scheduleManager.SetMLRunAction(runModelById);
    /// <summary>Resolve script name to full path. Called by host.</summary>
    public void SetScriptPathResolver(Func<string, string?>? resolver) => _scheduleManager.SetScriptPathResolver(resolver);

    public override bool Initialize(JsonObject? config = null)
    {
        _scheduleManager.Clear();
        if (config != null && config["schedules"] is JsonArray arr)
        {
            foreach (var node in arr)
            {
                if (node is JsonObject obj && Schedule.FromJson(obj) is { } s)
                {
                    _scheduleManager.AddSchedule(s);
                    System.Diagnostics.Trace.WriteLine($"[Scheduler] Loaded schedule: {s.Name} ({(s.TargetKind == ScheduleTargetKind.MLModel ? "ML" : "Script")}, {(s.Type == ScheduleType.Interval ? $"every {s.IntervalSeconds}s" : s.Type.ToString())})");
                }
            }
        }
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        _scheduleManager.Start();
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        _scheduleManager.Stop();
        SetRunning(false);
    }
}
