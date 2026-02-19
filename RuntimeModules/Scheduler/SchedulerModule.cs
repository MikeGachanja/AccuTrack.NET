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
        System.Diagnostics.Trace.WriteLine("[Scheduler] Initialize: Starting scheduler initialization");
        _scheduleManager.Clear();
        
        if (config != null && config["schedules"] is JsonArray arr)
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Initialize: Found {arr.Count} schedule(s) in config");
            int loadedCount = 0;
            foreach (var node in arr)
            {
                if (node is JsonObject obj && Schedule.FromJson(obj) is { } s)
                {
                    if (_scheduleManager.AddSchedule(s))
                    {
                        loadedCount++;
                        System.Diagnostics.Trace.WriteLine($"[Scheduler] Initialize: Loaded schedule: {s.Name} " +
                            $"(Target: {(s.TargetKind == ScheduleTargetKind.MLModel ? "ML " + s.ModelId : "Script " + s.ScriptName)}, " +
                            $"Type: {(s.Type == ScheduleType.Interval ? $"Interval every {s.IntervalSeconds}s" : s.Type.ToString())}, " +
                            $"Enabled: {s.Enabled})");
                    }
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("[Scheduler] Initialize: Failed to parse schedule node");
                }
            }
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Initialize: Successfully loaded {loadedCount} schedule(s)");
        }
        else
        {
            System.Diagnostics.Trace.WriteLine("[Scheduler] Initialize: No schedules found in config (config is null or schedules array not found)");
        }
        
        SetStatus("Initialized");
        RaiseInitialized();
        System.Diagnostics.Trace.WriteLine("[Scheduler] Initialize: Scheduler initialization completed");
        return true;
    }

    public override bool Start()
    {
        System.Diagnostics.Trace.WriteLine("[Scheduler] Start: Starting scheduler module");
        _scheduleManager.Start();
        SetRunning(true);
        System.Diagnostics.Trace.WriteLine("[Scheduler] Start: Scheduler module started successfully");
        return true;
    }

    public override void Stop()
    {
        System.Diagnostics.Trace.WriteLine("[Scheduler] Stop: Stopping scheduler module");
        _scheduleManager.Stop();
        SetRunning(false);
        System.Diagnostics.Trace.WriteLine("[Scheduler] Stop: Scheduler module stopped");
    }
}
