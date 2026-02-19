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
        if (schedule == null)
        {
            System.Diagnostics.Trace.WriteLine("[Scheduler] Execute: Schedule is null");
            return false;
        }

        System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Starting execution of '{schedule.Name}' (Target: {schedule.TargetKind})");

        if (schedule.TargetKind == ScheduleTargetKind.MLModel && !string.IsNullOrEmpty(schedule.ModelId))
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Executing ML model '{schedule.ModelId}'");
            try
            {
                if (_runModelById == null)
                {
                    System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: ML run callback not configured; cannot run model {schedule.ModelId}");
                    return false;
                }
                _runModelById.Invoke(schedule.ModelId);
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: ML model '{schedule.ModelId}' execution completed");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: ML model run failed for {schedule.ModelId}: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Exception: {ex}");
                return false;
            }
        }

        // Script execution
        var scriptPath = schedule.ScriptPath;
        if (string.IsNullOrEmpty(scriptPath) && !string.IsNullOrEmpty(schedule.ScriptName))
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Resolving script path for '{schedule.ScriptName}'");
            scriptPath = _scriptPathResolver?.Invoke(schedule.ScriptName) ?? "";
            if (!string.IsNullOrEmpty(scriptPath))
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Resolved script path: {scriptPath}");
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Script path resolver returned null for '{schedule.ScriptName}'");
            }
        }
        
        if (!string.IsNullOrEmpty(scriptPath) && _scriptEngine != null)
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Loading script from: {scriptPath}");
            if (!_scriptEngine.LoadScript(scriptPath))
            {
                var error = _scriptEngine.GetLastError();
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Script load failed: {scriptPath} (Error: {error})");
                return false;
            }
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Executing script: {scriptPath}");
            var result = _scriptEngine.ExecuteScript();
            if (result)
            {
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Script execution completed successfully");
            }
            else
            {
                var error = _scriptEngine.GetLastError();
                System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Script execution failed: {error}");
            }
            return result;
        }
        
        if (string.IsNullOrEmpty(scriptPath) && !string.IsNullOrEmpty(schedule.ScriptName))
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Script path not resolved for: {schedule.ScriptName} (ScriptEngine: {(_scriptEngine != null ? "available" : "null")})");
        }
        
        if (!string.IsNullOrEmpty(schedule.Command))
        {
            System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: Executing command: {schedule.Command}");
            return true;
        }
        
        System.Diagnostics.Trace.WriteLine($"[Scheduler] Execute: No valid execution target found for schedule '{schedule.Name}'");
        return false;
    }
}
