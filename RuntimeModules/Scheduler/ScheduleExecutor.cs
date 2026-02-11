using Runtime.Modules.ScriptingEngine;

namespace Runtime.Modules.Scheduler;

/// <summary>Executes scheduled tasks (script or command).</summary>
public sealed class ScheduleExecutor
{
    private IScriptingEngine? _scriptEngine;

    public void SetScriptEngine(IScriptingEngine? engine) => _scriptEngine = engine;

    public bool Execute(Schedule schedule)
    {
        if (schedule == null) return false;
        if (!string.IsNullOrEmpty(schedule.ScriptPath) && _scriptEngine != null)
        {
            if (!_scriptEngine.LoadScript(schedule.ScriptPath))
                return false;
            return _scriptEngine.ExecuteScript();
        }
        if (!string.IsNullOrEmpty(schedule.Command))
        {
            // Stub: run command (e.g. Process.Start)
            return true;
        }
        return false;
    }
}
