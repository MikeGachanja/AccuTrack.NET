using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.EventDispatcher;

namespace Runtime.Modules.Alarms;

/// <summary>Alarms module: implements IModuleInterface, wraps AlarmManager, publishes alarm events.</summary>
public sealed class AlarmsModule : ModuleBase, IAlarms
{
    private readonly AlarmManager _alarmManager = new();

    public AlarmManager AlarmManager => _alarmManager;

    public override string ModuleName => "AlarmsModule";
    public override string DisplayName => "Alarms Module";
    public override IReadOnlyList<string> Dependencies => new[] { "EventDispatcher" };

    public override bool Initialize(JsonObject? config = null)
    {
        if (config != null)
        {
            // Config may be root with "alarms" array
            if (config["alarms"] is JsonArray arr)
            {
                foreach (var node in arr)
                    if (node is JsonObject obj && Alarm.FromJson(obj) is { } alarm)
                        _alarmManager.AddAlarm(alarm);
            }
        }

        var dispatcher = Runtime.Modules.ExecutionEngine.ExecutionEngine.Instance.ModuleManager.GetModule("EventDispatcher") as Runtime.Modules.EventDispatcher.EventDispatcher;
        if (dispatcher != null)
        {
            _alarmManager.SetEventPublisher((name, eventType, state) =>
            {
                var evt = new Event(
                    eventType == "Activated" ? EventType.AlarmTriggered :
                    eventType == "Acknowledged" ? EventType.AlarmAcknowledged :
                    eventType == "Cleared" ? EventType.AlarmCleared : EventType.Custom,
                    "AlarmsModule",
                    EventPriority.Normal);
                evt.SetData("alarmName", name);
                evt.SetData("state", state.ToString());
                dispatcher.Publish(evt);
            });
        }

        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        SetRunning(false);
    }

    public IReadOnlyList<string> GetActiveAlarms() => _alarmManager.GetActiveAlarms();
    public IReadOnlyList<string> GetUnacknowledgedAlarms() => _alarmManager.GetUnacknowledgedAlarms();
    public bool AcknowledgeAlarm(string alarmName) => _alarmManager.AcknowledgeAlarm(alarmName);
    public void AcknowledgeAllAlarms() => _alarmManager.AcknowledgeAllAlarms();
}
