using System.Collections.Generic;
using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.EventDispatcher;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.Alarms;

/// <summary>Alarms module: implements IModuleInterface, wraps AlarmManager, publishes alarm events.</summary>
public sealed class AlarmsModule : ModuleBase, IAlarms
{
    private readonly AlarmManager _alarmManager = new();
    private readonly List<TagSubscription> _tagSubscriptions = new();

    public AlarmManager AlarmManager => _alarmManager;

    public override string ModuleName => "AlarmsModule";
    public override string DisplayName => "Alarms Module";
    public override IReadOnlyList<string> Dependencies => new[] { "EventDispatcher", "TagsModule" };

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
        // Subscribe to tag updates for alarm evaluation
        SubscribeToTagUpdates();
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        // Unsubscribe from tag updates
        UnsubscribeFromTagUpdates();
        SetRunning(false);
    }

    /// <summary>
    /// Subscribes to all tag updates so alarms can be evaluated when tag values change.
    /// </summary>
    private void SubscribeToTagUpdates()
    {
        var tagsModule = Runtime.Modules.ExecutionEngine.ExecutionEngine.Instance.ModuleManager.GetModule("TagsModule") as TagsModule;
        if (tagsModule == null) return;

        var tagManager = tagsModule.TagManager;
        if (tagManager == null) return;

        // Clear existing subscriptions
        UnsubscribeFromTagUpdates();

        // Subscribe to all tags
        foreach (var tagName in tagManager.GetTagNames())
        {
            var subscription = tagManager.Subscribe(tagName, (name, value, quality) =>
            {
                // Evaluate alarms for this tag
                _alarmManager.EvaluateTagValue(name, value, (int)quality);
            });
            _tagSubscriptions.Add(subscription);
        }

        System.Diagnostics.Debug.WriteLine($"[AlarmsModule] Subscribed to {_tagSubscriptions.Count} tag(s) for alarm evaluation");
    }

    /// <summary>
    /// Unsubscribes from all tag updates.
    /// </summary>
    private void UnsubscribeFromTagUpdates()
    {
        foreach (var subscription in _tagSubscriptions)
        {
            subscription?.Dispose();
        }
        _tagSubscriptions.Clear();
    }

    public IReadOnlyList<string> GetActiveAlarms() => _alarmManager.GetActiveAlarms();
    public IReadOnlyList<string> GetUnacknowledgedAlarms() => _alarmManager.GetUnacknowledgedAlarms();
    public bool AcknowledgeAlarm(string alarmName) => _alarmManager.AcknowledgeAlarm(alarmName);
    public void AcknowledgeAllAlarms() => _alarmManager.AcknowledgeAllAlarms();
}
