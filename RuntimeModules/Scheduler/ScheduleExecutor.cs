using Runtime.Modules.ScriptingEngine;

namespace Runtime.Modules.Scheduler;

/// <summary>Executes scheduled tasks (script, ML model, or command).</summary>
public sealed class ScheduleExecutor
{
    private IScriptingEngine? _scriptEngine;
    private Action<string>? _runModelById;
    private Func<string, string?>? _scriptPathResolver;

    public void SetScriptEngine(IScriptingEngine? engine) => _scriptEngine = engine;

    /// <summary>Set callback to run an ML model by id when schedule target is MLModel.</summary>
    public void SetMLRunAction(Action<string>? runModelById) => _runModelById = runModelById;

    /// <summary>Resolve script name to full path when schedule has ScriptName but no ScriptPath.</summary>
    public void SetScriptPathResolver(Func<string, string?>? resolver) => _scriptPathResolver = resolver;

    public bool Execute(Schedule schedule)
    {
        if (schedule == null) return false;

        if (schedule.TargetKind == ScheduleTargetKind.MLModel && !string.IsNullOrEmpty(schedule.ModelId))
        {
            try
            {
                if (_runModelById == null)
                {
                    System.Diagnostics.Trace.WriteLine($"[Scheduler] ML run not configured; cannot run model {schedule.ModelId}");
                    return false;
                }
                _runModelById.Invoke(schedule.ModelId);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] ML model run failed for {schedule.ModelId}: {ex.Message}");
                return false;
            }
        }

        var scriptPath = schedule.ScriptPath;
        if (string.IsNullOrEmpty(scriptPath) && !string.IsNullOrEmpty(schedule.ScriptName))
            scriptPath = _scriptPathResolver?.Invoke(schedule.ScriptName) ?? "";
        if (!string.IsNullOrEmpty(scriptPath) && _scriptEngine != null)
        {
            if (!_scriptEngine.LoadScript(scriptPath))
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Script load failed: {scriptPath} ({_scriptEngine.GetLastError()})");
                return false;
            }
            return _scriptEngine.ExecuteScript();
        }
        if (string.IsNullOrEmpty(scriptPath) && !string.IsNullOrEmpty(schedule.ScriptName))
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Script path not resolved for: {schedule.ScriptName}");
        if (!string.IsNullOrEmpty(schedule.Command))
            return true;
        return false;
    }
}
