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

    public override bool Initialize(JsonObject? config = null)
    {
        if (config != null)
        {
            if (config["schedules"] is JsonArray arr)
            {
                foreach (var node in arr)
                    if (node is JsonObject obj && Schedule.FromJson(obj) is { } s)
                        _scheduleManager.AddSchedule(s);
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
