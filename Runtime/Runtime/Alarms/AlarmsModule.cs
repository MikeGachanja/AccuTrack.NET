using System.Collections.Concurrent;
using System.Text.Json;
using AccuTrack.Runtime.EventDispatch;
using AccuTrack.Runtime.Modules;
using AccuTrack.Runtime.Tags;

namespace AccuTrack.Runtime.Alarms;

/// <summary>
/// AlarmsModule implementing IModuleInterface; AlarmManager; subscribe to tag changes; raise/clear alarms. Config: alarms.json.
/// </summary>
public sealed class AlarmsModule : IModuleInterface
{
    private readonly AlarmManager _alarmManager = new();
    private JsonElement? _config;
    private bool _running;
    private string _status = "Stopped";

    public AlarmManager AlarmManager => _alarmManager;

    public string ModuleName => "Alarms";
    public string DisplayName => "Alarms Module";
    public string Version => "1.0";
    public IReadOnlyList<string> Dependencies => new[] { "Tags" };
    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

    public Task InitializeAsync(JsonElement? config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _alarmManager.LoadFromConfig(config);
        SetStatus("Initialized");
        Initialized?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _running = true;
        SetStatus("Running");
        Started?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _running = false;
        SetStatus("Stopped");
        Stopped?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public JsonElement? GetConfiguration() => _config;
    public bool ValidateConfiguration(JsonElement? config) => true;

    public void SetTagManager(TagManager? tagManager)
    {
        _alarmManager.SetTagManager(tagManager);
    }

    public void SubscribeToTagEvents(EventDispatcher dispatcher)
    {
        dispatcher.Subscribe(EventTypes.TagValueChanged, evt =>
        {
            var name = evt.Source ?? evt.Payload?.GetValueOrDefault("TagName")?.ToString();
            if (name != null)
                _alarmManager.EvaluateConditions(name);
            return ValueTask.CompletedTask;
        });
    }

    private void SetStatus(string s)
    {
        _status = s;
        StatusChanged?.Invoke(this, s);
    }
}
