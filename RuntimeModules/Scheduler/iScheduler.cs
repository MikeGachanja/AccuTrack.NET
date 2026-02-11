namespace Runtime.Modules.Scheduler;

/// <summary>Scheduler module interface. Runs scheduled scripts (e.g. by time of day).</summary>
public interface IScheduler
{
    /// <summary>Manages schedules and execution; requires ScriptingEngine to be set by host.</summary>
    ScheduleManager ScheduleManager { get; }
}
