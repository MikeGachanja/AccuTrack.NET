using System.Collections.Concurrent;
using System.Text.Json;
using AccuTrack.Runtime.Modules;
using AccuTrack.Runtime.Tags;

namespace AccuTrack.Runtime.Historian;

/// <summary>
/// HistorianModule implementing IModuleInterface; sample tags at interval; store (SQLite stub); retention. Config: historian.json.
/// </summary>
public sealed class HistorianModule : IModuleInterface
{
    private readonly HistorianManager _manager = new();
    private JsonElement? _config;
    private bool _running;
    private string _status = "Stopped";
    private Timer? _sampleTimer;
    private TagManager? _tagManager;

    public HistorianManager Manager => _manager;

    public string ModuleName => "Historian";
    public string DisplayName => "Historian Module";
    public string Version => "1.0";
    public IReadOnlyList<string> Dependencies => new[] { "Tags" };
    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

    public void SetTagManager(TagManager? manager) => _tagManager = manager;

    public Task InitializeAsync(JsonElement? config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _manager.LoadConfig(config);
        SetStatus("Initialized");
        Initialized?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _running = true;
        var intervalMs = _manager.SampleIntervalMs;
        _sampleTimer = new Timer(_ => SampleTags(), null, intervalMs, intervalMs);
        SetStatus("Running");
        Started?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _sampleTimer?.Dispose();
        _sampleTimer = null;
        _running = false;
        SetStatus("Stopped");
        Stopped?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public JsonElement? GetConfiguration() => _config;
    public bool ValidateConfiguration(JsonElement? config) => true;

    private void SampleTags()
    {
        if (_tagManager == null) return;
        foreach (var tagName in _manager.SampledTags)
        {
            var v = _tagManager.GetValue(tagName);
            if (v != null)
                _manager.Record(tagName, v);
        }
    }

    private void SetStatus(string s)
    {
        _status = s;
        StatusChanged?.Invoke(this, s);
    }
}
