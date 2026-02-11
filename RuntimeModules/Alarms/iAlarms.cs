namespace Runtime.Modules.Alarms;

/// <summary>Alarms module interface. Manages active alarms and acknowledgment.</summary>
public interface IAlarms
{
    /// <summary>Underlying alarm manager (conditions, state).</summary>
    AlarmManager AlarmManager { get; }
    /// <summary>Returns display strings for all active alarms.</summary>
    IReadOnlyList<string> GetActiveAlarms();
    /// <summary>Returns display strings for unacknowledged alarms.</summary>
    IReadOnlyList<string> GetUnacknowledgedAlarms();
    /// <summary>Acknowledges a single alarm by name. Returns true if found and acknowledged.</summary>
    bool AcknowledgeAlarm(string alarmName);
    /// <summary>Acknowledges all active alarms.</summary>
    void AcknowledgeAllAlarms();
}
