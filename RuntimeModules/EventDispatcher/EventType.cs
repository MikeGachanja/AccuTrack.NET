namespace Runtime.Modules.EventDispatcher;

/// <summary>
/// Event type enumeration for SCADA runtime events.
/// </summary>
public enum EventType
{
    TagValueChanged,
    AlarmTriggered,
    AlarmAcknowledged,
    AlarmCleared,
    ScheduleExecuted,
    CommunicationError,
    CommunicationConnected,
    CommunicationDisconnected,
    ModuleStarted,
    ModuleStopped,
    ModuleError,
    SystemShutdown,
    Custom
}

/// <summary>
/// Event priority levels.
/// </summary>
public enum EventPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
