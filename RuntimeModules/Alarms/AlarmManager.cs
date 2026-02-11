using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runtime.Modules.Alarms;

/// <summary>Manages alarms: load from JSON, evaluate conditions, acknowledge, publish events.</summary>
public sealed class AlarmManager
{
    private readonly ConcurrentDictionary<string, Alarm> _alarms = new();
    private readonly ConcurrentDictionary<string, List<Alarm>> _tagAlarms = new();
    private Action<string, string, AlarmState>? _publishAlarmEvent;

    public void SetEventPublisher(Action<string, string, AlarmState>? publish)
    {
        _publishAlarmEvent = publish;
    }

    public bool LoadFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            return LoadFromJson(doc.RootElement);
        }
        catch
        {
            return false;
        }
    }

    public bool LoadFromJson(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("alarms", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var node = JsonNode.Parse(item.GetRawText());
                    if (node is JsonObject obj && Alarm.FromJson(obj) is { } alarm)
                        AddAlarm(alarm);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool AddAlarm(Alarm alarm)
    {
        if (alarm == null || string.IsNullOrEmpty(alarm.Name)) return false;
        _alarms[alarm.Name] = alarm;
        UpdateTagAlarmMapping(alarm);
        alarm.StateChanged += (_, pair) =>
        {
            _publishAlarmEvent?.Invoke(alarm.Name, "StateChanged", pair.NewState);
        };
        return true;
    }

    public bool RemoveAlarm(string alarmName)
    {
        if (!_alarms.TryRemove(alarmName, out var alarm)) return false;
        if (!string.IsNullOrEmpty(alarm.TagName) && _tagAlarms.TryGetValue(alarm.TagName, out var list))
        {
            list.RemoveAll(a => a.Name == alarmName);
        }
        return true;
    }

    public Alarm? GetAlarm(string alarmName) => _alarms.TryGetValue(alarmName, out var a) ? a : null;

    public IReadOnlyList<string> GetAlarmNames() => _alarms.Keys.ToList();

    public IReadOnlyList<string> GetActiveAlarms() =>
        _alarms.Values.Where(a => a.State == AlarmState.Active || a.State == AlarmState.Acknowledged).Select(a => a.Name).ToList();

    public IReadOnlyList<string> GetUnacknowledgedAlarms() =>
        _alarms.Values.Where(a => a.State == AlarmState.Active).Select(a => a.Name).ToList();

    public IReadOnlyList<Alarm> GetActiveAlarmObjects() =>
        _alarms.Values.Where(a => a.State == AlarmState.Active || a.State == AlarmState.Acknowledged).ToList();

    public void EvaluateTagValue(string tagName, object? value, int quality = 0)
    {
        if (!_tagAlarms.TryGetValue(tagName, out var alarms)) return;
        foreach (var alarm in alarms)
        {
            if (!alarm.Enabled) continue;
            var conditionMet = AlarmCondition.Evaluate(alarm, value);
            var shouldClear = AlarmCondition.ShouldClear(alarm, value);
            if (alarm.State == AlarmState.Normal || alarm.State == AlarmState.Cleared)
            {
                if (conditionMet)
                {
                    alarm.Activate();
                    _publishAlarmEvent?.Invoke(alarm.Name, "Activated", alarm.State);
                }
            }
            else if (shouldClear)
            {
                alarm.Clear();
                _publishAlarmEvent?.Invoke(alarm.Name, "Cleared", alarm.State);
            }
            alarm.LastValue = value;
        }
    }

    public bool AcknowledgeAlarm(string alarmName)
    {
        if (!_alarms.TryGetValue(alarmName, out var alarm)) return false;
        if (alarm.State != AlarmState.Active) return false;
        alarm.Acknowledge();
        _publishAlarmEvent?.Invoke(alarmName, "Acknowledged", alarm.State);
        return true;
    }

    public void AcknowledgeAllAlarms()
    {
        foreach (var name in GetUnacknowledgedAlarms())
            AcknowledgeAlarm(name);
    }

    private void UpdateTagAlarmMapping(Alarm alarm)
    {
        if (string.IsNullOrEmpty(alarm.TagName)) return;
        _tagAlarms.AddOrUpdate(alarm.TagName, _ => new List<Alarm> { alarm }, (_, list) =>
        {
            list.RemoveAll(a => a.Name == alarm.Name);
            list.Add(alarm);
            return list;
        });
    }
}
